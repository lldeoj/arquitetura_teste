using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMqMessage.Models;

namespace RabbitMqMessage
{
    public class RabbitMqDataContext : IDisposable
    {
        private IConnection? _connection;
        private IChannel? _channel;

        private readonly IServiceProvider _provider;
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<RabbitMqDataContext> _logger;

        public RabbitMqDataContext(IServiceProvider provider, IConnectionFactory connectionFactory, ILogger<RabbitMqDataContext> logger)
        {
            _provider = provider;
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<IChannel> GetChannelAsync()
        {
            if (_channel == null)
            {
                _connection = await _connectionFactory.CreateConnectionAsync();
                LogInformation("RabbitMQ connection established.");
                _channel = await _connection.CreateChannelAsync();
                LogInformation("RabbitMQ channel created.");
            }

            return _channel;
        }

        #region Métodos Públicos
        public async Task SendMessageAsync<T>(QueueSettings settings, T message)
        {
            var channel = await GetChannelAsync();
            await EnsureTopologyAsync(settings);

            try
            {
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                await channel.BasicPublishAsync(
                    exchange: settings.Exchange,
                    routingKey: settings.Queue,
                    body: body
                );
            }
            catch (Exception ex)
            {
                LogError(ex, $"Erro ao enviar mensagem para a fila: {settings.Queue}.");
                throw;
            }
        }

        public async Task<T?> ReceiveMessageAsync<T>(QueueSettings settings)
        {
            var channel = await GetChannelAsync();

            try
            {
                var result = await channel.BasicGetAsync(settings.Queue, autoAck: true);
                if (result == null)
                    return default;

                var message = Encoding.UTF8.GetString(result.Body.ToArray());
                return await Task.FromResult(JsonSerializer.Deserialize<T>(message));
            }
            catch (Exception ex)
            {
                LogError(ex, $"Erro ao receber mensagem da fila: {settings.Queue}.");
                throw;
            }
        }

        public async Task ListenToQueueAsync<T>(Func<T, Task> processMessage, QueueSettings settings, string queueName)
        {
            var channel = await GetChannelAsync();
            await EnsureTopologyAsync(settings);
            var consumer = new AsyncEventingBasicConsumer(channel);

            await channel.QueueDeclareAsync(
                queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            consumer.ReceivedAsync += async (model, ea) =>
            {

                LogInformation($"Iniciando consumo da fila: {queueName}");
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    int retryCount = GetRetryCount(ea);
                    LogInformation($"Mensagem recebida da fila: {queueName}. Tentativas: {retryCount}");

                    var deserializedMessage = JsonSerializer.Deserialize<T>(message);

                    if (deserializedMessage != null)
                    {
                        await processMessage(deserializedMessage);
                        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                        LogInformation($"Mensagem processada com sucesso. Fila: {queueName}");
                    }
                    else
                    {
                        LogInformation($"Mensagem nula recebida. Fila: {queueName}");
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, $"Erro ao processar mensagem da fila {queueName}. exception {ex.Message}" );

                    int retryCount = GetRetryCount(ea) + 1;
                    if (retryCount <= settings.MaxRetry)
                    {
                        LogInformation($"Reenviando mensagem. Tentativa {retryCount}. Fila: {queueName}");
                        await Task.Delay(TimeSpan.FromSeconds(15));
                        await SendRetryQueueAsync(body, retryCount, settings, ea);
                    }
                    else
                    {
                        LogInformation($"Número máximo de tentativas atingido. Enviando para fila de falha.");
                        await SendFailQueueAsync(body, settings);
                    }

                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            consumer.ShutdownAsync += async (_, _) =>
            {
                LogInformation($"Consumer da fila {queueName} foi desligado. Tentando reiniciar...");
            };

            consumer.RegisteredAsync += async (_, _) =>
            {
                LogInformation($"Consumer da fila {queueName} registrado no RabbitMQ.");
            };

            consumer.UnregisteredAsync += async (_, _) =>
            {
                LogInformation($"Consumer da fila {queueName} foi removido. Tentando reiniciar...");
            };


            await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumerTag: string.Empty,
                noLocal: false,
                exclusive: false,
                arguments: null,
                consumer: consumer
            );
        }
        #endregion

        #region Métodos Privados
        private static int GetRetryCount(BasicDeliverEventArgs ea)
        {
            var headers = ea.BasicProperties.Headers;
            var retryCount = headers != null && headers.ContainsKey("x-retry-count")
                ? (int?)headers["x-retry-count"] ?? 0
                : 0;
            return retryCount;
        }

        private async Task SendRetryQueueAsync(byte[] body, int retryCount, QueueSettings settings, BasicDeliverEventArgs ea)
        {
            var channel = await GetChannelAsync();

            await EnsureTopologyAsync(settings);

            var props = new BasicProperties(ea.BasicProperties);
            props.Headers ??= new Dictionary<string, object?>();
            props.Headers["x-retry-count"] = retryCount;

            await channel.BasicPublishAsync(
                exchange: settings.Exchange,
                routingKey: settings.QueueRetry,
                basicProperties: props,
                mandatory: false,
                body: body
            );
        }

        private async Task SendFailQueueAsync(byte[] body, QueueSettings settings)
        {
            var channel = await GetChannelAsync();

            await EnsureTopologyAsync(settings);

            await channel.BasicPublishAsync(
                exchange: settings.Exchange,
                routingKey: settings.QueueFail,
                body: body
            );
        }

        private async Task EnsureTopologyAsync(QueueSettings settings)
        {
            var channel = await GetChannelAsync();

            try
            {
                // Declare exchange
                await channel.ExchangeDeclareAsync(
                    exchange: settings.Exchange,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false,
                    arguments: null);
                LogInformation($"Declared exchange: {settings.Exchange}");

                // Declare main queue
                await channel.QueueDeclareAsync(
                    queue: settings.Queue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
                LogInformation($"Declared queue: {settings.Queue}");

                // Bind main queue to exchange
                await channel.QueueBindAsync(
                    queue: settings.Queue,
                    exchange: settings.Exchange,
                    routingKey: settings.Queue,
                    arguments: null);
                LogInformation($"Bound queue {settings.Queue} to exchange {settings.Exchange} with routing key {settings.Queue}");

                // Declare retry queue and bind
                if (!string.IsNullOrEmpty(settings.QueueRetry))
                {
                    await channel.QueueDeclareAsync(
                        queue: settings.QueueRetry,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);
                    LogInformation($"Declared retry queue: {settings.QueueRetry}");

                    await channel.QueueBindAsync(
                        queue: settings.QueueRetry,
                        exchange: settings.Exchange,
                        routingKey: settings.QueueRetry,
                        arguments: null);
                    LogInformation($"Bound retry queue {settings.QueueRetry} to exchange {settings.Exchange} with routing key {settings.QueueRetry}");
                }

                // Declare fail queue and bind
                if (!string.IsNullOrEmpty(settings.QueueFail))
                {
                    await channel.QueueDeclareAsync(
                        queue: settings.QueueFail,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);
                    LogInformation($"Declared fail queue: {settings.QueueFail}");

                    await channel.QueueBindAsync(
                        queue: settings.QueueFail,
                        exchange: settings.Exchange,
                        routingKey: settings.QueueFail,
                        arguments: null);
                    LogInformation($"Bound fail queue {settings.QueueFail} to exchange {settings.Exchange} with routing key {settings.QueueFail}");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Erro ao garantir topologia do RabbitMQ (exchange/queues). ");
                throw;
            }
        }
        #endregion

        public void Dispose()
        {
            _logger.LogInformation("Fechando conexão com o RabbitMQ.");
            _channel?.Dispose();
            _connection?.Dispose();
        }

        private void LogInformation(string message)
        {
            _logger.LogInformation(message);
        }

        private void LogError(Exception ex, string message)
        {
            _logger.LogError(message);
            _logger.LogError($"Message :: {ex.Message}");
            _logger.LogError($"StackTrace :: {ex.StackTrace}");
        }
    }
}
