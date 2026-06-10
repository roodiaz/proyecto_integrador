using System.ComponentModel.DataAnnotations;
using InvestLab.Models.Validation;

namespace InvestLab.Models.DTOs.Auth;

public class ResetPasswordDto
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; }

    [Required(ErrorMessage = "El código es requerido")]
    public string Code { get; set; }

    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [StrongPassword]
    public string NewPassword { get; set; }

    [Required(ErrorMessage = "La confirmación de la contraseña es requerida")]
    public string ConfirmPassword { get; set; }
}
