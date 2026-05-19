using InvestLab.Business.Interfaces.Api;
using InvestLab.Models.DTOs.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InvestLab.Api.Controllers
{

    /// <summary>
    /// Controlador encargado de la gestión de notificaciones generadas por alertas.
    /// Permite listar, marcar como leídas, eliminar y consultar notificaciones no leídas.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : BaseController
    {
        private readonly INotificationService _service;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(INotificationService service, ILogger<NotificationController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene las notificaciones del usuario autenticado con paginación y filtros.
        /// </summary>
        /// <param name="filter">Parámetros de paginación y filtro (leídas/no leídas)</param>
        /// <returns>Listado de notificaciones</returns>
        /// <response code="200">Listado obtenido correctamente</response>
        /// <response code="400">Error en la consulta</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpPost("search")]
        public async Task<IActionResult> Get([FromBody] NotificationFilterDto filter)
        {
            _logger.LogInformation("Listando notificaciones para usuario {UserId}", UserId);

            var result = await _service.GetAsync(UserId, filter);

            if (!result.Success)
            {
                _logger.LogWarning("Error al obtener notificaciones: {Message}", result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Marca una notificación específica como leída.
        /// </summary>
        /// <param name="id">Identificador de la notificación</param>
        /// <returns>Resultado de la operación</returns>
        /// <response code="200">Notificación marcada como leída</response>
        /// <response code="400">Notificación no encontrada o error</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            _logger.LogInformation("Marcando notificación {NotificationId} como leída para usuario {UserId}", id, UserId);

            var result = await _service.MarkAsReadAsync(UserId, id);

            if (!result.Success)
            {
                _logger.LogWarning("Error al marcar notificación: {Message}", result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Marca todas las notificaciones del usuario como leídas.
        /// </summary>
        /// <returns>Resultado de la operación</returns>
        /// <response code="200">Notificaciones marcadas correctamente</response>
        /// <response code="400">Error en la operación</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAll()
        {
            _logger.LogInformation("Marcando todas las notificaciones como leídas para usuario {UserId}", UserId);

            var result = await _service.MarkAllAsReadAsync(UserId);

            if (!result.Success)
            {
                _logger.LogWarning("Error al marcar todas las notificaciones: {Message}", result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Elimina una notificación del usuario.
        /// </summary>
        /// <param name="id">Identificador de la notificación</param>
        /// <returns>Resultado de la operación</returns>
        /// <response code="200">Notificación eliminada correctamente</response>
        /// <response code="400">Notificación no encontrada o error</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Eliminando notificación {NotificationId} para usuario {UserId}", id, UserId);

            var result = await _service.DeleteAsync(UserId, id);

            if (!result.Success)
            {
                _logger.LogWarning("Error al eliminar notificación: {Message}", result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Obtiene la cantidad de notificaciones no leídas del usuario.
        /// </summary>
        /// <returns>Cantidad de notificaciones no leídas</returns>
        /// <response code="200">Cantidad obtenida correctamente</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnread()
        {
            _logger.LogInformation("Obteniendo cantidad de notificaciones no leídas para usuario {UserId}", UserId);

            var result = await _service.GetUnreadCountAsync(UserId);

            return Ok(result);
        }
    }
}
