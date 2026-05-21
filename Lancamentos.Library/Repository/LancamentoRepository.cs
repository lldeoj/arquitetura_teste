using Lancamentos.Library.Interface;
using Lancamentos.Library.Models;
using Microsoft.EntityFrameworkCore;
using ServiceLancamentos.Data;

namespace Lancamentos.Library.Repository
{
    public class LancamentoRepository : ILancamentoRepository
    {
        private readonly AppDbContext _context;

        public LancamentoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Lancamento?> GetByIdAsync(Guid id)
        {
            return await _context.Lancamentos.FindAsync(id);
        }

        public async Task<IEnumerable<Lancamento>> GetAllAsync()
        {
            return await _context.Lancamentos
                .OrderByDescending(x => x.DataHora)
                .ToListAsync();
        }

        public async Task<IEnumerable<Lancamento>> GetByUsuarioAsync(string usuario)
        {
            return await _context.Lancamentos
                .Where(x => x.Usuario == usuario)
                .OrderByDescending(x => x.DataHora)
                .ToListAsync();
        }

        public async Task<IEnumerable<Lancamento>> GetByContaAsync(string conta)
        {
            return await _context.Lancamentos
                .Where(x => x.ContaOrigem == conta)
                .OrderByDescending(x => x.DataHora)
                .ToListAsync();
        }

        public async Task<IEnumerable<Lancamento>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Lancamentos
                .Where(x => x.DataHora >= startDate && x.DataHora <= endDate)
                .OrderByDescending(x => x.DataHora)
                .ToListAsync();
        }

        public async Task<Lancamento> AddAsync(Lancamento lancamento)
        {
            if (lancamento.Id == Guid.Empty)
            {
                lancamento.Id = Guid.NewGuid();
            }

            if (lancamento.DataHora == default)
            {
                lancamento.DataHora = DateTime.UtcNow;
            }

            _context.Lancamentos.Add(lancamento);
            await _context.SaveChangesAsync();
            return lancamento;
        }

        public async Task<Lancamento> UpdateAsync(Lancamento lancamento)
        {
            var existingLancamento = await _context.Lancamentos.FindAsync(lancamento.Id);
            if (existingLancamento == null)
            {
                throw new InvalidOperationException($"Lancamento com ID {lancamento.Id} não encontrado.");
            }

            _context.Entry(existingLancamento).CurrentValues.SetValues(lancamento);
            await _context.SaveChangesAsync();
            return lancamento;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var lancamento = await _context.Lancamentos.FindAsync(id);
            if (lancamento == null)
            {
                return false;
            }

            _context.Lancamentos.Remove(lancamento);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CountAsync()
        {
            return await _context.Lancamentos.CountAsync();
        }

        public async Task<decimal> GetTotalByTipoAsync(bool isCredito)
        {
            return await _context.Lancamentos
                .Where(x => x.IsCredito == isCredito)
                .SumAsync(x => x.Valor);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Lancamentos.AnyAsync(x => x.Id == id);
        }
    }
}
