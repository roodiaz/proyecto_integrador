/// <summary>
/// Datos para verificar la cuenta de un usuario recién registrado.
/// </summary>
public class VerifyDto
{
    /// <summary>Email del usuario a verificar.</summary>
    public string Email { get; set; }

    /// <summary>Código de verificación enviado por email.</summary>
    public string Code { get; set; }
}