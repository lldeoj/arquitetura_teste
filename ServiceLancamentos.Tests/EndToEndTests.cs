using System;
using System.Threading.Tasks;
using Lancamentos.Library.Interface;
using Lancamentos.Library.Models;
using Lancamentos.Library.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceLancamentos.Data;
using Xunit;

namespace ServiceLancamentos.Tests
{
    public class EndToEndTests
    {
        private AppDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task MessageProcessing_ShouldStoreRecordWithSameId_AndBeIdempotent()
        {
            // Arrange
            var db = CreateInMemoryDb();
            var repository = new LancamentoRepository(db);
            var service = new Lancamentos.Library.Service.LancamentoService(repository);

            var messageId = Guid.NewGuid();
            var request = new RabbitMQRequest
            {
                Id = messageId,
                Valor = 100m,
                IsCredito = true,
                AgenciaOrigem = "001",
                ContaOrigem = "12345-6",
                Descricao = "Teste",
                Usuario = "usuario",
                DataHora = DateTime.UtcNow
            };

            // Act - first processing
            var createDto = Lancamentos.Library.Mappers.RabbitMQRequestMapper.MapToCreateLancamentoDto(request);
            var created = await service.CreateAsync(createDto);

            // Assert first
            Assert.Equal(messageId, created.Id);
            Assert.Equal(1, await repository.CountAsync());

            // Act - process same message again
            var createDto2 = Lancamentos.Library.Mappers.RabbitMQRequestMapper.MapToCreateLancamentoDto(request);
            var created2 = await service.CreateAsync(createDto2);

            // Assert idempotency: still 1 record and same Id
            Assert.Equal(messageId, created2.Id);
            Assert.Equal(1, await repository.CountAsync());
        }
    }
}
