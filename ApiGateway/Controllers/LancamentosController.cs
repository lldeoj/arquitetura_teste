using Lancamentos.Library.Models;
using RabbitMqMessage.Interface;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers
{
    /// <summary>
    /// Controller responsável pela gestão de lançamentos
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class LancamentosController : ControllerBase
    {
        private readonly IRabbitMqMessageRepository _rabbit;

        public LancamentosController(IRabbitMqMessageRepository rabbit)
        {
            _rabbit = rabbit;
        }

        /// <summary>
        /// Adiciona um novo lançamento na fila
        /// </summary>
        /// <param name="request">Dados do lançamento</param>
        /// <returns>Retorna 202 Accepted se adicionado com sucesso</returns>
        /// <response code="202">Lançamento adicionado na fila com sucesso</response>
        /// <response code="400">Requisição inválida</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post([FromBody] RabbitMQRequest request)
        {
            if (request == null) return BadRequest("Requisição não pode ser nula");

            await _rabbit.SendQueueMessageAsync(request);
            return Accepted();
        }
    }
}
