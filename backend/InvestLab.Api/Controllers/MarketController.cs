using InvestLab.Business.Interfaces;
using InvestLab.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestLab.Api.Controllers
{
    /// <summary>
    /// Controlador encargado de la consulta de información de mercado.
    /// Expone panorama general, detalle de activos, tendencias, rankings y noticias.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Response), StatusCodes.Status401Unauthorized)]
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
        [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Obtiene el detalle de un activo del mercado.
        /// </summary>
        /// <param name="symbol">
        /// Símbolo del activo a consultar.
        /// </param>
        /// <returns>
        /// Información principal del activo solicitado.
        /// </returns>
        /// <response code="200">
        /// Activo obtenido correctamente.
        /// </response>
        /// <response code="400">
        /// Error al obtener el activo solicitado.
        /// </response>
        [HttpGet("asset/{symbol}")]
        [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAssetDetail(string symbol)
        {
            _logger.LogInformation("Solicitud de detalle de activo recibida para {Symbol}", symbol);

            var result = await _service.GetAssetDetailAsync(symbol);

            if (!result.Success)
            {
                _logger.LogWarning("Error al obtener detalle de activo {Symbol}: {Message}", symbol, result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Obtiene las tendencias actuales del mercado.
        /// </summary>
        /// <returns>
        /// Lista de activos con mayor actividad.
        /// </returns>
        /// <response code="200">
        /// Tendencias obtenidas correctamente.
        /// </response>
        /// <response code="400">
        /// Error al obtener las tendencias.
        /// </response>
        [HttpGet("trending")]
        [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetTrending()
        {
            _logger.LogInformation("Solicitud de tendencias del mercado recibida");

            var result = await _service.GetTrendingAsync();

            if (!result.Success)
            {
                _logger.LogWarning("Error al obtener tendencias del mercado: {Message}", result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Obtiene los ganadores del día.
        /// </summary>
        /// <returns>
        /// Lista de activos con mayores subas del día.
        /// </returns>
        /// <response code="200">
        /// Ganadores obtenidos correctamente.
        /// </response>
        /// <response code="400">
        /// Error al obtener los ganadores.
        /// </response>
        [HttpGet("gainers")]
        [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetGainers()
        {
            _logger.LogInformation("Solicitud de ganadores del día recibida");

            var result = await _service.GetGainersAsync();

            if (!result.Success)
            {
                _logger.LogWarning("Error al obtener ganadores del día: {Message}", result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Obtiene los perdedores del día.
        /// </summary>
        /// <returns>
        /// Lista de activos con mayores bajas del día.
        /// </returns>
        /// <response code="200">
        /// Perdedores obtenidos correctamente.
        /// </response>
        /// <response code="400">
        /// Error al obtener los perdedores.
        /// </response>
        [HttpGet("losers")]
        [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetLosers()
        {
            _logger.LogInformation("Solicitud de perdedores del día recibida");

            var result = await _service.GetLosersAsync();

            if (!result.Success)
            {
                _logger.LogWarning("Error al obtener perdedores del día: {Message}", result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Obtiene las noticias principales del mercado.
        /// </summary>
        /// <returns>
        /// Lista de noticias financieras destacadas.
        /// </returns>
        /// <response code="200">
        /// Noticias obtenidas correctamente.
        /// </response>
        /// <response code="400">
        /// Error al obtener las noticias del mercado.
        /// </response>
        [HttpGet("news")]
        [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMarketNews()
        {
            _logger.LogInformation("Solicitud de noticias del mercado recibida");

            var result = await _service.GetMarketNewsAsync();

            if (!result.Success)
            {
                _logger.LogWarning("Error al obtener noticias del mercado: {Message}", result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Obtiene el estado de actualización de los precios de mercado.
        /// </summary>
        /// <returns>
        /// Fecha de última actualización del cache de precios.
        /// </returns>
        /// <response code="200">
        /// Estado de actualización obtenido correctamente.
        /// </response>
        /// <response code="400">
        /// Error al obtener el estado de actualización.
        /// </response>
        [HttpGet("prices-status")]
        [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMarketPriceStatus()
        {
            _logger.LogInformation("Solicitud de estado de actualización de precios recibida");

            var result = await _service.GetMarketPriceStatusAsync();

            if (!result.Success)
            {
                _logger.LogWarning("Error al obtener estado de actualización de precios: {Message}", result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Obtiene el histórico de los índices usados para comparar en el gráfico.
        /// </summary>
        /// <param name="range">
        /// Rango del gráfico. Valores permitidos: 1d, 1w, 1m, 3m, 6m, 1y.
        /// </param>
        /// <returns>
        /// Series históricas de S&P 500, NASDAQ y Dow Jones.
        /// </returns>
        /// <response code="200">
        /// Histórico de comparación obtenido correctamente.
        /// </response>
        /// <response code="400">
        /// Error al obtener el histórico de comparación.
        /// </response>
        [HttpGet("comparison-history")]
        [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetComparisonHistory([FromQuery] string range = "1m")
        {
            _logger.LogInformation("Solicitud de histórico de comparación recibida con rango {Range}", range);

            var result = await _service.GetComparisonHistoryAsync(range);

            if (!result.Success)
            {
                _logger.LogWarning("Error al obtener histórico de comparación: {Message}", result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Obtiene el histórico de un activo para el gráfico.
        /// </summary>
        /// <param name="symbol">
        /// Símbolo del activo a consultar.
        /// </param>
        /// <param name="range">
        /// Rango del gráfico. Valores permitidos: 1d, 1w, 1m, 3m, 6m, 1y.
        /// </param>
        /// <returns>
        /// Serie histórica del activo solicitado.
        /// </returns>
        /// <response code="200">
        /// Histórico del activo obtenido correctamente.
        /// </response>
        /// <response code="400">
        /// Error al obtener el histórico del activo.
        /// </response>
        [HttpGet("asset/{symbol}/history")]
        [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAssetHistory(string symbol, [FromQuery] string range = "1m")
        {
            _logger.LogInformation("Solicitud de histórico de activo recibida para {Symbol} con rango {Range}", symbol, range);

            var result = await _service.GetAssetHistoryAsync(symbol, range);

            if (!result.Success)
            {
                _logger.LogWarning("Error al obtener histórico de activo {Symbol}: {Message}", symbol, result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }

    }
}