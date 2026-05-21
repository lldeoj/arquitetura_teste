using ServiceConsolidado.Models;
using RabbitMqMessage.Interface;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers
{
    /// <summary>
    /// Controller responsável pela gestão de consolidados
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ConsolidadoController : ControllerBase
    {
        private readonly IRabbitMqMessageRepository _rabbit;

        public ConsolidadoController(IRabbitMqMessageRepository rabbit)
        {
            _rabbit = rabbit;
        }

        /// <summary>
        /// Adiciona uma solicitação de consolidado na fila
        /// </summary>
        /// <param name="request">Dados do consolidado</param>
        /// <returns>Retorna 202 Accepted com o nome do arquivo que será gerado</returns>
        /// <response code="202">Consolidado adicionado na fila com sucesso</response>
        /// <response code="400">Requisição inválida</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post([FromBody] ConsolidadoRequest request)
        {
            if (request == null) return BadRequest("Requisição não pode ser nula");

            request.Id = Guid.NewGuid();

            await _rabbit.SendQueueMessageAsync(request);
            return Accepted(new { fileName = request.Id.ToString() + ".json" });
        }
    }
}
