using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiResponse = InvestLab.Models.Response;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    // Endpoint para registrar un nuevo usuario
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            var result = await _authService.RegisterAsync(registerDto);

            if (!result.Success)
            {
                _logger.LogWarning("Register fallido para {Email}: {Message}", registerDto.Email, result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("Register exitoso para {Email}", registerDto.Email);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en endpoint Register para {Email}", registerDto.Email);

            return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
        }
    }

    // Endpoint para verificar el correo electrónico del usuario
    [AllowAnonymous]
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyDto verifyDto)
    {
        try
        {
            var result = await _authService.VerifyAsync(verifyDto);

            if (!result.Success)
            {
                _logger.LogWarning("Verify fallido para {Email}: {Message}", verifyDto.Email, result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("Usuario verificado correctamente: {Email}", verifyDto.Email);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en endpoint Verify para {Email}", verifyDto.Email);

            return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
        }
    }

    // Endpoint para reenviar el código de verificación
    [AllowAnonymous]
    [HttpPost("resend-code")]
    public async Task<IActionResult> ResendCode([FromBody] ResendCodeDto dto)
    {
        try
        {
            var result = await _authService.ResendCodeAsync(dto);

            if (!result.Success)
            {
                _logger.LogWarning("ResendCode fallido para {Email}: {Message}", dto.Email, result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("Código reenviado correctamente a {Email}", dto.Email);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en endpoint ResendCode para {Email}", dto.Email);
            return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
        }
    }

    // Endpoint para iniciar sesión
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto);

            if (!result.Success)
            {
                _logger.LogWarning("Login fallido para {Email}: {Message}", dto.Email, result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("Login exitoso para {Email}", dto.Email);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en endpoint Login para {Email}", dto.Email);
            return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
        }
    }

    // Endpoint para refrescar el token
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(dto.RefreshToken);

            if (!result.Success)
            {
                _logger.LogWarning("Refresh fallido: {Message}", result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("Refresh exitoso");

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en endpoint Refresh");
            return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
        }
    }

    // Endpoint para cerrar sesión
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
    {
        try
        {
            var result = await _authService.LogoutAsync(dto.RefreshToken);

            if (!result.Success)
            {
                _logger.LogWarning("Logout fallido: {Message}", result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("Logout exitoso");

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en endpoint Logout");
            return StatusCode(500, ApiResponse.Fail("Error interno del servidor"));
        }
    }

}