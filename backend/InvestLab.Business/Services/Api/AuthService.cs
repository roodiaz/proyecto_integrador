using InvestLab.Data;
using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using InvestLab.Models;
using InvestLab.Models.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class AuthService : IAuthService
{
    private readonly InvestLabDbContext _context;
    private readonly LimitsOptions _limits;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher<InvestLab.Data.User> _passwordHasher;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IUserTempCredentialRepository _tempRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserSettingRepository _userSettingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(IUserRepository userRepository, IUserTempCredentialRepository tempRepository, IRefreshTokenRepository refreshTokenRepository, IUserSettingRepository userSettingRepository, IUnitOfWork unitOfWork, IJwtService jwtService, IPasswordHasher<User> passwordHasher, ILogger<AuthService> logger, IEmailService emailService, IOptions<LimitsOptions> limitsOptions)
    {
        _userRepository = userRepository;
        _tempRepository = tempRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _userSettingRepository = userSettingRepository;
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _emailService = emailService;
        _limits = limitsOptions.Value;
    }

    public async Task<Response> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            if (registerDto.Password != registerDto.ConfirmPassword)
                return Response.Fail("Las contraseñas no coinciden");

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
                        : "La cuenta existe pero no pudimos reenviar el código.");
                }

                return Response.Fail("El email ya está registrado");
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
                EmailNotifications = true
            };

            await _userSettingRepository.AddAsync(userProfile);

            var code = new Random().Next(100000, 999999).ToString();
            var codeHash = _passwordHasher.HashPassword(user, code);

            var tempCredential = new UserTempCredential
            {
                User = user,
                TempPasswordHash = codeHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false
            };

            await _tempRepository.AddAsync(tempCredential);

            await _unitOfWork.SaveChangesAsync();

            try
            {
                await _emailService.SendAsync(
                   user.Email,
                   "Verificación de cuenta",
                   EmailTemplates.VerificationCode(code)
                );

                return Response.Ok(new
                {
                    requiresVerification = true,
                    email = user.Email,
                    emailSent = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar email de verificación");

                return Response.Ok(new
                {
                    requiresVerification = true,
                    email = user.Email,
                    emailSent = false
                },
                "La cuenta fue creada correctamente, pero no pudimos enviar el correo de verificación. Intente reenviar el código.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en Register");
            return Response.Fail("Error interno");
        }
    }

    public async Task<Response> VerifyAsync(VerifyDto dto)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                _logger.LogWarning("Verify: usuario no encontrado {Email}", dto.Email);
                return Response.Fail("Usuario no encontrado");
            }
            if (user.IsActive)
            {
                return Response.Ok(null, "La cuenta ya se encuentra verificada");
            }

            var temp = await _tempRepository.GetByUserIdAsync(user.Id);
            if (temp == null)
            {
                _logger.LogWarning("Verify: código no encontrado {UserId}", user.Id);
                return Response.Fail("Código no encontrado");
            }
            if (temp.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Verify: código expirado {UserId}", user.Id);
                return Response.Fail("Código expirado");
            }
            if (temp.IsUsed)
            {
                _logger.LogWarning("Verify: código ya utilizado {UserId}", user.Id);
                return Response.Fail("El código ya fue utilizado");
            }

            var result = _passwordHasher.VerifyHashedPassword(user, temp.TempPasswordHash, dto.Code);

            if (result == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Verify: código inválido {UserId}", user.Id);
                return Response.Fail("Código inválido");
            }

            temp.IsUsed = true;
            user.IsActive = true;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Usuario verificado correctamente {Email}", user.Email);

            return Response.Ok(null, "Cuenta verificada correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en VerifyAsync para {Email}", dto.Email);
            return Response.Fail("Error interno del servidor");
        }
    }

    public async Task<Response> ResendCodeAsync(ResendCodeDto dto)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);

            if (user == null)
            {
                _logger.LogWarning("Resend: usuario no encontrado {Email}", dto.Email);
                return Response.Fail("Usuario no encontrado");
            }

            if (user.IsActive)
            {
                _logger.LogWarning("Resend: usuario ya verificado {Email}", dto.Email);
                return Response.Fail("La cuenta ya está verificada");
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
                : "No pudimos enviar el correo de verificación. Intente reenviar el código.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ResendCodeAsync para {Email}", dto.Email);
            return Response.Fail("Error interno del servidor");
        }
    }

    public async Task<Response> LoginAsync(LoginDto dto)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);

            if (user == null)
                return Response.Fail("Email o contraseña incorrectos");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

            if (result == PasswordVerificationResult.Failed)
                return Response.Fail("Email o contraseña incorrectos");

            if (!user.IsActive)
                return Response.Fail("Debes verificar tu cuenta");

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

            return Response.Ok(new { tokens });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en Login");
            return Response.Fail("Error interno");
        }
    }

    public async Task<Response> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

            if (storedToken == null || storedToken.IsRevoked)
                return Response.Fail("Token inválido");

            if (storedToken.ExpiresAt < DateTime.UtcNow)
                return Response.Fail("Token expirado");

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

            return Response.Ok(new { tokens });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en Refresh");
            return Response.Fail("Error interno");
        }
    }

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

            return Response.Ok(null, "Logout exitoso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en LogoutAsync");
            return Response.Fail("Error interno del servidor");
        }
    }

    private async Task<bool> ResendCodeInternal(User user)
    {
        var code = new Random().Next(100000, 999999).ToString();
        var codeHash = _passwordHasher.HashPassword(user, code);

        var oldCode = await _tempRepository.GetByUserIdAsync(user.Id);

        if (oldCode is null)
        {
            var tempCredential = new UserTempCredential
            {
                UserId = user.Id,
                TempPasswordHash = codeHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false
            };

            await _tempRepository.AddAsync(tempCredential);
        }
        else
        {
            oldCode.TempPasswordHash = codeHash;
            oldCode.ExpiresAt = DateTime.UtcNow.AddMinutes(15);
            oldCode.IsUsed = false;
        }

        await _unitOfWork.SaveChangesAsync();

        try
        {
            await _emailService.SendAsync(
                user.Email,
                "Verificación de cuenta",
                EmailTemplates.VerificationCode(code)
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"No se pudo reenviar el código para {Email}",user.Email);
            return false;
        }
    }
}