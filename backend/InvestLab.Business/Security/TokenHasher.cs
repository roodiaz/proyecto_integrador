using System.Security.Cryptography;
using System.Text;

namespace InvestLab.Business.Security;

/// <summary>
/// Genera hashes SHA-256 de refresh tokens para que el valor en texto plano
/// nunca se persista en la base de datos.
/// </summary>
public static class TokenHasher
{
    /// <summary>
    /// Calcula el hash SHA-256 (en hexadecimal) del token recibido.
    /// </summary>
    /// <param name="token">Valor del refresh token en texto plano.</param>
    /// <returns>El hash SHA-256 del token, en formato hexadecimal en minúsculas.</returns>
    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
