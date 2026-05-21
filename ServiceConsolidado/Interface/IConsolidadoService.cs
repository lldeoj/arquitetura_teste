using ServiceConsolidado.Models;

namespace ServiceConsolidado.Interface
{
    public interface IConsolidadoService
    {
        Task<SaldoDiarioConsolidado> CalcularSaldoConsolidadoAsync(ConsolidadoRequest request, CancellationToken cancellationToken);
        Task SalvarRelatorioAsync(SaldoDiarioConsolidado relatorio, string caminhoSaida, CancellationToken cancellationToken);
    }
}
