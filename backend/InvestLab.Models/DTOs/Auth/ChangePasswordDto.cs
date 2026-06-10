using System.ComponentModel.DataAnnotations;
using InvestLab.Models.Validation;

namespace InvestLab.Models.DTOs.Auth;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "La contraseña actual es requerida")]
    public string CurrentPassword { get; set; }

    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [StrongPassword]
    public string NewPassword { get; set; }
}
