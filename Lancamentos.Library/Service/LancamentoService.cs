using Lancamentos.Domain.Dto;
using Lancamentos.Library.Interface;
using Lancamentos.Library.Models;

namespace Lancamentos.Library.Service
{
    public class LancamentoService : ILancamentoService
    {
        private readonly ILancamentoRepository _repository;

        public LancamentoService(ILancamentoRepository repository)
        {
            _repository = repository;
        }

        public async Task<LancamentoDto?> GetByIdAsync(Guid id)
        {
            var lancamento = await _repository.GetByIdAsync(id);
            if (lancamento == null)
                return null;

            return MapToDto(lancamento);
        }

        public async Task<IEnumerable<LancamentoDto>> GetAllAsync()
        {
            var lancamentos = await _repository.GetAllAsync();
            return lancamentos.Select(MapToDto);
        }

        public async Task<IEnumerable<LancamentoDto>> SearchAsync(LancamentoFilterDto filter)
        {
            IEnumerable<Lancamento> lancamentos;

            if (filter.DataInicio.HasValue && filter.DataFim.HasValue)
            {
                lancamentos = await _repository.GetByDateRangeAsync(filter.DataInicio.Value, filter.DataFim.Value);
            }
            else if (!string.IsNullOrEmpty(filter.Usuario))
            {
                lancamentos = await _repository.GetByUsuarioAsync(filter.Usuario);
            }
            else if (!string.IsNullOrEmpty(filter.ContaOrigem))
            {
                lancamentos = await _repository.GetByContaAsync(filter.ContaOrigem);
            }
            else
            {
                lancamentos = await _repository.GetAllAsync();
            }

            // Aplicar filtros adicionais
            if (filter.IsCredito.HasValue)
            {
                lancamentos = lancamentos.Where(x => x.IsCredito == filter.IsCredito.Value);
            }

            return lancamentos.Select(MapToDto);
        }

        public async Task<decimal> GetTotalCreditoAsync()
        {
            return await _repository.GetTotalByTipoAsync(true);
        }

        public async Task<decimal> GetTotalDebitoAsync()
        {
            return await _repository.GetTotalByTipoAsync(false);
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _repository.CountAsync();
        }

        public async Task<LancamentoDto> CreateAsync(CreateLancamentoDto dto)
        {
            // Use provided Id to prevent duplicates from messages
            if (dto.Id == Guid.Empty)
            {
                throw new ArgumentException("Id nao informado");
            }

            // If already exists, return existing to avoid duplicate insert
            if (await _repository.ExistsAsync(dto.Id))
            {
                var existing = await _repository.GetByIdAsync(dto.Id);
                if (existing != null)
                    return MapToDto(existing);
            }

            var lancamento = new Lancamento
            {
                Id = dto.Id,
                Valor = dto.Valor,
                IsCredito = dto.IsCredito,
                AgenciaOrigem = dto.AgenciaOrigem,
                ContaOrigem = dto.ContaOrigem,
                Descricao = dto.Descricao,
                Usuario = dto.Usuario,
                DataHora = dto.DataHora ?? DateTime.UtcNow
            };

            var created = await _repository.AddAsync(lancamento);
            return MapToDto(created);
        }

        public async Task<LancamentoDto> UpdateAsync(UpdateLancamentoDto dto)
        {
            var exists = await _repository.ExistsAsync(dto.Id);
            if (!exists)
                throw new InvalidOperationException($"Lancamento com ID {dto.Id} não encontrado.");

            var lancamento = new Lancamento
            {
                Id = dto.Id,
                Valor = dto.Valor,
                IsCredito = dto.IsCredito,
                AgenciaOrigem = dto.AgenciaOrigem,
                ContaOrigem = dto.ContaOrigem,
                Descricao = dto.Descricao,
                Usuario = dto.Usuario
            };

            var updated = await _repository.UpdateAsync(lancamento);
            return MapToDto(updated);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<IEnumerable<LancamentoDto>> GetLancamentosByAgencyAccountAndDateAsync(string agencia, string conta, DateTime dia, CancellationToken cancellationToken)
        {
            var lancamentos = await _repository.GetByDateRangeAsync(dia, dia.AddDays(1));

            return lancamentos
                .Where(l => l.AgenciaOrigem == agencia && l.ContaOrigem == conta)
                .Select(MapToDto)
                .ToList();
        }

        private static LancamentoDto MapToDto(Lancamento lancamento)
        {
            return new LancamentoDto
            {
                Id = lancamento.Id,
                Valor = lancamento.Valor,
                IsCredito = lancamento.IsCredito,
                AgenciaOrigem = lancamento.AgenciaOrigem,
                ContaOrigem = lancamento.ContaOrigem,
                Descricao = lancamento.Descricao,
                Usuario = lancamento.Usuario,
                DataHora = lancamento.DataHora
            };
        }
    }
}
