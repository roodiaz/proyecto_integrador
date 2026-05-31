using InvestLab.Business.Interfaces.Api;
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
    /// <returns>
    /// Valor total del portfolio,
    /// ganancia diaria y cantidad de activos.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpGet("top-cards")]
    public async Task<IActionResult> GetTopCards()
    {
        _logger.LogInformation("Consultando dashboard top cards: UserId={UserId}", UserId);

        var result = await _service.GetTopCardsAsync(UserId);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener dashboard top cards: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene los activos con mejor rendimiento
    /// del portfolio del usuario autenticado.
    /// </summary>
    /// <returns>
    /// Top de activos ordenados por rendimiento.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpGet("top-assets")]
    public async Task<IActionResult> GetTopAssets()
    {
        _logger.LogInformation("Consultando dashboard top assets: UserId={UserId}", UserId);

        var result = await _service.GetTopAssetsAsync(UserId);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener dashboard top assets: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene las alertas activas
    /// del usuario autenticado.
    /// </summary>
    /// <returns>
    /// Últimas alertas activas configuradas.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpGet("active-alerts")]
    public async Task<IActionResult> GetActiveAlerts()
    {
        _logger.LogInformation("Consultando dashboard active alerts: UserId={UserId}", UserId);

        var result = await _service.GetActiveAlertsAsync(UserId);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener dashboard active alerts: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene la evolución comparativa entre el portfolio
    /// del usuario autenticado y el índice S&P 500.
    /// </summary>
    /// <param name="filter">
    /// Período del gráfico:
    /// 1W, 1M, 3M o 1Y.
    /// </param>
    /// <returns>
    /// Serie temporal normalizada del portfolio
    /// y benchmark junto con el resumen del período.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="400">Error en la consulta</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpPost("performance-chart")]
    public async Task<IActionResult> GetPerformanceChart([FromBody] DashboardPerformanceChartFilterDto filter)
    {
        _logger.LogInformation("Consultando performance chart para UserId={UserId}", UserId);

        var result = await _service.GetPerformanceChartAsync(UserId, filter);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener performance chart: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene las últimas operaciones realizadas
    /// por el usuario autenticado.
    /// </summary>
    /// <returns>
    /// Últimas transacciones registradas en el portfolio.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="400">Error en la consulta</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpGet("latest-transactions")]
    public async Task<IActionResult> GetLatestTransactions()
    {
        _logger.LogInformation("Consultando últimas operaciones para UserId={UserId}", UserId);

        var result = await _service.GetLatestTransactionsAsync(UserId);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener últimas operaciones: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }
}