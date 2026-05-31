using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controlador encargado de la autenticación y gestión de acceso de usuarios.
/// Permite registro, login, verificación, renovación de tokens y logout.
/// </summary>
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

    /// <summary>
    /// Registra un nuevo usuario en el sistema.
    /// </summary>
    /// <param name="registerDto">Datos de registro del usuario</param>
    /// <returns>Resultado de la operación</returns>
    /// <response code="200">Usuario registrado correctamente</response>
    /// <response code="400">Datos inválidos o email ya registrado</response>
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

    /// <summary>
    /// Verifica la cuenta del usuario mediante un código enviado por email.
    /// </summary>
    /// <param name="verifyDto">Email y código de verificación</param>
    /// <returns>Resultado de la operación</returns>
    /// <response code="200">Cuenta verificada correctamente</response>
    /// <response code="400">Código inválido o expirado</response>
    [AllowAnonymous]
    [HttpPost("verify-code")]
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

    /// <summary>
    /// Reenvía el código de verificación al usuario.
    /// </summary>
    /// <param name="dto">Email del usuario</param>
    /// <returns>Resultado de la operación</returns>
    /// <response code="200">Código reenviado correctamente</response>
    /// <response code="400">Error en el envío o usuario inválido</response>
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

    /// <summary>
    /// Inicia sesión de un usuario y devuelve tokens de autenticación.
    /// </summary>
    /// <param name="dto">Credenciales del usuario</param>
    /// <returns>Access token y refresh token</returns>
    /// <response code="200">Login exitoso</response>
    /// <response code="400">Credenciales inválidas</response>
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

    /// <summary>
    /// Renueva el access token utilizando un refresh token válido.
    /// </summary>
    /// <param name="dto">Refresh token</param>
    /// <returns>Nuevos tokens de autenticación</returns>
    /// <response code="200">Token renovado correctamente</response>
    /// <response code="400">Refresh token inválido o expirado</response>
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

    /// <summary>
    /// Cierra la sesión del usuario invalidando el refresh token.
    /// </summary>
    /// <param name="dto">Refresh token a invalidar</param>
    /// <returns>Resultado de la operación</returns>
    /// <response code="200">Logout exitoso</response>
    /// <response code="400">Error en la operación</response>
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