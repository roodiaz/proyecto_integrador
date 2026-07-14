using System.ComponentModel.DataAnnotations;

/// <summary>
/// Contiene el refresh token utilizado para renovar la sesión o cerrarla.
/// </summary>
public class RefreshTokenDto
{
    /// <summary>Refresh token vigente emitido en el login.</summary>
    [Required(ErrorMessage = "El refresh token es requerido")]
    public string RefreshToken { get; set; }
}