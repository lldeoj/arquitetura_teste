using Microsoft.Extensions.Logging;
using RabbitMqMessage.Interface;
using ServiceConsolidado.Interface;
using ServiceConsolidado.Models;

namespace ServiceConsolidado.Service
{
    public class ConsolidadoProcessService : IConsolidadoProcessService
    {
        private readonly ILogger<ConsolidadoProcessService> _logger;
        private readonly IRabbitMqMessageRepository _rabbitMqRepository;
        private readonly IConsolidadoService _consolidadoService;
        private readonly string _caminhoSaida;

        public ConsolidadoProcessService(
            IRabbitMqMessageRepository rabbitMqRepository,
            IConsolidadoService consolidadoService,
            ILogger<ConsolidadoProcessService> logger,
            IConfiguration configuration)
        {
            _rabbitMqRepository = rabbitMqRepository;
            _consolidadoService = consolidadoService;
            _logger = logger;
            _caminhoSaida = configuration["ConsolidadoSettings:OutputPath"] ?? "/app/relatorios";
        }

        public void ListenerOnRabbitMqQueue(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _logger.LogInformation("Iniciando listener da fila consolidado.");
                StartListeningQueue(_rabbitMqRepository.ListenToQueueAsync, cancellationToken);

                _logger.LogInformation("Iniciando listener da fila de retry consolidado.");
                StartListeningQueue(_rabbitMqRepository.ListenToRetryQueueAsync, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao iniciar os listeners da fila.");
                throw;
            }
        }

        private void StartListeningQueue(Func<Func<ConsolidadoRequest, Task>, Task> listenToQueue, CancellationToken cancellationToken)
        {
            var listenerTask = listenToQueue(async (message) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await ProcessMessageWithServicesAsync(message, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar a mensagem recebida do RabbitMQ.");
                }
            });

            // Capture and log any startup/connection errors from the fire-and-forget listener task
            listenerTask.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    _logger.LogError(t.Exception, "Listener task faulted while starting or during execution.");
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task ProcessMessageWithServicesAsync(ConsolidadoRequest message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (message is null)
            {
                _logger.LogWarning("Mensagem recebida é nula. Ignorando processamento.");
                return;
            }

            _logger.LogInformation("Processando mensagem com ID: {MessageId}, Agência: {Agencia}, Conta: {Conta}, Dia: {Dia}",
                message.Id, message.Agencia, message.Conta, message.Dia.Date);

            try
            {
                // Calcular saldo consolidado
                var saldoConsolidado = await _consolidadoService.CalcularSaldoConsolidadoAsync(message, cancellationToken);

                // Salvar relatório em arquivo JSON
                await _consolidadoService.SalvarRelatorioAsync(saldoConsolidado, _caminhoSaida, cancellationToken);

                _logger.LogInformation("Mensagem processada com sucesso. ID: {MessageId}", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro fatal ao processar mensagem ID: {MessageId}. Será necessário processar novamente.", message.Id);
                throw;
            }
        }
    }
}
