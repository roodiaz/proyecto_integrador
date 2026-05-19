using InvestLab.Data;
using InvestLab.Data.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InvestLab.Models;

public class AuthService : IAuthService
{
    private readonly InvestLabDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher<InvestLab.Data.User> _passwordHasher;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailService _emailService;

    public AuthService(InvestLabDbContext context, IJwtService jwtService, IPasswordHasher<User> passwordHasher, ILogger<AuthService> logger, IEmailService emailService)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<Response> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            if (registerDto.Password != registerDto.ConfirmPassword)
            {
                _logger.LogWarning("Passwords no coinciden para {Email}", registerDto.Email);
                return Response.Fail("Las contraseñas no coinciden");
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == registerDto.Email);

            if (existingUser != null)
            {
                if (!existingUser.IsActive)
                {
                    _logger.LogInformation("Usuario no verificado intenta registrarse nuevamente: {Email}", existingUser.Email);

                    await ResendCodeInternal(existingUser);

                    return Response.Ok(new
                    {
                        requiresVerification = true,
                        email = existingUser.Email
                    },
                    "Ya existe una cuenta sin verificar. Te enviamos un nuevo código.");
                }

                _logger.LogWarning("Intento de registro con email existente: {Email}", registerDto.Email);
                return Response.Fail("El email ya está registrado");
            }

            var user = new User
            {
                Username = registerDto.FullName,
                Email = registerDto.Email,
                Phone = registerDto.Phone,
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, registerDto.Password);

            // OTP
            var code = new Random().Next(100000, 999999).ToString();
            var codeHash = _passwordHasher.HashPassword(user, code);

            var tempCredential = new UserTempCredential
            {
                UserId = user.Id,
                TempPasswordHash = codeHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false
            };

            var userProfile = new UserSetting
            {
                UserId = user.Id,
                Currency = "USD",
                EmailNotifications = true
            };

            _context.Users.Add(user);
            _context.UserSettings.Add(userProfile);
            _context.UserTempCredentials.Add(tempCredential);

            await _context.SaveChangesAsync();

            await _emailService.SendAsync(
                    user.Email,
                    "Código de verificación",
                    $@"
                <h2>Verificación de cuenta</h2>
                <p>Tu código es:</p>
                <h1>{code}</h1>
                <p>Válido por 15 minutos</p>"
            );

            _logger.LogInformation("Usuario registrado: {Email}", user.Email);

            return Response.Ok(new
            {
                requiresVerification = true,
                email = user.Email
            },
            "Usuario registrado. Revisá tu email para verificar la cuenta.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en RegisterAsync para {Email}", registerDto.Email);
            return Response.Fail("Error interno del servidor");
        }
    }

    public async Task<Response> VerifyAsync(VerifyDto dto)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (user == null)
                return Response.Fail("Usuario no encontrado");

            var temp = await _context.UserTempCredentials
                .Where(x => x.UserId == user.Id && !x.IsUsed)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (temp == null)
                return Response.Fail("Código no encontrado");

            if (temp.ExpiresAt < DateTime.UtcNow)
                return Response.Fail("Código expirado");

            var result = _passwordHasher.VerifyHashedPassword(user, temp.TempPasswordHash, dto.Code);

            if (result == PasswordVerificationResult.Failed)
                return Response.Fail("Código inválido");

            temp.IsUsed = true;
            user.IsActive = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuario verificado: {Email}", user.Email);

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
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (user == null)
                return Response.Fail("Usuario no encontrado");

            if (user.IsActive)
                return Response.Fail("La cuenta ya está verificada");

            await ResendCodeInternal(user);

            _logger.LogInformation("Código reenviado a {Email}", user.Email);

            return Response.Ok(null, "Se envió un nuevo código");
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
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (user == null)
                return Response.Fail("Email o contraseña incorrectos");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

            if (result == PasswordVerificationResult.Failed)
                return Response.Fail("Email o contraseña incorrectos");

            if (!user.IsActive)
                return Response.Fail("Debes verificar tu cuenta antes de iniciar sesión");

            var tokens = await _jwtService.GenerateTokensAsync(user);
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = tokens.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            _context.RefreshTokens.Add(refreshToken);
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Login exitoso: {Email}", user.Email);

            return Response.Ok(new
            {
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email
                },
                tokens
            }, "Login exitoso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en LoginAsync para {Email}", dto.Email);
            return Response.Fail("Error interno del servidor");
        }
    }

    public async Task<Response> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var storedToken = await _context.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == refreshToken);

            if (storedToken == null)
            {
                _logger.LogWarning("Refresh token no encontrado");
                return Response.Fail("Token inválido");
            }

            if (storedToken.IsRevoked)
            {
                _logger.LogWarning("Refresh token revocado para usuario {UserId}", storedToken.UserId);
                return Response.Fail("Token inválido");
            }

            if (storedToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token expirado para usuario {UserId}", storedToken.UserId);
                return Response.Fail("Token expirado");
            }

            var user = storedToken.User;
            storedToken.IsRevoked = true;

            var tokens = await _jwtService.GenerateTokensAsync(user);
            var newRefreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = tokens.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            _context.RefreshTokens.Add(newRefreshToken);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Refresh exitoso para usuario {UserId}", user.Id);

            return Response.Ok(new
            {
                tokens
            }, "Token renovado correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en RefreshTokenAsync");
            return Response.Fail("Error interno del servidor");
        }
    }

    public async Task<Response> LogoutAsync(string refreshToken)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (storedToken == null)
            return Response.Ok(null);

        storedToken.IsRevoked = true;

        await _context.SaveChangesAsync();

        return Response.Ok(null, "Logout exitoso");
    }

    private async Task ResendCodeInternal(User user)
    {
        var oldCodes = await _context.UserTempCredentials
            .Where(x => x.UserId == user.Id && !x.IsUsed)
            .ToListAsync();

        foreach (var item in oldCodes)
            item.IsUsed = true;

        var code = new Random().Next(100000, 999999).ToString();
        var codeHash = _passwordHasher.HashPassword(user, code);

        var tempCredential = new UserTempCredential
        {
            UserId = user.Id,
            TempPasswordHash = codeHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            IsUsed = false
        };

        _context.UserTempCredentials.Add(tempCredential);

        await _context.SaveChangesAsync();

        await _emailService.SendAsync(
                user.Email,
                "Código de verificación",
                $@"
            <h2>Verificación de cuenta</h2>
            <p>Tu código es:</p>
            <h1>{code}</h1>
            <p>Válido por 15 minutos</p>"
        );
    }
}