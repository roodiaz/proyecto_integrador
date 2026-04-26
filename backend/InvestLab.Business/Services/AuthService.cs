using InvestLab.Data;
using InvestLab.Data.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

public class AuthService : IAuthService
{
    private readonly InvestLabDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher<InvestLab.Data.User> _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(InvestLabDbContext context, IJwtService jwtService, IPasswordHasher<User> passwordHasher, ILogger<AuthService> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    //public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto registerDto)
    //{
    //    // Validar que las contraseñas coincidan
    //    if (registerDto.Password != registerDto.ConfirmPassword)
    //    {
    //        return new AuthResponseDto { Success = false, Message = "Las contraseñas no coinciden" };
    //    }

    //    // Verificar si el email ya existe
    //    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
    //    if (existingUser != null)
    //    {
    //        return new AuthResponseDto { Success = false, Message = "El email ya está registrado" };
    //    }

    //    // Crear nuevo usuario
    //    var user = new User
    //    {
    //        FullName = registerDto.FullName,
    //        Email = registerDto.Email,
    //        Phone = registerDto.Phone,
    //        PasswordHash = _passwordHasher.HashPassword(null, registerDto.Password),
    //        IsActive = true,
    //        CreatedAt = DateTime.UtcNow,
    //        UpdatedAt = DateTime.UtcNow
    //    };

    //    _context.Users.Add(user);
    //    await _context.SaveChangesAsync();

    //    // Crear perfil del usuario
    //    var userProfile = new UserProfile
    //    {
    //        UserId = user.Id,
    //        PreferredLanguage = "es",
    //        Timezone = "UTC-3",
    //        ThemePreference = "dark",
    //        CreatedAt = DateTime.UtcNow,
    //        UpdatedAt = DateTime.UtcNow
    //    };

    //    _context.UserProfiles.Add(userProfile);
    //    await _context.SaveChangesAsync();

    //    // Generar tokens
    //    var tokens = await _jwtService.GenerateTokensAsync(user);

    //    return new AuthResponseDto
    //    {
    //        Success = true,
    //        User = new UserDto
    //        {
    //            Id = user.Id,
    //            FullName = user.FullName,
    //            Email = user.Email,
    //            Phone = user.Phone,
    //            CreatedAt = user.CreatedAt
    //        },
    //        Tokens = tokens,
    //        Message = "Usuario registrado exitosamente"
    //    };
    //}

    //public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    //{
    //    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
    //    if (user == null || !user.IsActive)
    //    {
    //        return new AuthResponseDto { Success = false, Message = "Credenciales inválidas" };
    //    }

    //    var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
    //    if (result == PasswordVerificationResult.Failed)
    //    {
    //        return new AuthResponseDto { Success = false, Message = "Credenciales inválidas" };
    //    }

    //    // Generar tokens
    //    var tokens = await _jwtService.GenerateTokensAsync(user);

    //    return new AuthResponseDto
    //    {
    //        Success = true,
    //        User = new UserDto
    //        {
    //            Id = user.Id,
    //            FullName = user.FullName,
    //            Email = user.Email,
    //            Phone = user.Phone,
    //            CreatedAt = user.CreatedAt
    //        },
    //        Tokens = tokens,
    //        Message = "Login exitoso"
    //    };
    //}

    //public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    //{
    //    // Implementar refresh token logic
    //    // Validar refresh token, generar nuevos tokens
    //    throw new NotImplementedException();
    //}

    //public async Task<bool> LogoutAsync(int userId)
    //{
    //    // Implementar logout logic
    //    // Invalidar refresh tokens
    //    throw new NotImplementedException();
    //}

    //public async Task<UserDto> GetUserByIdAsync(int userId)
    //{
    //    var user = await _context.Users.FindAsync(userId);
    //    if (user == null) return null;

    //    return new UserDto
    //    {
    //        Id = user.Id,
    //        FullName = user.FullName,
    //        Email = user.Email,
    //        Phone = user.Phone,
    //        CreatedAt = user.CreatedAt
    //    };
    //}
}