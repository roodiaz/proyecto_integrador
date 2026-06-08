using System.ComponentModel.DataAnnotations;

namespace InvestLab.Models.DTOs.Auth;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; }
}
