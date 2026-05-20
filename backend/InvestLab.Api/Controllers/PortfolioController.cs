using InvestLab.Business.Interfaces.Api;
using InvestLab.Models.DTOs.Portfolio;
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
    private readonly IPortfolioService _service;
    private readonly ILogger<PortfolioController> _logger;

    public PortfolioController(IPortfolioService service, ILogger<PortfolioController> logger)
    {
        _service = service;
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

        var result = await _service.BuyAsync(UserId, dto);

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

        var result = await _service.SellAsync(UserId, dto);

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

        var result = await _service.GetPositionForSellAsync(UserId, symbol);

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

        var result = await _service.GetPriceAsync(symbol);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener precio: {Message}", result.Message);

            return NotFound(result);
        }

        return Ok(result);
    }
}