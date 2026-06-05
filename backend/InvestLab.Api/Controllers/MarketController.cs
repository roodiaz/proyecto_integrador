using InvestLab.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestLab.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MarketController : BaseController
    {
        private readonly IMarketService _service;
        private readonly ILogger<MarketController> _logger;

        public MarketController(IMarketService service, ILogger<MarketController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el panorama principal del mercado.
        /// </summary>
        /// <returns>
        /// Estado actual del mercado e índices principales.
        /// </returns>
        /// <response code="200">
        /// Panorama de mercado obtenido correctamente.
        /// </response>
        /// <response code="400">
        /// Error al obtener el panorama de mercado.
        /// </response>
        [HttpGet("overview")]
        public async Task<IActionResult> GetMarketOverview()
        {
            _logger.LogInformation("Solicitud de panorama principal del mercado recibida");

            var result = await _service.GetMarketOverviewAsync();

            if (!result.Success)
            {
                _logger.LogWarning("Error al obtener panorama de mercado: {Message}", result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}