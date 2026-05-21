using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers
{
    /// <summary>
    /// Controller responsável pela recuperação de relatórios gerados
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class RelatorioController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public RelatorioController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Recupera um relatório consolidado gerado
        /// </summary>
        /// <param name="fileName">Nome do arquivo do relatório (ex: {guid}.json)</param>
        /// <returns>Conteúdo JSON do relatório</returns>
        /// <response code="200">Relatório encontrado e retornado com sucesso</response>
        /// <response code="404">Relatório não encontrado</response>
        [HttpGet("{fileName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Get(string fileName)
        {
            var outputPath = _configuration["ConsolidadoSettings:OutputPath"] ?? "/app/relatorios";
            var fullPath = Path.Combine(outputPath, fileName);

            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { message = $"Relatório '{fileName}' não encontrado" });

            var json = System.IO.File.ReadAllText(fullPath);
            return Content(json, "application/json");
        }
    }
}
