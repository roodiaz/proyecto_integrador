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
        var result = await _authService.RegisterAsync(registerDto);

        if (!result.Success)
        {
            _logger.LogWarning("Register fallido para {Email}: {Message}", registerDto.Email, result.Message);
            return BadRequest(result);
        }

        _logger.LogInformation("Register exitoso para {Email}", registerDto.Email);

        return Ok(result);
    }

    // Endpoint para verificar el correo electrónico del usuario
    [AllowAnonymous]
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyDto verifyDto)
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

    // Endpoint para reenviar el código de verificación
    [AllowAnonymous]
    [HttpPost("resend-code")]
    public async Task<IActionResult> ResendCode([FromBody] ResendCodeDto dto)
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

    // Endpoint para iniciar sesión
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
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

    // Endpoint para refrescar el token
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
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

    // Endpoint para cerrar sesión
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
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

}