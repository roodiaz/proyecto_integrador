using System.ComponentModel.DataAnnotations;

namespace InvestLab.Models.DTOs.Auth;

/// <summary>
/// Datos para iniciar la recuperación de contraseña de un usuario.
/// </summary>
public class ForgotPasswordDto
{
    /// <summary>Email del usuario que olvidó su contraseña.</summary>
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; }
}
