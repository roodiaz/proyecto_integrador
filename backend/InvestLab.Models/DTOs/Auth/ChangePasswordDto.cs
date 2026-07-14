using System.ComponentModel.DataAnnotations;
using InvestLab.Models.Validation;

namespace InvestLab.Models.DTOs.Auth;

/// <summary>
/// Datos para cambiar la contraseña de un usuario autenticado.
/// </summary>
public class ChangePasswordDto
{
    /// <summary>Contraseña actual del usuario, utilizada para validar la operación.</summary>
    [Required(ErrorMessage = "La contraseña actual es requerida")]
    public string CurrentPassword { get; set; }

    /// <summary>Nueva contraseña. Debe cumplir la política de contraseña fuerte.</summary>
    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [StrongPassword]
    public string NewPassword { get; set; }
}
