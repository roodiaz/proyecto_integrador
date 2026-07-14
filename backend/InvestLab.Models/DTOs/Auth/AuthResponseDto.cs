/// <summary>
/// Resultado de una operación de autenticación (login, registro o refresh).
/// </summary>
public class AuthResponseDto
{
    /// <summary>Indica si la operación fue exitosa.</summary>
    public bool Success { get; set; }

    /// <summary>Datos básicos del usuario autenticado.</summary>
    public UserDto User { get; set; }

    /// <summary>Par de tokens (access y refresh) emitidos.</summary>
    public TokenDto Tokens { get; set; }

    /// <summary>Mensaje descriptivo del resultado.</summary>
    public string Message { get; set; }
}

/// <summary>
/// Datos básicos del usuario devueltos tras autenticarse.
/// </summary>
public class UserDto
{
    /// <summary>Identificador del usuario.</summary>
    public int Id { get; set; }

    /// <summary>Nombre de usuario.</summary>
    public string UserName { get; set; }

    /// <summary>Email del usuario.</summary>
    public string Email { get; set; }

    /// <summary>Teléfono del usuario.</summary>
    public string Phone { get; set; }

    /// <summary>Fecha de creación de la cuenta.</summary>
    public DateTime? CreatedAt { get; set; }
}

/// <summary>
/// Par de tokens de autenticación emitidos al usuario.
/// </summary>
public class TokenDto
{
    /// <summary>Token JWT utilizado para autenticar cada request.</summary>
    public string AccessToken { get; set; }

    /// <summary>Token utilizado para renovar el access token sin volver a loguearse.</summary>
    public string RefreshToken { get; set; }

    /// <summary>Fecha y hora de expiración del access token.</summary>
    public DateTime ExpiresAt { get; set; }
}