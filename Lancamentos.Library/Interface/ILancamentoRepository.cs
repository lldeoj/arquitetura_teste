using Lancamentos.Library.Models;

namespace Lancamentos.Library.Interface
{
    public interface ILancamentoRepository
    {
        Task<Lancamento?> GetByIdAsync(Guid id);
        Task<IEnumerable<Lancamento>> GetAllAsync();
        Task<IEnumerable<Lancamento>> GetByUsuarioAsync(string usuario);
        Task<IEnumerable<Lancamento>> GetByContaAsync(string conta);
        Task<IEnumerable<Lancamento>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Lancamento> AddAsync(Lancamento lancamento);
        Task<Lancamento> UpdateAsync(Lancamento lancamento);
        Task<bool> DeleteAsync(Guid id);
        Task<int> CountAsync();
        Task<decimal> GetTotalByTipoAsync(bool isCredito);
        Task<bool> ExistsAsync(Guid id);
    }
}
