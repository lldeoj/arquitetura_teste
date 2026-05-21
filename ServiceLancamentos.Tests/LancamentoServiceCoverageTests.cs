using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lancamentos.Library.Interface;
using Lancamentos.Library.Models;
using Lancamentos.Library.Repository;
using Lancamentos.Library.Service;
using Microsoft.EntityFrameworkCore;
using ServiceLancamentos.Data;
using Xunit;

namespace ServiceLancamentos.Tests
{
    public class LancamentoServiceCoverageTests
    {
        private AppDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetById_ReturnsNull_WhenNotFound()
        {
            var db = CreateInMemoryDb();
            var repo = new LancamentoRepository(db) as ILancamentoRepository;
            var service = new LancamentoService(repo!);

            var result = await service.GetByIdAsync(Guid.NewGuid());
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAll_ReturnsAllItems()
        {
            var db = CreateInMemoryDb();
            var repo = new LancamentoRepository(db) as ILancamentoRepository;
            var service = new LancamentoService(repo!);

            await repo.AddAsync(new Lancamento { Id = Guid.NewGuid(), Valor = 1m });
            await repo.AddAsync(new Lancamento { Id = Guid.NewGuid(), Valor = 2m });

            var all = (await service.GetAllAsync()).ToList();
            Assert.Equal(2, all.Count);
        }

        [Fact]
        public async Task SearchAsync_ByDateRange_ReturnsExpected()
        {
            var db = CreateInMemoryDb();
            var repo = new LancamentoRepository(db) as ILancamentoRepository;
            var service = new LancamentoService(repo!);

            var older = new Lancamento { Id = Guid.NewGuid(), DataHora = DateTime.UtcNow.AddDays(-5), Valor = 5m };
            var newer = new Lancamento { Id = Guid.NewGuid(), DataHora = DateTime.UtcNow, Valor = 10m };
            await repo.AddAsync(older);
            await repo.AddAsync(newer);

            var filter = new Lancamentos.Domain.Dto.LancamentoFilterDto { DataInicio = DateTime.UtcNow.AddDays(-1), DataFim = DateTime.UtcNow.AddDays(1) };
            var result = (await service.SearchAsync(filter)).ToList();
            Assert.Single(result);
            Assert.Equal(newer.Id, result[0].Id);
        }

        [Fact]
        public async Task SearchAsync_ByUsuario_ReturnsExpected()
        {
            var db = CreateInMemoryDb();
            var repo = new LancamentoRepository(db) as ILancamentoRepository;
            var service = new LancamentoService(repo!);

            await repo.AddAsync(new Lancamento { Id = Guid.NewGuid(), Usuario = "A", Valor = 1m });
            await repo.AddAsync(new Lancamento { Id = Guid.NewGuid(), Usuario = "B", Valor = 2m });

            var filter = new Lancamentos.Domain.Dto.LancamentoFilterDto { Usuario = "A" };
            var result = (await service.SearchAsync(filter)).ToList();
            Assert.Single(result);
            Assert.Equal("A", result[0].Usuario);
        }

        [Fact]
        public async Task SearchAsync_ByConta_ReturnsExpected()
        {
            var db = CreateInMemoryDb();
            var repo = new LancamentoRepository(db) as ILancamentoRepository;
            var service = new LancamentoService(repo!);

            await repo.AddAsync(new Lancamento { Id = Guid.NewGuid(), ContaOrigem = "C1", Valor = 1m });
            await repo.AddAsync(new Lancamento { Id = Guid.NewGuid(), ContaOrigem = "C2", Valor = 2m });

            var filter = new Lancamentos.Domain.Dto.LancamentoFilterDto { ContaOrigem = "C2" };
            var result = (await service.SearchAsync(filter)).ToList();
            Assert.Single(result);
            Assert.Equal("C2", result[0].ContaOrigem);
        }

        [Fact]
        public async Task SearchAsync_FilterIsCredito_ReturnsExpected()
        {
            var db = CreateInMemoryDb();
            var repo = new LancamentoRepository(db) as ILancamentoRepository;
            var service = new LancamentoService(repo!);

            await repo.AddAsync(new Lancamento { Id = Guid.NewGuid(), IsCredito = true, Valor = 5m });
            await repo.AddAsync(new Lancamento { Id = Guid.NewGuid(), IsCredito = false, Valor = 3m });

            var filter = new Lancamentos.Domain.Dto.LancamentoFilterDto { IsCredito = true };
            var result = (await service.SearchAsync(filter)).ToList();
            Assert.Single(result);
            Assert.True(result[0].IsCredito);
        }

        [Fact]
        public async Task GetTotals_ReturnCorrectValues()
        {
            var db = CreateInMemoryDb();
            var repo = new LancamentoRepository(db) as ILancamentoRepository;
            var service = new LancamentoService(repo!);

            await repo.AddAsync(new Lancamento { Id = Guid.NewGuid(), IsCredito = true, Valor = 7m });
            await repo.AddAsync(new Lancamento { Id = Guid.NewGuid(), IsCredito = false, Valor = 2m });

            var totalCredito = await service.GetTotalCreditoAsync();
            var totalDebito = await service.GetTotalDebitoAsync();

            Assert.Equal(7m, totalCredito);
            Assert.Equal(2m, totalDebito);
        }

        [Fact]
        public async Task GetLancamentosByAgencyAccountAndDateAsync_ReturnsExpected()
        {
            var db = CreateInMemoryDb();
            var repo = new LancamentoRepository(db) as ILancamentoRepository;
            var service = new LancamentoService(repo!);

            var dia = DateTime.UtcNow.Date;
            await repo.AddAsync(new Lancamento { Id = Guid.NewGuid(), AgenciaOrigem = "0001", ContaOrigem = "111", DataHora = dia.AddHours(1), Valor = 1m });
            await repo.AddAsync(new Lancamento { Id = Guid.NewGuid(), AgenciaOrigem = "0001", ContaOrigem = "222", DataHora = dia.AddHours(2), Valor = 2m });

            var result = (await service.GetLancamentosByAgencyAccountAndDateAsync("0001", "111", dia, CancellationToken.None)).ToList();
            Assert.Single(result);
            Assert.Equal("111", result[0].ContaOrigem);
        }
    }
}
