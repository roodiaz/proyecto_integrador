using InvestLab.Business.Interfaces.Api;
using InvestLab.Data;
using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Auth;
using InvestLab.Models.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static InvestLab.Models.MessageCodes;

public class AuthService : IAuthService
{
    private readonly InvestLabDbContext _context;
    private readonly LimitsOptions _limits;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher<InvestLab.Data.User> _passwordHasher;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailProviderResolver _emailProviderResolver;
    private readonly IVerificationCodeService _verificationCodeService;
    private readonly IUserRepository _userRepository;
    private readonly IUserTempCredentialRepository _tempRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserSettingRepository _userSettingRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AuthService"/> con sus dependencias requeridas.
    /// </summary>
    /// <param name="userRepository">Repositorio de usuarios.</param>
    /// <param name="tempRepository">Repositorio de credenciales temporales de usuario.</param>
    /// <param name="refreshTokenRepository">Repositorio de tokens de actualización (refresh tokens).</param>
    /// <param name="userSettingRepository">Repositorio de configuraciones de usuario.</param>
    /// <param name="unitOfWork">Unidad de trabajo para guardar cambios en la base de datos.</param>
    /// <param name="jwtService">Servicio para la generación de tokens JWT.</param>
    /// <param name="passwordHasher">Servicio de hash y verificación de contraseñas.</param>
    /// <param name="logger">Logger para el registro de eventos del servicio.</param>
    /// <param name="emailProviderResolver">Resolver utilizado para obtener el proveedor de envío de correos electrónicos activo.</param>
    /// <param name="limitsOptions">Opciones de configuración de límites de la aplicación.</param>
    public AuthService(IUserRepository userRepository, IUserTempCredentialRepository tempRepository, IRefreshTokenRepository refreshTokenRepository, IUserSettingRepository userSettingRepository, IUnitOfWork unitOfWork, IJwtService jwtService, IPasswordHasher<User> passwordHasher, ILogger<AuthService> logger, IEmailProviderResolver emailProviderResolver, IVerificationCodeService verificationCodeService, IOptions<LimitsOptions> limitsOptions)
    {
        _userRepository = userRepository;
        _tempRepository = tempRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _userSettingRepository = userSettingRepository;
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _emailProviderResolver = emailProviderResolver;
        _verificationCodeService = verificationCodeService;
        _limits = limitsOptions.Value;
    }

    /// <summary>
    /// Registra un nuevo usuario en el sistema, valida sus datos, crea su cuenta, configuración inicial
    /// y credencial temporal de verificación, y envía un correo con el código de verificación.
    /// </summary>
    /// <param name="registerDto">Datos de registro del usuario, incluyendo email, contraseña y datos personales.</param>
    /// <returns>Una respuesta indicando el resultado de la operación de registro.</returns>
    public async Task<Response> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            if (registerDto.Password != registerDto.ConfirmPassword)
                return Response.Fail("Las contraseñas no coinciden", PASSWORDS_DO_NOT_MATCH);

            var existingUser = await _userRepository.GetByEmailAsync(registerDto.Email);

            if (existingUser != null)
            {
                if (!existingUser.IsActive)
                {
                    var emailSent = await ResendCodeInternal(existingUser);

                    return Response.Ok(new
                    {
                        requiresVerification = true,
                        email = existingUser.Email,
                        emailSent
                    },
                    emailSent
                        ? "Ya existe una cuenta sin verificar."
                        : "La cuenta existe pero no pudimos reenviar el código.",
                    UNVERIFIED_ACCOUNT_EXISTS);
                }

                return Response.Fail("El email ya está registrado", EMAIL_ALREADY_REGISTERED);
            }

            var user = new User
            {
                Username = registerDto.FullName,
                Email = registerDto.Email,
                Phone = registerDto.Phone,
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                Balance = _limits.InitialBalance
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, registerDto.Password);

            await _userRepository.AddAsync(user);

            var userProfile = new UserSetting
            {
                User = user,
                Currency = "USD",
                EmailNotifications = true,
                Language = "es"
            };

            await _userSettingRepository.AddAsync(userProfile);

            await _unitOfWork.SaveChangesAsync();

            var verificationEmailSent = await _verificationCodeService.GenerateAndSendCodeAsync(user, "Verificación de cuenta");

            if (verificationEmailSent)
            {
                return Response.Ok(new
                {
                    requiresVerification = true,
                    email = user.Email,
                    emailSent = true
                }, code: REGISTRATION_SUCCESS);
            }

            return Response.Ok(new
            {
                requiresVerification = true,
                email = user.Email,
                emailSent = false
            },
            "La cuenta fue creada correctamente, pero no pudimos enviar el correo de verificación. Intente reenviar el código.",
            REGISTRATION_SUCCESS_EMAIL_FAILED);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en Register");
            return Response.Fail("Error interno", INTERNAL_ERROR);
        }
    }

    /// <summary>
    /// Verifica la cuenta de un usuario validando el código de verificación enviado por correo electrónico.
    /// </summary>
    /// <param name="dto">Datos necesarios para la verificación, incluyendo el email y el código.</param>
    /// <returns>Una respuesta indicando si la verificación fue exitosa o el motivo del fallo.</returns>
    public async Task<Response> VerifyAsync(VerifyDto dto)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                _logger.LogWarning("Verify: usuario no encontrado {Email}", dto.Email);
                return Response.Fail("Usuario no encontrado", USER_NOT_FOUND);
            }
            if (user.IsActive)
            {
                return Response.Ok(null, "La cuenta ya se encuentra verificada", ACCOUNT_ALREADY_VERIFIED);
            }

            var validation = await _verificationCodeService.ValidateCodeAsync(user, dto.Code);

            if (!validation.Success)
            {
                _logger.LogWarning("Verify: {Reason} {UserId}", validation.ErrorMessage, user.Id);
                return Response.Fail(validation.ErrorMessage!, validation.ErrorCode);
            }

            validation.Credential!.IsUsed = true;
            user.IsActive = true;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Usuario verificado correctamente {Email}", user.Email);

            return Response.Ok(null, "Cuenta verificada correctamente", ACCOUNT_VERIFIED);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en VerifyAsync para {Email}", dto.Email);
            return Response.Fail("Error interno del servidor", INTERNAL_ERROR);
        }
    }

    /// <summary>
    /// Reenvía el código de verificación a un usuario que aún no ha activado su cuenta.
    /// </summary>
    /// <param name="dto">Datos necesarios para el reenvío, incluyendo el email del usuario.</param>
    /// <returns>Una respuesta indicando si el código fue reenviado correctamente.</returns>
    public async Task<Response> ResendCodeAsync(ResendCodeDto dto)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);

            if (user == null)
            {
                _logger.LogWarning("Resend: usuario no encontrado {Email}", dto.Email);
                return Response.Fail("Usuario no encontrado", USER_NOT_FOUND);
            }

            if (user.IsActive)
            {
                _logger.LogWarning("Resend: usuario ya verificado {Email}", dto.Email);
                return Response.Fail("La cuenta ya está verificada", ACCOUNT_ALREADY_VERIFIED);
            }

            var emailSent = await ResendCodeInternal(user);

            if (emailSent)
                _logger.LogInformation("Código reenviado para {Email}", user.Email);
            else
                _logger.LogWarning("No se pudo reenviar el código para {Email}", user.Email);

            return Response.Ok(new
            {
                requiresVerification = true,
                email = user.Email,
                emailSent
            },
            emailSent
                ? "Se envió un nuevo código"
                : "No pudimos enviar el correo de verificación. Intente reenviar el código.",
            emailSent ? CODE_RESENT : CODE_RESEND_FAILED);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ResendCodeAsync para {Email}", dto.Email);
            return Response.Fail("Error interno del servidor", INTERNAL_ERROR);
        }
    }

    /// <summary>
    /// Autentica a un usuario validando sus credenciales, genera nuevos tokens de acceso y actualización,
    /// y registra la fecha de su último inicio de sesión.
    /// </summary>
    /// <param name="dto">Credenciales de inicio de sesión del usuario (email y contraseña).</param>
    /// <returns>Una respuesta con los tokens generados si el inicio de sesión es exitoso, o el motivo del fallo.</returns>
    public async Task<Response> LoginAsync(LoginDto dto)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);

            if (user == null)
                return Response.Fail("Email o contraseña incorrectos", INVALID_CREDENTIALS);

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

            if (result == PasswordVerificationResult.Failed)
                return Response.Fail("Email o contraseña incorrectos", INVALID_CREDENTIALS);

            if (!user.IsActive)
                return Response.Fail("Debes verificar tu cuenta", ACCOUNT_NOT_VERIFIED);

            var tokens = await _jwtService.GenerateTokensAsync(user);

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = tokens.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            await _refreshTokenRepository.AddAsync(refreshToken);

            user.LastLoginAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return Response.Ok(new { tokens }, code: LOGIN_SUCCESS);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en Login");
            return Response.Fail("Error interno", INTERNAL_ERROR);
        }
    }

    /// <summary>
    /// Renueva los tokens de acceso y actualización de un usuario a partir de un token de actualización válido,
    /// revocando el token anterior y generando uno nuevo.
    /// </summary>
    /// <param name="refreshToken">Token de actualización (refresh token) actual del usuario.</param>
    /// <returns>Una respuesta con los nuevos tokens generados, o el motivo del fallo si el token es inválido o expiró.</returns>
    public async Task<Response> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

            if (storedToken == null || storedToken.IsRevoked)
                return Response.Fail("Token inválido", INVALID_TOKEN);

            if (storedToken.ExpiresAt < DateTime.UtcNow)
                return Response.Fail("Token expirado", TOKEN_EXPIRED);

            var user = storedToken.User;

            storedToken.IsRevoked = true;

            var tokens = await _jwtService.GenerateTokensAsync(user);

            var newToken = new RefreshToken
            {
                UserId = user.Id,
                Token = tokens.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _refreshTokenRepository.AddAsync(newToken);

            await _unitOfWork.SaveChangesAsync();

            return Response.Ok(new { tokens }, code: LOGIN_SUCCESS);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en Refresh");
            return Response.Fail("Error interno", INTERNAL_ERROR);
        }
    }

    /// <summary>
    /// Cierra la sesión de un usuario revocando su token de actualización (refresh token).
    /// </summary>
    /// <param name="refreshToken">Token de actualización a revocar.</param>
    /// <returns>Una respuesta indicando si el cierre de sesión se realizó correctamente.</returns>
    public async Task<Response> LogoutAsync(string refreshToken)
    {
        try
        {
            var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

            if (storedToken == null || storedToken.IsRevoked)
            {
                _logger.LogWarning("Logout: token no encontrado");
                return Response.Ok(null);
            }

            storedToken.IsRevoked = true;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Logout exitoso para usuario {UserId}", storedToken.UserId);

            return Response.Ok(null, "Logout exitoso", LOGOUT_SUCCESS);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en LogoutAsync");
            return Response.Fail("Error interno del servidor", INTERNAL_ERROR);
        }
    }

    /// <summary>
    /// Inicia el proceso de recuperación de contraseña: valida que exista una cuenta asociada
    /// al email indicado, genera un código de recuperación reutilizando la infraestructura de
    /// códigos de verificación, y lo envía por correo electrónico.
    /// </summary>
    /// <param name="dto">Datos necesarios para iniciar la recuperación, incluyendo el email del usuario.</param>
    /// <returns>Una respuesta indicando si el código de recuperación fue enviado correctamente.</returns>
    public async Task<Response> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);

            if (user == null)
            {
                _logger.LogWarning("ForgotPassword: usuario no encontrado {Email}", dto.Email);
                return Response.Fail("No existe una cuenta asociada a ese email", ACCOUNT_NOT_FOUND_FOR_EMAIL);
            }

            var emailSent = await _verificationCodeService.GenerateAndSendCodeAsync(user, "Recuperación de contraseña");

            if (emailSent)
                _logger.LogInformation("Código de recuperación enviado a {Email}", user.Email);
            else
                _logger.LogWarning("No se pudo enviar el código de recuperación a {Email}", user.Email);

            return Response.Ok(new
            {
                email = user.Email,
                emailSent
            },
            emailSent
                ? "Se envió un código de recuperación a tu correo electrónico"
                : "No pudimos enviar el correo de recuperación. Intente nuevamente.",
            emailSent ? RECOVERY_EMAIL_SENT : RECOVERY_EMAIL_FAILED);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ForgotPasswordAsync para {Email}", dto.Email);
            return Response.Fail("Error interno del servidor", INTERNAL_ERROR);
        }
    }

    /// <summary>
    /// Restablece la contraseña de un usuario validando el código de recuperación enviado por correo,
    /// actualiza el hash de contraseña, invalida el código utilizado y revoca las sesiones activas
    /// (refresh tokens) para mantener la consistencia con el flujo de autenticación actual.
    /// </summary>
    /// <param name="dto">Datos necesarios para restablecer la contraseña: email, código, nueva contraseña y su confirmación.</param>
    /// <returns>Una respuesta indicando si la contraseña fue restablecida correctamente o el motivo del fallo.</returns>
    public async Task<Response> ResetPasswordAsync(ResetPasswordDto dto)
    {
        try
        {
            if (dto.NewPassword != dto.ConfirmPassword)
                return Response.Fail("Las contraseñas no coinciden", PASSWORDS_DO_NOT_MATCH);

            var user = await _userRepository.GetByEmailAsync(dto.Email);

            if (user == null)
            {
                _logger.LogWarning("ResetPassword: usuario no encontrado {Email}", dto.Email);
                return Response.Fail("Usuario no encontrado", USER_NOT_FOUND);
            }

            var validation = await _verificationCodeService.ValidateCodeAsync(user, dto.Code);

            if (!validation.Success)
            {
                _logger.LogWarning("ResetPassword: {Reason} {UserId}", validation.ErrorMessage, user.Id);
                return Response.Fail(validation.ErrorMessage!, validation.ErrorCode);
            }

            validation.Credential!.IsUsed = true;

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            user.PasswordChangedAt = DateTime.UtcNow;

            await _refreshTokenRepository.DeleteByUserIdAsync(user.Id);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Contraseña restablecida correctamente para {Email}", user.Email);

            return Response.Ok(null, "Contraseña actualizada correctamente", PASSWORD_RESET_SUCCESS);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ResetPasswordAsync para {Email}", dto.Email);
            return Response.Fail("Error interno del servidor", INTERNAL_ERROR);
        }
    }

    /// <summary>
    /// Genera o actualiza el código de verificación temporal de un usuario y le envía un correo electrónico
    /// con dicho código.
    /// </summary>
    /// <param name="user">Usuario al que se le generará y enviará el nuevo código de verificación.</param>
    /// <returns><c>true</c> si el correo con el código fue enviado correctamente; en caso contrario, <c>false</c>.</returns>
    private Task<bool> ResendCodeInternal(User user)
    {
        return _verificationCodeService.GenerateAndSendCodeAsync(user, "Verificación de cuenta");
    }
}