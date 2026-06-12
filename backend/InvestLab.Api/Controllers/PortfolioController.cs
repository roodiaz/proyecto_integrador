using InvestLab.Business.Interfaces.Api;
using InvestLab.Models.DTOs.Portfolio;
using InvestLab.Models.DTOs.Transaction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestLab.Api.Controllers;

/// <summary>
/// Controlador encargado de la gestión
/// de portfolios y operaciones.
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
    /// Obtiene los portfolios del usuario autenticado.
    /// </summary>
    /// <returns>Lista de portfolios del usuario.</returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpGet]
    public async Task<IActionResult> GetUserPortfolios()
    {
        _logger.LogInformation("Consultando portfolios: UserId={UserId}", UserId);

        var result = await _portfolioService.GetUserPortfoliosAsync(UserId);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener portfolios: {Message}", result.Message);

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Crea un nuevo portfolio para el usuario autenticado.
    /// </summary>
    /// <param name="dto">Nombre de portfolio y saldo inicial elegidos por el usuario.</param>
    /// <returns>El portfolio creado.</returns>
    /// <response code="200">Portfolio creado correctamente</response>
    /// <response code="400">Error en la creación o límite de portfolios alcanzado</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpPost]
    public async Task<IActionResult> CreatePortfolio([FromBody] SetupPortfolioDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation("Creando portfolio: UserId={UserId}", UserId);

        var result = await _portfolioService.CreatePortfolioAsync(UserId, dto);

        if (!result.Success)
        {
            _logger.LogWarning("Error al crear portfolio: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Marca un portfolio del usuario autenticado como el portfolio activo.
    /// </summary>
    /// <param name="portfolioId">Identificador del portfolio a activar.</param>
    /// <returns>Resultado de la operación.</returns>
    /// <response code="200">Portfolio activado correctamente</response>
    /// <response code="400">Portfolio no encontrado</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpPost("{portfolioId:int}/activate")]
    public async Task<IActionResult> SetActivePortfolio(int portfolioId)
    {
        _logger.LogInformation("Activando portfolio: UserId={UserId}, PortfolioId={PortfolioId}", UserId, portfolioId);

        var result = await _portfolioService.SetActivePortfolioAsync(UserId, portfolioId);

        if (!result.Success)
        {
            _logger.LogWarning("Error al activar portfolio: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Elimina un portfolio del usuario autenticado, junto con sus posiciones,
    /// transacciones e historial. No permite eliminar el último portfolio.
    /// </summary>
    /// <param name="portfolioId">Identificador del portfolio a eliminar.</param>
    /// <returns>Resultado de la operación.</returns>
    /// <response code="200">Portfolio eliminado correctamente</response>
    /// <response code="400">Portfolio no encontrado o es el último del usuario</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpDelete("{portfolioId:int}")]
    public async Task<IActionResult> DeletePortfolio(int portfolioId)
    {
        _logger.LogInformation("Eliminando portfolio: UserId={UserId}, PortfolioId={PortfolioId}", UserId, portfolioId);

        var result = await _portfolioService.DeletePortfolioAsync(UserId, portfolioId);

        if (!result.Success)
        {
            _logger.LogWarning("Error al eliminar portfolio: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Reinicia un portfolio del usuario autenticado.
    /// </summary>
    /// <remarks>
    /// Elimina las tenencias actuales, el historial de operaciones simuladas
    /// y el historial de evolución del portfolio.
    /// Además, restablece el nombre y el saldo inicial del portfolio y reinicia el contador
    /// diario de operaciones utilizadas.
    ///
    /// No elimina favoritos, alertas, notificaciones ni preferencias del usuario.
    /// </remarks>
    /// <param name="portfolioId">Identificador del portfolio a reiniciar.</param>
    /// <param name="dto">Nuevo nombre de portfolio y saldo inicial elegidos por el usuario.</param>
    /// <returns>Resultado de la operación de reinicio.</returns>
    /// <response code="200">Portfolio reiniciado correctamente</response>
    /// <response code="400">Error al reiniciar el portfolio</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpPost("{portfolioId:int}/reset")]
    public async Task<IActionResult> ResetPortfolio(int portfolioId, [FromBody] SetupPortfolioDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation("Reiniciando portfolio: UserId={UserId}, PortfolioId={PortfolioId}", UserId, portfolioId);

        var result = await _portfolioService.ResetPortfolioAsync(UserId, portfolioId, dto);

        if (!result.Success)
        {
            _logger.LogWarning("Error al reiniciar portfolio: UserId={UserId}, PortfolioId={PortfolioId}, Message={Message}", UserId, portfolioId, result.Message);
            return BadRequest(result);
        }

        _logger.LogInformation("Portfolio reiniciado correctamente: UserId={UserId}, PortfolioId={PortfolioId}", UserId, portfolioId);

        return Ok(result);
    }

    /// <summary>
    /// Realiza una compra de activo utilizando
    /// el saldo disponible de un portfolio del usuario autenticado.
    /// </summary>
    /// <param name="portfolioId">Identificador del portfolio sobre el cual operar.</param>
    /// <param name="dto">Datos de compra del activo</param>
    /// <returns>Resultado de la operación</returns>
    /// <response code="200">Compra realizada correctamente</response>
    /// <response code="400">Error en la operación</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpPost("{portfolioId:int}/buy")]
    public async Task<IActionResult> Buy(int portfolioId, [FromBody] BuyAssetDto dto)
    {
        _logger.LogInformation("Compra solicitada por usuario {UserId}, PortfolioId={PortfolioId}", UserId, portfolioId);

        var result = await _portfolioService.BuyAsync(UserId, portfolioId, dto);

        if (!result.Success)
        {
            _logger.LogWarning("Error al comprar activo: {Message}", result.Message);

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Realiza una venta de activo de un portfolio
    /// del usuario autenticado.
    /// </summary>
    /// <param name="portfolioId">Identificador del portfolio sobre el cual operar.</param>
    /// <param name="dto">Datos de venta del activo</param>
    /// <returns>Resultado de la operación</returns>
    /// <response code="200">Venta realizada correctamente</response>
    /// <response code="400">Error en la operación</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpPost("{portfolioId:int}/sell")]
    public async Task<IActionResult> Sell(int portfolioId, [FromBody] SellAssetDto dto)
    {
        _logger.LogInformation("Venta solicitada por usuario {UserId}, PortfolioId={PortfolioId}", UserId, portfolioId);

        var result = await _portfolioService.SellAsync(UserId, portfolioId, dto);

        if (!result.Success)
        {
            _logger.LogWarning("Error al vender activo: {Message}", result.Message);

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene la posición actual de un activo
    /// dentro de un portfolio del usuario autenticado.
    /// </summary>
    /// <param name="portfolioId">Identificador del portfolio.</param>
    /// <param name="symbol">Nombre del activo</param>
    /// <returns>Información de la posición</returns>
    /// <response code="200">Posición obtenida correctamente</response>
    /// <response code="404">Posición no encontrada</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpGet("{portfolioId:int}/position/{symbol}")]
    public async Task<IActionResult> GetPosition(int portfolioId, string symbol)
    {
        _logger.LogInformation("Consultando posición: UserId={UserId}, PortfolioId={PortfolioId}, AssetId={AssetId}", UserId, portfolioId, symbol);

        var result = await _portfolioService.GetPositionForSellAsync(UserId, portfolioId, symbol);

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
    /// de un portfolio del usuario autenticado.
    /// </summary>
    /// <param name="portfolioId">Identificador del portfolio.</param>
    /// <returns>
    /// Saldo inicial, saldo actual,
    /// ganancia/pérdida y porcentaje de rendimiento.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpGet("{portfolioId:int}/balance-cards")]
    public async Task<IActionResult> GetBalanceCards(int portfolioId)
    {
        _logger.LogInformation("Consultando resumen portfolio: UserId={UserId}, PortfolioId={PortfolioId}", UserId, portfolioId);

        var result = await _portfolioService.GetBalanceCardsAsync(UserId, portfolioId);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener resumen portfolio: {Message}", result.Message);

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene la información necesaria para
    /// construir el gráfico de torta de un portfolio
    /// del usuario autenticado.
    /// </summary>
    /// <param name="portfolioId">Identificador del portfolio.</param>
    /// <returns>
    /// Lista de activos con porcentaje de participación
    /// y valor actual dentro del portfolio.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpGet("{portfolioId:int}/pie-chart")]
    public async Task<IActionResult> GetPieChart(int portfolioId)
    {
        _logger.LogInformation("Consultando pie chart portfolio: UserId={UserId}, PortfolioId={PortfolioId}", UserId, portfolioId);

        var result = await _portfolioService.GetPieChartAsync(UserId, portfolioId);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener pie chart portfolio: {Message}", result.Message);

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene las posiciones abiertas de un portfolio
    /// del usuario autenticado.
    /// </summary>
    /// <param name="portfolioId">Identificador del portfolio.</param>
    /// <param name="filter">
    /// Parámetros de paginación,
    /// filtrado y ordenamiento.
    /// </param>
    /// <returns>
    /// Lista paginada de posiciones abiertas.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpPost("{portfolioId:int}/open-positions")]
    public async Task<IActionResult> GetOpenPositions(int portfolioId, [FromBody] PortfolioOpenPositionsFilterDto filter)
    {
        _logger.LogInformation("Consultando open positions: UserId={UserId}, PortfolioId={PortfolioId}", UserId, portfolioId);

        var result = await _portfolioService.GetOpenPositionsAsync(UserId, portfolioId, filter);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener open positions: {Message}", result.Message);

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene la evolución histórica de un portfolio
    /// del usuario autenticado.
    /// </summary>
    /// <param name="portfolioId">Identificador del portfolio.</param>
    /// <param name="filter">
    /// Período de tiempo del gráfico.
    /// </param>
    /// <returns>
    /// Evolución histórica del valor total
    /// del portfolio.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpPost("{portfolioId:int}/line-chart")]
    public async Task<IActionResult> GetLineChart(int portfolioId, [FromBody] PortfolioLineChartFilterDto filter)
    {
        _logger.LogInformation("Consultando line chart portfolio: UserId={UserId}, PortfolioId={PortfolioId}", UserId, portfolioId);

        var result = await _portfolioService.GetLineChartAsync(UserId, portfolioId, filter);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener line chart portfolio: {Message}", result.Message);

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene el historial de operaciones
    /// (compras y ventas) de un portfolio del usuario autenticado.
    /// </summary>
    /// <param name="portfolioId">Identificador del portfolio.</param>
    /// <param name="filter">
    /// Parámetros de paginación,
    /// filtrado y ordenamiento.
    /// </param>
    /// <returns>
    /// Lista paginada de transacciones
    /// realizadas en el portfolio.
    /// </returns>
    /// <response code="200">Información obtenida correctamente</response>
    /// <response code="400">Error al obtener el historial de transacciones</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpPost("{portfolioId:int}/history")]
    public async Task<IActionResult> GetTransactionHistory(int portfolioId, [FromBody] TransactionFilterDto filter)
    {
        _logger.LogInformation("Consultando historial de transacciones: UserId={UserId}, PortfolioId={PortfolioId}", UserId, portfolioId);

        var result = await _transactionService.GetTransactionHistoryAsync(UserId, portfolioId, filter);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener historial de transacciones: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Genera y descarga un archivo Excel con las tenencias actuales de un portfolio del usuario.
    /// </summary>
    [HttpPost("{portfolioId:int}/export/holdings")]
    public async Task<IActionResult> ExportHoldings(int portfolioId, [FromBody] PortfolioOpenPositionsFilterDto filter)
    {
        _logger.LogInformation("Exportando tenencias a Excel: UserId={UserId}, PortfolioId={PortfolioId}", UserId, portfolioId);
        var bytes = await _portfolioService.ExportHoldingsToExcelAsync(UserId, portfolioId, filter);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "tenencias.xlsx");
    }

    /// <summary>
    /// Genera y descarga un archivo Excel con el historial de operaciones de un portfolio del usuario.
    /// </summary>
    [HttpPost("{portfolioId:int}/export/transactions")]
    public async Task<IActionResult> ExportTransactions(int portfolioId, [FromBody] TransactionFilterDto filter)
    {
        _logger.LogInformation("Exportando operaciones a Excel: UserId={UserId}, PortfolioId={PortfolioId}", UserId, portfolioId);
        var bytes = await _transactionService.ExportTransactionsToExcelAsync(UserId, portfolioId, filter);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "operaciones.xlsx");
    }
}
