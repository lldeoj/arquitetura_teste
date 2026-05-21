using Lancamentos.Domain.Dto;

namespace Lancamentos.Library.Interface
{
    public interface ILancamentoService
    {
        Task<LancamentoDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<LancamentoDto>> GetAllAsync();
        Task<IEnumerable<LancamentoDto>> SearchAsync(LancamentoFilterDto filter);
        Task<decimal> GetTotalCreditoAsync();
        Task<decimal> GetTotalDebitoAsync();
        Task<int> GetTotalCountAsync();
        Task<LancamentoDto> CreateAsync(CreateLancamentoDto dto);
        Task<LancamentoDto> UpdateAsync(UpdateLancamentoDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<IEnumerable<LancamentoDto>> GetLancamentosByAgencyAccountAndDateAsync(string agencia, string conta, DateTime dia, CancellationToken cancellationToken);
    }
}
