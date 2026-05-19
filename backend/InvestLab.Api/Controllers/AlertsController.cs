using InvestLab.Business.Interfaces.Api;
using InvestLab.Models.DTOs.Alerts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controlador encargado de la gestión de alertas del usuario autenticado.
/// Permite crear, actualizar, eliminar, activar/desactivar y consultar alertas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlertController : BaseController
{
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertController> _logger;

    public AlertController(IAlertService alertService, ILogger<AlertController> logger)
    {
        _alertService = alertService;
        _logger = logger;
    }

    /// <summary>
    /// Crea una nueva alerta para el usuario autenticado.
    /// </summary>
    /// <param name="dto">Datos de la alerta a crear</param>
    /// <returns>Resultado de la operación</returns>
    /// <response code="200">Alerta creada correctamente</response>
    /// <response code="400">Datos inválidos o error de negocio</response>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAlertDto dto)
    {
        var userId = UserId;

        _logger.LogInformation("Creando alerta para usuario {UserId}", userId);

        var result = await _alertService.CreateAlertAsync(userId, dto);

        if (!result.Success)
        {
            _logger.LogWarning("Error al crear alerta: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Elimina una alerta existente del usuario.
    /// </summary>
    /// <param name="id">Identificador de la alerta</param>
    /// <returns>Resultado de la operación</returns>
    /// <response code="200">Alerta eliminada correctamente</response>
    /// <response code="400">Alerta no encontrada o error</response>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = UserId;

        _logger.LogInformation("Eliminando alerta {AlertId} del usuario {UserId}", id, userId);

        var result = await _alertService.DeleteAlertAsync(userId, id);

        if (!result.Success)
        {
            _logger.LogWarning("Error al eliminar alerta: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Actualiza una alerta existente del usuario.
    /// </summary>
    /// <param name="dto">Datos actualizados de la alerta</param>
    /// <returns>Resultado de la operación</returns>
    /// <response code="200">Alerta actualizada correctamente</response>
    /// <response code="400">Datos inválidos o error</response>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateAlertDto dto)
    {
        var userId = UserId;

        _logger.LogInformation("Actualizando alerta {AlertId} para usuario {UserId}", dto.Id, userId);

        var result = await _alertService.UpdateAlertAsync(userId, dto);

        if (!result.Success)
        {
            _logger.LogWarning("Error al actualizar alerta: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Activa o desactiva una alerta del usuario.
    /// </summary>
    /// <param name="id">Identificador de la alerta</param>
    /// <returns>Resultado de la operación</returns>
    /// <response code="200">Estado actualizado correctamente</response>
    /// <response code="400">Error en la operación</response>
    [HttpPatch("{id}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var userId = UserId;

        _logger.LogInformation("Toggle alerta {AlertId} para usuario {UserId}", id, userId);

        var result = await _alertService.ToggleAlertAsync(userId, id);

        if (!result.Success)
        {
            _logger.LogWarning("Error al toggle alerta: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene el listado de alertas del usuario con filtros y paginación.
    /// </summary>
    /// <param name="filter">Parámetros de filtrado y paginación</param>
    /// <returns>Listado de alertas</returns>
    /// <response code="200">Listado obtenido correctamente</response>
    /// <response code="400">Error en la consulta</response>
    [HttpPost("search")]
    public async Task<IActionResult> Get([FromBody] AlertFilterDto filter)
    {
        var userId = UserId;

        _logger.LogInformation("Listando alertas para usuario {UserId}", userId);

        var result = await _alertService.GetAlertsAsync(userId, filter);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener alertas: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtiene estadísticas de las alertas del usuario.
    /// </summary>
    /// <returns>Datos de alertas activas, pausadas, disparadas y total</returns>
    /// <response code="200">Estadísticas obtenidas correctamente</response>
    /// <response code="400">Error en la consulta</response>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var userId = UserId;

        _logger.LogInformation("Obteniendo stats de alertas para usuario {UserId}", userId);

        var result = await _alertService.GetStatsAsync(userId);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener stats: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }
}