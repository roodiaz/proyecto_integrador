using System.ComponentModel.DataAnnotations;

namespace InvestLab.Models.DTOs.Auth;

public class ResetPasswordDto
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; }

    [Required(ErrorMessage = "El código es requerido")]
    public string Code { get; set; }

    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string NewPassword { get; set; }

    [Required(ErrorMessage = "La confirmación de la contraseña es requerida")]
    public string ConfirmPassword { get; set; }
}
