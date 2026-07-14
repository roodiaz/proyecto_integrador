using System.ComponentModel.DataAnnotations;
using InvestLab.Models.Validation;

namespace InvestLab.Models.DTOs.Auth;

/// <summary>
/// Datos para restablecer la contraseña validando el código de recuperación enviado por email.
/// </summary>
public class ResetPasswordDto
{
    /// <summary>Email del usuario.</summary>
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; }

    /// <summary>Código de recuperación enviado por email.</summary>
    [Required(ErrorMessage = "El código es requerido")]
    public string Code { get; set; }

    /// <summary>Nueva contraseña. Debe cumplir la política de contraseña fuerte.</summary>
    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [StrongPassword]
    public string NewPassword { get; set; }

    /// <summary>Confirmación de la nueva contraseña; debe coincidir con <see cref="NewPassword"/>.</summary>
    [Required(ErrorMessage = "La confirmación de la contraseña es requerida")]
    public string ConfirmPassword { get; set; }
}
