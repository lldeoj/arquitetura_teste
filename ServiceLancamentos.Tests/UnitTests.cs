using System;
using System.Linq;
using System.Threading.Tasks;
using Lancamentos.Library.Interface;
using Lancamentos.Library.Models;
using Lancamentos.Library.Repository;
using Lancamentos.Library.Service;
using Lancamentos.Library.Mappers;
using Microsoft.EntityFrameworkCore;
using ServiceLancamentos.Data;
using Xunit;
using Lancamentos.Domain.Dto;

namespace ServiceLancamentos.Tests
{
    public class UnitTests
    {
        private AppDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task Repository_Add_GetById_Count_Exists()
        {
            var db = CreateInMemoryDb();
            var repo = new LancamentoRepository(db);

            var l = new Lancamento { Id = Guid.NewGuid(), Valor = 10m, IsCredito = true, ContaOrigem = "1", Usuario = "u" };
            var created = await repo.AddAsync(l);

            Assert.Equal(l.Id, created.Id);
            Assert.Equal(1, await repo.CountAsync());
            Assert.True(await repo.ExistsAsync(l.Id));
            var fetched = await repo.GetByIdAsync(l.Id);
            Assert.NotNull(fetched);
            Assert.Equal(l.Valor, fetched!.Valor);
        }

        [Fact]
        public async Task Repository_Update_Throws_WhenNotFound()
        {
            var db = CreateInMemoryDb();
            var repo = new LancamentoRepository(db);

            var l = new Lancamento { Id = Guid.NewGuid(), Valor = 1m };
            await Assert.ThrowsAsync<InvalidOperationException>(() => repo.UpdateAsync(l));
        }

        [Fact]
        public async Task Repository_Delete_ReturnsExpected()
        {
            var db = CreateInMemoryDb();
            var repo = new LancamentoRepository(db);

            var id = Guid.NewGuid();
            Assert.False(await repo.DeleteAsync(id));

            var l = new Lancamento { Id = id, Valor = 2m };
            await repo.AddAsync(l);
            Assert.True(await repo.DeleteAsync(id));
        }

        [Fact]
        public async Task Repository_QueryMethods_Work()
        {
            var db = CreateInMemoryDb();
            var repo = new LancamentoRepository(db);

            var a = new Lancamento { Id = Guid.NewGuid(), Usuario = "A", ContaOrigem = "C1", Valor = 5m, IsCredito = true, DataHora = DateTime.UtcNow.AddDays(-2) };
            var b = new Lancamento { Id = Guid.NewGuid(), Usuario = "B", ContaOrigem = "C2", Valor = 7m, IsCredito = false, DataHora = DateTime.UtcNow.AddDays(-1) };
            var c = new Lancamento { Id = Guid.NewGuid(), Usuario = "A", ContaOrigem = "C1", Valor = 3m, IsCredito = true, DataHora = DateTime.UtcNow };

            await repo.AddAsync(a);
            await repo.AddAsync(b);
            await repo.AddAsync(c);

            var all = (await repo.GetAllAsync()).ToList();
            Assert.Equal(3, all.Count);

            var byUser = (await repo.GetByUsuarioAsync("A")).ToList();
            Assert.Equal(2, byUser.Count);

            var byConta = (await repo.GetByContaAsync("C2")).ToList();
            Assert.Single(byConta);

            var range = (await repo.GetByDateRangeAsync(DateTime.UtcNow.AddDays(-3), DateTime.UtcNow.AddDays(-1))).ToList();
            Assert.Equal(2, range.Count);

            var totalCredito = await repo.GetTotalByTipoAsync(true);
            Assert.Equal(8m, totalCredito);
        }

        [Fact]
        public async Task Service_Create_And_Idempotency_And_Validation()
        {
            var db = CreateInMemoryDb();
            var repo = new LancamentoRepository(db) as ILancamentoRepository;
            var service = new LancamentoService(repo!);

            // Validation: empty id
            var invalid = new CreateLancamentoDto { Id = Guid.Empty, Valor = 1m };
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(invalid));

            var id = Guid.NewGuid();
            var dto = new CreateLancamentoDto { Id = id, Valor = 10m, IsCredito = true, Usuario = "u" };
            var created = await service.CreateAsync(dto);
            Assert.Equal(id, created.Id);
            Assert.Equal(1, await service.GetTotalCountAsync());

            // Idempotent: create again same id returns same
            var created2 = await service.CreateAsync(dto);
            Assert.Equal(id, created2.Id);
            Assert.Equal(1, await service.GetTotalCountAsync());
        }

        [Fact]
        public async Task Service_Update_And_Delete_Works_And_Throws_WhenMissing()
        {
            var db = CreateInMemoryDb();
            var repo = new LancamentoRepository(db) as ILancamentoRepository;
            var service = new LancamentoService(repo!);

            var id = Guid.NewGuid();
            var dto = new UpdateLancamentoDto { Id = id, Valor = 2m };
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(dto));

            var create = new CreateLancamentoDto { Id = id, Valor = 5m, IsCredito = false };
            await service.CreateAsync(create);

            var update = new UpdateLancamentoDto { Id = id, Valor = 20m, IsCredito = false };
            var updated = await service.UpdateAsync(update);
            Assert.Equal(20m, updated.Valor);

            var deleted = await service.DeleteAsync(id);
            Assert.True(deleted);

            var deletedAgain = await service.DeleteAsync(id);
            Assert.False(deletedAgain);
        }

        [Fact]
        public void Mapper_NullOrEmpty_Throws_And_Maps()
        {
            Assert.Throws<ArgumentNullException>(() => RabbitMQRequestMapper.MapToLancamentoDto(null!));
            Assert.Throws<ArgumentNullException>(() => RabbitMQRequestMapper.MapToCreateLancamentoDto(new RabbitMQRequest { Id = Guid.Empty }));

            var id = Guid.NewGuid();
            var req = new RabbitMQRequest { Id = id, Valor = 99m, IsCredito = true, AgenciaOrigem = "a", ContaOrigem = "c", Descricao = "d", Usuario = "u", DataHora = DateTime.UtcNow };
            var dto = RabbitMQRequestMapper.MapToCreateLancamentoDto(req);
            Assert.Equal(id, dto.Id);
            Assert.Equal(99m, dto.Valor);
        }
    }
}
