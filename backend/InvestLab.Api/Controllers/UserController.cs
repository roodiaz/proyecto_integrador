using InvestLab.Business.Interfaces;
using InvestLab.Models.DTOs.Auth;
using InvestLab.Models.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controlador encargado de la gestión del perfil del usuario autenticado.
/// Permite consultar, actualizar datos personales, cambiar contraseña y gestionar la imagen de perfil.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : BaseController
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene la información del perfil del usuario autenticado.
    /// </summary>
    /// <returns>Datos del perfil del usuario</returns>
    /// <response code="200">Perfil obtenido correctamente</response>
    /// <response code="400">Error al obtener el perfil</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        _logger.LogInformation("Obteniendo perfil para usuario {UserId}", UserId);

        var result = await _userService.GetProfileAsync(UserId);

        if (!result.Success)
        {
            _logger.LogWarning("Error al obtener perfil: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Actualiza la información del perfil del usuario autenticado.
    /// </summary>
    /// <param name="dto">Datos actualizados del usuario (nombre, teléfono, preferencias, etc.)</param>
    /// <returns>Resultado de la operación</returns>
    /// <response code="200">Perfil actualizado correctamente</response>
    /// <response code="400">Datos inválidos o error de negocio</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation("Actualizando perfil para usuario {UserId}", UserId);

        var result = await _userService.UpdateProfileAsync(UserId, dto);

        if (!result.Success)
        {
            _logger.LogWarning("Error al actualizar perfil: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Permite cambiar la contraseña del usuario autenticado.
    /// </summary>
    /// <param name="dto">Datos necesarios para el cambio de contraseña (contraseña actual, nueva y confirmación)</param>
    /// <returns>Resultado de la operación</returns>
    /// <response code="200">Contraseña actualizada correctamente</response>
    /// <response code="400">Datos inválidos o contraseña incorrecta</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation("Cambio de contraseña para usuario {UserId}", UserId);

        var result = await _userService.ChangePasswordAsync(UserId, dto);

        if (!result.Success)
        {
            _logger.LogWarning("Error al cambiar contraseña: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Permite subir o actualizar la imagen de perfil del usuario autenticado.
    /// </summary>
    /// <param name="file">Archivo de imagen a subir</param>
    /// <returns>Resultado de la operación</returns>
    /// <response code="200">Imagen subida correctamente</response>
    /// <response code="400">Error al subir la imagen</response>
    /// <response code="401">Usuario no autenticado</response>
    [HttpPost("profile-image")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        _logger.LogInformation("Subiendo imagen de perfil para usuario {UserId}", UserId);

        var result = await _userService.UploadProfileImageAsync(UserId, file);

        if (!result.Success)
        {
            _logger.LogWarning("Error al subir imagen: {Message}", result.Message);
            return BadRequest(result);
        }

        return Ok(result);
    }
}