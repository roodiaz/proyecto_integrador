using InvestLab.Business.Interfaces.Api;
using InvestLab.Models;
using InvestLab.Models.DTOs.Favorite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestLab.Api.Controllers
{
    /// <summary>
    /// Controlador encargado de la gestión de la lista de favoritos del usuario.
    /// Permite agregar, eliminar, listar con paginación y consultar la cantidad de favoritos.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Response), StatusCodes.Status401Unauthorized)]
    public class FavoriteController : BaseController
    {
        private readonly IFavoriteService _service;
        private readonly ILogger<FavoriteController> _logger;

        public FavoriteController(IFavoriteService service, ILogger<FavoriteController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene la lista de favoritos del usuario autenticado con paginación,
        /// incluyendo información de mercado en tiempo real.
        /// </summary>
        /// <param name="filter">Parámetros de paginación (página y tamaño de página)</param>
        /// <returns>Listado paginado de favoritos con precio actual y variación</returns>
        /// <response code="200">Listado obtenido correctamente</response>
        /// <response code="400">Error en la consulta</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpPost("list")]
        [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Get([FromBody] FavoriteFilterDto filter)
        {
            _logger.LogInformation("Listando favoritos para usuario {UserId}", UserId);

            var result = await _service.GetAsync(UserId, filter);

            if (!result.Success)
            {
                _logger.LogWarning("Error al obtener favoritos: {Message}", result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Agrega un activo a la lista de favoritos del usuario.
        /// Valida que el activo exista, que no esté duplicado y que no supere el límite permitido.
        /// </summary>
        /// <param name="dto">Datos del activo a agregar (símbolo/ticker)</param>
        /// <returns>Resultado de la operación</returns>
        /// <response code="200">Activo agregado correctamente</response>
        /// <response code="400">Error de validación (activo inexistente, duplicado o límite alcanzado)</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpPost("add")]
        [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Add([FromBody] AddFavoriteDto dto)
        {
            _logger.LogInformation("Agregando favorito {Symbol} para usuario {UserId}", dto.Symbol, UserId);

            var result = await _service.AddAsync(UserId, dto);

            if (!result.Success)
            {
                _logger.LogWarning("Error al agregar favorito: {Message}", result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Elimina un activo de la lista de favoritos del usuario.
        /// </summary>
        /// <param name="symbol">Símbolo (ticker) del activo a eliminar</param>
        /// <returns>Resultado de la operación</returns>
        /// <response code="200">Activo eliminado correctamente</response>
        /// <response code="400">Activo no encontrado en favoritos</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpDelete("{symbol}")]
        [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Remove(string symbol)
        {
            _logger.LogInformation("Eliminando favorito {Symbol} para usuario {UserId}", symbol, UserId);

            var result = await _service.RemoveAsync(UserId, symbol);

            if (!result.Success)
            {
                _logger.LogWarning("Error al eliminar favorito: {Message}", result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Indica si un activo ya se encuentra en la lista de favoritos del usuario autenticado.
        /// </summary>
        /// <param name="symbol">Símbolo/ticker del activo</param>
        /// <returns>True si el activo está en favoritos, false en caso contrario</returns>
        /// <response code="200">Consulta realizada correctamente</response>
        /// <response code="400">Error en la consulta</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpGet("exists/{symbol}")]
        [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Exists(string symbol)
        {
            _logger.LogInformation("Consultando si el activo {Symbol} está en favoritos para usuario {UserId}", symbol, UserId);

            var result = await _service.ExistsAsync(UserId, symbol);

            if (!result.Success)
            {
                _logger.LogWarning("Error al consultar favorito: {Message}", result.Message);
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
