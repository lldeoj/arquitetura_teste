using Lancamentos.Library.Interface;
using Lancamentos.Library.Mappers;
using Lancamentos.Library.Models;
using Microsoft.Extensions.Logging;
using RabbitMqMessage.Interface;

namespace Lancamentos.Library.Service
{
    public class LancamentoProcessService : ILancamentoProcessService
    {
        private readonly ILogger<LancamentoProcessService> _logger;
        private readonly IRabbitMqMessageRepository _rabbitMqRepository;
        private readonly ILancamentoService _lancamentoService;

        public LancamentoProcessService(IRabbitMqMessageRepository rabbitMqRepository, ILancamentoService lancamentoService, ILogger<LancamentoProcessService> logger)
        {
            _rabbitMqRepository = rabbitMqRepository;
            _logger = logger;
            _lancamentoService = lancamentoService;
        }

        public void ListenerOnRabbitMqQueue(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _logger.LogInformation("Iniciando listener da fila principal.");
                StartListeningQueue(_rabbitMqRepository.ListenToQueueAsync, cancellationToken);

                _logger.LogInformation("Iniciando listener da fila de retry.");
                StartListeningQueue(_rabbitMqRepository.ListenToRetryQueueAsync, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao iniciar os listeners da fila.");
                throw;
            }
        }

        private void StartListeningQueue(Func<Func<RabbitMQRequest, Task>, Task> listenToQueue, CancellationToken cancellationToken)
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

        private async Task ProcessMessageWithServicesAsync(RabbitMQRequest message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (message is null)
            {
                _logger.LogWarning("Mensagem recebida é nula. Ignorando processamento.");
                return;
            }

            _logger.LogInformation("Processando mensagem com ID: {MessageId}, Usuário: {Usuario}, Valor: {Valor}", 
                message.Id, message.Usuario, message.Valor);

            try
            {
                // Mapear RabbitMQRequest para CreateLancamentoDto
                var createLancamentoDto = RabbitMQRequestMapper.MapToCreateLancamentoDto(message);

                // Criar o lançamento no banco de dados
                var lancamentoDto = await _lancamentoService.CreateAsync(createLancamentoDto);

                _logger.LogInformation("Lançamento criado com sucesso. ID: {LancamentoId}, Usuário: {Usuario}", 
                    lancamentoDto.Id, lancamentoDto.Usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem com ID: {MessageId}. Detalhes: {Message}", 
                    message.Id, ex.Message);
                throw;
            }
        }
    }
}
