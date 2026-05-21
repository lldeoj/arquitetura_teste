using Lancamentos.Library.Models;

namespace Lancamentos.Library.Mappers
{
    public static class RabbitMQRequestMapper
    {
        /// <summary>
        /// Mapeia um RabbitMQRequest para LancamentoDto
        /// </summary>
        public static LancamentoDto MapToLancamentoDto(RabbitMQRequest request)
        {
            if (request == null || request.Id == Guid.Empty)
                throw new ArgumentNullException(nameof(request));

            return new LancamentoDto
            {
                Id = request.Id,
                Valor = request.Valor,
                IsCredito = request.IsCredito,
                AgenciaOrigem = request.AgenciaOrigem ?? string.Empty,
                ContaOrigem = request.ContaOrigem ?? string.Empty,
                Descricao = request.Descricao ?? string.Empty,
                Usuario = request.Usuario ?? string.Empty,
                DataHora = request.DataHora != default ? request.DataHora : DateTime.UtcNow
            };
        }

        /// <summary>
        /// Mapeia um RabbitMQRequest para CreateLancamentoDto
        /// </summary>
        public static CreateLancamentoDto MapToCreateLancamentoDto(RabbitMQRequest request)
        {
            if (request == null || request.Id == Guid.Empty)
                throw new ArgumentNullException(nameof(request));

            return new CreateLancamentoDto
            {
                Id = request.Id,
                Valor = request.Valor,
                IsCredito = request.IsCredito,
                AgenciaOrigem = request.AgenciaOrigem ?? string.Empty,
                ContaOrigem = request.ContaOrigem ?? string.Empty,
                Descricao = request.Descricao ?? string.Empty,
                Usuario = request.Usuario ?? string.Empty,
                DataHora = request.DataHora != default ? request.DataHora : DateTime.UtcNow
            };
        }
    }
}
