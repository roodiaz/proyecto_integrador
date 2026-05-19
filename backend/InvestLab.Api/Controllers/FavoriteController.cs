using InvestLab.Business.Interfaces;
using InvestLab.Models.DTOs.Favorite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestLab.Api.Controllers
{
    /// <summary>
    /// Controlador encargado de la gestión de la lista de favoritos del usuario.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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
        /// Obtiene la lista de favoritos del usuario.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            _logger.LogInformation("Obteniendo favoritos para usuario {UserId}", UserId);

            var result = await _service.GetAsync(UserId);

            return Ok(result);
        }

        /// <summary>
        /// Agrega un activo a la lista de favoritos.
        /// </summary>
        /// <param name="dto">Símbolo del activo</param>
        [HttpPost]
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
        /// Elimina un activo de la lista de favoritos.
        /// </summary>
        /// <param name="symbol">Ticker del activo</param>
        [HttpDelete("{symbol}")]
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
        /// Obtiene la cantidad de favoritos del usuario.
        /// </summary>
        [HttpGet("count")]
        public async Task<IActionResult> Count()
        {
            var result = await _service.GetCountAsync(UserId);
            return Ok(result);
        }
    }
}
