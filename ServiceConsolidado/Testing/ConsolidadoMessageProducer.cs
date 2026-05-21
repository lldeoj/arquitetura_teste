using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace ServiceConsolidado.Testing
{
    /// <summary>
    /// Exemplo de como enviar uma mensagem de teste para a fila consolidado
    /// Use este código para testar o ServiceConsolidado localmente
    /// </summary>
    public class ConsolidadoMessageProducer
    {
        public static async Task Main(string[] args)
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost", // ou "rabbitmq" se usando Docker
                UserName = "rabbitmq-user",
                Password = "rabbitmq-user",
                Port = 5672
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            const string exchange = "consolidado.exchange";
            const string queue = "consolidado.queue";
            const string routingKey = "consolidado.queue";

            // Declarar exchange e fila
            await channel.ExchangeDeclareAsync(exchange, ExchangeType.Direct, durable: true);
            await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(queue, exchange, routingKey);

            // Criar mensagem
            var request = new
            {
                id = Guid.NewGuid(),
                agencia = "0001",
                conta = "123456",
                dia = DateTime.UtcNow.Date
            };

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            string jsonMessage = JsonSerializer.Serialize(request, jsonOptions);
            byte[] body = Encoding.UTF8.GetBytes(jsonMessage);

            // Enviar
            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                body: body
            );

            Console.WriteLine("Mensagem enviada com sucesso!");
            Console.WriteLine("Request ID: " + request.id);
            Console.WriteLine("Payload: " + jsonMessage);
        }
    }
}
