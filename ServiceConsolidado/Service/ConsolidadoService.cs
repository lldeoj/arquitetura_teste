using Lancamentos.Library.Interface;
using Microsoft.Extensions.Logging;
using ServiceConsolidado.Interface;
using ServiceConsolidado.Models;
using System.Text.Json;

namespace ServiceConsolidado.Service
{
    public class ConsolidadoService : IConsolidadoService
    {
        private readonly ILancamentoService _lancamentoService;
        private readonly ILogger<ConsolidadoService> _logger;

        public ConsolidadoService(ILancamentoService lancamentoService, ILogger<ConsolidadoService> logger)
        {
            _lancamentoService = lancamentoService;
            _logger = logger;
        }

        public async Task<SaldoDiarioConsolidado> CalcularSaldoConsolidadoAsync(ConsolidadoRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _logger.LogInformation("Iniciando cálculo do saldo consolidado para ID: {Id}, Agência: {Agencia}, Conta: {Conta}, Dia: {Dia}",
                    request.Id, request.Agencia, request.Conta, request.Dia.Date);

                // Buscar todos os lançamentos do dia para a agência e conta especificada
                var lancamentos = await _lancamentoService.GetLancamentosByAgencyAccountAndDateAsync(
                    request.Agencia,
                    request.Conta,
                    request.Dia.Date,
                    cancellationToken
                );

                decimal saldoInicial = 0; // Pode ser obtido de uma tabela de saldos iniciais
                decimal totalCreditos = lancamentos
                    .Where(l => l.IsCredito)
                    .Sum(l => l.Valor);

                decimal totalDebitos = lancamentos
                    .Where(l => !l.IsCredito)
                    .Sum(l => l.Valor);

                decimal saldoFinal = saldoInicial + totalCreditos - totalDebitos;

                var consolidado = new SaldoDiarioConsolidado
                {
                    Id = request.Id,
                    Agencia = request.Agencia,
                    Conta = request.Conta,
                    Dia = request.Dia.Date,
                    SaldoInicial = saldoInicial,
                    TotalCreditos = totalCreditos,
                    TotalDebitos = totalDebitos,
                    SaldoFinal = saldoFinal,
                    DataProcessamento = DateTime.UtcNow
                };

                _logger.LogInformation("Saldo consolidado calculado: ID: {Id}, Saldo Final: {SaldoFinal}",
                    consolidado.Id, consolidado.SaldoFinal);

                return consolidado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular saldo consolidado para ID: {Id}", request.Id);
                throw;
            }
        }

        public async Task SalvarRelatorioAsync(SaldoDiarioConsolidado relatorio, string caminhoSaida, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Garantir que o diretório existe
                Directory.CreateDirectory(caminhoSaida);

                // Caminho completo do arquivo: /caminho/{GUID}.json
                string nomeArquivo = $"{relatorio.Id}.json";
                string caminhoCompleto = Path.Combine(caminhoSaida, nomeArquivo);

                // Serializar para JSON com formatação indentada
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonConteudo = JsonSerializer.Serialize(relatorio, options);

                // Salvar arquivo
                await File.WriteAllTextAsync(caminhoCompleto, jsonConteudo, cancellationToken);

                _logger.LogInformation("Relatório salvo com sucesso no caminho: {CaminhoCompleto}", caminhoCompleto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar relatório para ID: {Id}", relatorio.Id);
                throw;
            }
        }
    }
}
