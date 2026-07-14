using InvestLab.Business.Interfaces.Api;
using InvestLab.Models;
using InvestLab.Models.DTOs.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestLab.Api.Controllers;

/// <summary>
/// Controlador encargado de la gestión
/// del dashboard principal.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(Response), StatusCodes.Status401Unauthorized)]
public class DashboardController : BaseController
{
    private readonly IDashboardService _service;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService service, ILogger<DashboardController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene la información de las cards
    /// principales del dashboard del usuario autenticado.
    /// </summary>
    /// <param name="portfolioId">Identificador del portfolio activo.</param>
    /// <returns>
    /// Valor total del portfolio,
    /// ganancia diaria y cantidad de activos.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpGet("top-cards")]
    [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTopCards([FromQuery] int portfolioId)
    {
        _logger.LogInformation("Consultando dashboard top cards: UserId={UserId}, PortfolioId={PortfolioId}", UserId, portfolioId);

        var result = await _service.GetTopCardsAsync(UserId, portfolioId);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener dashboard top cards: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene la distribución del portfolio
    /// del usuario autenticado agrupada por sector.
    /// </summary>
    /// <param name="portfolioId">Identificador del portfolio activo.</param>
    /// <returns>
    /// Distribución porcentual del portfolio por sector.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpGet("portfolio-distribution")]
    [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPortfolioDistribution([FromQuery] int portfolioId)
    {
        _logger.LogInformation("Consultando dashboard portfolio distribution: UserId={UserId}, PortfolioId={PortfolioId}", UserId, portfolioId);

        var result = await _service.GetPortfolioDistributionAsync(UserId, portfolioId);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener dashboard portfolio distribution: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene las últimas notificaciones generadas
    /// por alertas disparadas del usuario autenticado.
    /// </summary>
    /// <returns>
    /// Últimas notificaciones registradas.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpGet("recent-notifications")]
    [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRecentNotifications()
    {
        _logger.LogInformation("Consultando dashboard recent notifications: UserId={UserId}", UserId);

        var result = await _service.GetRecentNotificationsAsync(UserId);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener dashboard recent notifications: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene la evolución comparativa entre el portfolio
    /// del usuario autenticado, el índice S&P 500 y NASDAQ.
    /// </summary>
    /// <param name="portfolioId">Identificador del portfolio activo.</param>
    /// <param name="filter">
    /// Período del gráfico:
    /// 1W, 1M, 3M o 1Y.
    /// </param>
    /// <returns>
    /// Serie temporal normalizada del portfolio
    /// y benchmarks junto con el resumen del período.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="400">Error en la consulta</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpPost("performance-chart")]
    [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPerformanceChart([FromQuery] int portfolioId, [FromBody] DashboardPerformanceChartFilterDto filter)
    {
        _logger.LogInformation("Consultando performance chart para UserId={UserId}, PortfolioId={PortfolioId}", UserId, portfolioId);

        var result = await _service.GetPerformanceChartAsync(UserId, portfolioId, filter);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener performance chart: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene la composición histórica del portfolio del usuario autenticado para una fecha determinada.
    /// Si no existe snapshot exacto para esa fecha, retorna el snapshot más reciente anterior.
    /// </summary>
    /// <param name="portfolioId">Identificador del portfolio activo.</param>
    /// <param name="date">Fecha de referencia en formato yyyy-MM-dd.</param>
    /// <returns>Efectivo disponible, capital invertido y valor total junto con la fecha efectiva del snapshot.</returns>
    /// <response code="200">Composición obtenida correctamente</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpGet("portfolio-composition")]
    [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPortfolioComposition([FromQuery] int portfolioId, [FromQuery] DateTime date)
    {
        _logger.LogInformation("Consultando composición del portfolio: UserId={UserId}, PortfolioId={PortfolioId}, Date={Date}", UserId, portfolioId, date);

        var result = await _service.GetPortfolioCompositionAsync(UserId, portfolioId, date);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener composición del portfolio: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene las últimas operaciones realizadas
    /// por el usuario autenticado.
    /// </summary>
    /// <param name="portfolioId">Identificador del portfolio activo.</param>
    /// <returns>
    /// Últimas transacciones registradas en el portfolio.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="400">Error en la consulta</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpGet("latest-transactions")]
    [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLatestTransactions([FromQuery] int portfolioId)
    {
        _logger.LogInformation("Consultando últimas operaciones para UserId={UserId}, PortfolioId={PortfolioId}", UserId, portfolioId);

        var result = await _service.GetLatestTransactionsAsync(UserId, portfolioId);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener últimas operaciones: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }
}
