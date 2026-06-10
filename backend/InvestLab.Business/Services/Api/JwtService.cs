using InvestLab.Data;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InvestLab.Models.DTOs.Auth;

namespace InvestLab.Business.Services.Api;

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="JwtService"/> con la configuración de JWT proporcionada.
    /// </summary>
    /// <param name="jwtSettings">Opciones de configuración para la generación y validación de tokens JWT.</param>
    public JwtService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    /// <summary>
    /// Genera un token de acceso JWT y un token de actualización (refresh token) para el usuario indicado,
    /// incluyendo sus claims principales (identificador, email y nombre de usuario).
    /// </summary>
    /// <param name="user">Usuario para el cual se generarán los tokens.</param>
    /// <returns>Un objeto <see cref="TokenDto"/> con el token de acceso, el token de actualización y su fecha de expiración.</returns>
    public Task<TokenDto> GenerateTokensAsync(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.Username!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiration,
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return Task.FromResult(new TokenDto
        {
            AccessToken = tokenHandler.WriteToken(token),
            RefreshToken = GenerateRefreshToken(),
            ExpiresAt = expiration
        });
    }

    /// <summary>
    /// Genera un token de actualización (refresh token) seguro y aleatorio codificado en Base64.
    /// </summary>
    /// <returns>Una cadena con el token de actualización generado.</returns>
    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}