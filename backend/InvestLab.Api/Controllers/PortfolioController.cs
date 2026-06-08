using InvestLab.Business.Interfaces.Api;
using InvestLab.Models.DTOs.Portfolio;
using InvestLab.Models.DTOs.Transaction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestLab.Api.Controllers;

/// <summary>
/// Controlador encargado de la gestión
/// de portfolio y operaciones.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfolioController : BaseController
{
    private readonly IPortfolioService _portfolioService;
    private readonly ITransactionService _transactionService;
    private readonly ILogger<PortfolioController> _logger;

    public PortfolioController(IPortfolioService portfolioService, ILogger<PortfolioController> logger, ITransactionService transactionService)
    {
        _portfolioService = portfolioService;
        _transactionService = transactionService;
        _logger = logger;
    }

    /// <summary>
    /// Realiza una compra de activo utilizando
    /// el saldo disponible del usuario autenticado.
    /// </summary>
    /// <param name="dto">Datos de compra del activo</param>
    /// <returns>Resultado de la operación</returns>
    /// <response code="200">Compra realizada correctamente</response>
    /// <response code="400">Error en la operación</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpPost("buy")]
    public async Task<IActionResult> Buy([FromBody] BuyAssetDto dto)
    {
        _logger.LogInformation("Compra solicitada por usuario {UserId}", UserId);

        var result = await _portfolioService.BuyAsync(UserId, dto);

        if (!result.Success)
        {
            _logger.LogWarning("Error al comprar activo: {Message}", result.Message);

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Realiza una venta de activo del portfolio
    /// del usuario autenticado.
    /// </summary>
    /// <param name="dto">Datos de venta del activo</param>
    /// <returns>Resultado de la operación</returns>
    /// <response code="200">Venta realizada correctamente</response>
    /// <response code="400">Error en la operación</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpPost("sell")]
    public async Task<IActionResult> Sell([FromBody] SellAssetDto dto)
    {
        _logger.LogInformation("Venta solicitada por usuario {UserId}", UserId);

        var result = await _portfolioService.SellAsync(UserId, dto);

        if (!result.Success)
        {
            _logger.LogWarning("Error al vender activo: {Message}", result.Message);

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene la posición actual de un activo
    /// dentro del portfolio del usuario autenticado.
    /// </summary>
    /// <param name="symbol">Nombre del activo</param>
    /// <returns>Información de la posición</returns>
    /// <response code="200">Posición obtenida correctamente</response>
    /// <response code="404">Posición no encontrada</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpGet("{symbol}")]
    public async Task<IActionResult> GetPosition(string symbol)
    {
        _logger.LogInformation("Consultando posición: UserId={UserId}, AssetId={AssetId}", UserId, symbol);

        var result = await _portfolioService.GetPositionForSellAsync(UserId, symbol);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener posición: {Message}", result.Message);

            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene el precio actual de mercado
    /// de un activo según su ticker.
    /// </summary>
    /// <param name="symbol">Ticker del activo</param>
    /// <returns>Precio actual del activo</returns>
    /// <response code="200">Precio obtenido correctamente</response>
    /// <response code="404">Activo no encontrado</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpGet("price/{symbol}")]
    public async Task<IActionResult> GetPrice(string symbol)
    {
        _logger.LogInformation("Consultando precio actual: {Symbol}", symbol);

        var result = await _portfolioService.GetPriceAsync(symbol);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener precio: {Message}", result.Message);

            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene la información de las cards principales
    /// del portfolio del usuario autenticado.
    /// </summary>
    /// <returns>
    /// Saldo inicial, saldo actual,
    /// ganancia/pérdida y porcentaje de rendimiento.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpGet("balance-cards")]
    public async Task<IActionResult> GetBalanceCards()
    {
        _logger.LogInformation("Consultando resumen portfolio: UserId={UserId}", UserId);

        var result = await _portfolioService.GetBalanceCardsAsync(UserId);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener resumen portfolio: {Message}", result.Message);

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene la información necesaria para
    /// construir el gráfico de torta del portfolio
    /// del usuario autenticado.
    /// </summary>
    /// <returns>
    /// Lista de activos con porcentaje de participación
    /// y valor actual dentro del portfolio.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpGet("pie-chart")]
    public async Task<IActionResult> GetPieChart()
    {
        _logger.LogInformation("Consultando pie chart portfolio: UserId={UserId}", UserId);

        var result = await _portfolioService.GetPieChartAsync(UserId);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener pie chart portfolio: {Message}", result.Message);

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene las posiciones abiertas del portfolio
    /// del usuario autenticado.
    /// </summary>
    /// <param name="filter">
    /// Parámetros de paginación,
    /// filtrado y ordenamiento.
    /// </param>
    /// <returns>
    /// Lista paginada de posiciones abiertas.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpPost("open-positions")]
    public async Task<IActionResult> GetOpenPositions([FromBody] PortfolioOpenPositionsFilterDto filter)
    {
        _logger.LogInformation("Consultando open positions: UserId={UserId}", UserId);

        var result = await _portfolioService.GetOpenPositionsAsync(UserId, filter);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener open positions: {Message}", result.Message);

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene la evolución histórica del portfolio
    /// del usuario autenticado.
    /// </summary>
    /// <param name="filter">
    /// Período de tiempo del gráfico.
    /// </param>
    /// <returns>
    /// Evolución histórica del valor total
    /// del portfolio.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpPost("line-chart")]
    public async Task<IActionResult> GetLineChart([FromBody] PortfolioLineChartFilterDto filter)
    {
        _logger.LogInformation("Consultando line chart portfolio: UserId={UserId}", UserId);

        var result = await _portfolioService.GetLineChartAsync(UserId, filter);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener line chart portfolio: {Message}", result.Message);

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene el historial de operaciones
    /// (compras y ventas) del usuario autenticado.
    /// </summary>
    /// <param name="filter">
    /// Parámetros de paginación,
    /// filtrado y ordenamiento.
    /// </param>
    /// <returns>
    /// Lista paginada de transacciones
    /// realizadas por el usuario.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="400">Error al obtener el historial de transacciones</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpPost("history")]
    public async Task<IActionResult> GetTransactionHistory( [FromBody] TransactionFilterDto filter)
    {
        _logger.LogInformation(  "Consultando historial de transacciones: UserId={UserId}",     UserId);

        var result = await _transactionService.GetTransactionHistoryAsync( UserId, filter);

        if (!result.Success)
        {
            _logger.LogWarning(   "Error al obtener historial de transacciones: {Message}",      result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Genera y descarga un archivo Excel con las tenencias actuales del usuario.
    /// </summary>
    [HttpPost("export/holdings")]
    public async Task<IActionResult> ExportHoldings([FromBody] PortfolioOpenPositionsFilterDto filter)
    {
        _logger.LogInformation("Exportando tenencias a Excel: UserId={UserId}", UserId);
        var bytes = await _portfolioService.ExportHoldingsToExcelAsync(UserId, filter);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "tenencias.xlsx");
    }

    /// <summary>
    /// Genera y descarga un archivo Excel con el historial de operaciones del usuario.
    /// </summary>
    [HttpPost("export/transactions")]
    public async Task<IActionResult> ExportTransactions([FromBody] TransactionFilterDto filter)
    {
        _logger.LogInformation("Exportando operaciones a Excel: UserId={UserId}", UserId);
        var bytes = await _transactionService.ExportTransactionsToExcelAsync(UserId, filter);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "operaciones.xlsx");
    }

    /// <summary>
    /// Reinicia la simulación del portfolio del usuario autenticado.
    /// </summary>
    /// <remarks>
    /// Elimina las tenencias actuales, el historial de operaciones simuladas
    /// y el historial de evolución del portfolio.
    /// Además, restablece el balance virtual inicial y reinicia el contador
    /// diario de operaciones utilizadas.
    /// 
    /// No elimina favoritos, alertas, notificaciones ni preferencias del usuario.
    /// </remarks>
    /// <returns>
    /// Resultado de la operación de reinicio.
    /// </returns>
    /// <response code="200">Portfolio reiniciado correctamente</response>
    /// <response code="400">Error al reiniciar el portfolio</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpPost("reset-simulation")]
    public async Task<IActionResult> ResetSimulation()
    {
        _logger.LogInformation("Reiniciando simulación de portfolio: UserId={UserId}", UserId);

        var result = await _portfolioService.ResetSimulationAsync(UserId);

        if (!result.Success)
        {
            _logger.LogWarning("Error al reiniciar simulación de portfolio: UserId={UserId}, Message={Message}", UserId, result.Message);
            return BadRequest(result);
        }

        _logger.LogInformation("Simulación de portfolio reiniciada correctamente: UserId={UserId}", UserId);

        return Ok(result);
    }
}