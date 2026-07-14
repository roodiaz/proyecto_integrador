using System.ComponentModel.DataAnnotations;

/// <summary>
/// Datos para solicitar el reenvío de un código de verificación.
/// </summary>
public class ResendCodeDto
{
    /// <summary>Email del usuario que solicita el reenvío.</summary>
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; }
}