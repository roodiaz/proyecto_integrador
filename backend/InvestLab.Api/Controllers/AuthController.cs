using InvestLab.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    //[HttpPost("register")]
    //public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto registerDto)
    //{
    //    try
    //    {
    //        var result = await _authService.RegisterAsync(registerDto);

    //        if (!result.Success)
    //        {
    //            return BadRequest(result);
    //        }

    //        return Ok(result);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error en el registro de usuario");
    //        return StatusCode(500, new AuthResponseDto
    //        {
    //            Success = false,
    //            Message = "Error interno del servidor"
    //        });
    //    }
    //}

    //[HttpPost("login")]
    //public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto loginDto)
    //{
    //    try
    //    {
    //        var result = await _authService.LoginAsync(loginDto);

    //        if (!result.Success)
    //        {
    //            return BadRequest(result);
    //        }

    //        return Ok(result);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error en el login de usuario");
    //        return StatusCode(500, new AuthResponseDto
    //        {
    //            Success = false,
    //            Message = "Error interno del servidor"
    //        });
    //    }
    //}

    ////[HttpPost("refresh")]
    ////public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    ////{
    ////    try
    ////    {
    ////        var result = await _authService.RefreshTokenAsync(refreshTokenDto.RefreshToken);

    ////        if (!result.Success)
    ////        {
    ////            return BadRequest(result);
    ////        }

    ////        return Ok(result);
    ////    }
    ////    catch (Exception ex)
    ////    {
    ////        _logger.LogError(ex, "Error al refrescar token");
    ////        return StatusCode(500, new AuthResponseDto
    ////        {
    ////            Success = false,
    ////            Message = "Error interno del servidor"
    ////        });
    ////    }
    ////}

    //[HttpPost("logout")]
    //[Authorize]
    //public async Task<ActionResult> Logout()
    //{
    //    try
    //    {
    //        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    //        if (int.TryParse(userId, out int userIdInt))
    //        {
    //            await _authService.LogoutAsync(userIdInt);
    //        }

    //        return Ok(new { message = "Logout exitoso" });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error en el logout");
    //        return StatusCode(500, new { message = "Error interno del servidor" });
    //    }
    //}

    //[HttpGet("me")]
    //[Authorize]
    //public async Task<ActionResult<UserDto>> GetCurrentUser()
    //{
    //    try
    //    {
    //        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    //        if (int.TryParse(userId, out int userIdInt))
    //        {
    //            var user = await _authService.GetUserByIdAsync(userIdInt);
    //            if (user == null)
    //            {
    //                return NotFound(new { message = "Usuario no encontrado" });
    //            }

    //            return Ok(user);
    //        }

    //        return BadRequest(new { message = "Token inválido" });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error al obtener usuario actual");
    //        return StatusCode(500, new { message = "Error interno del servidor" });
    //    }
    //}
}