using System.ComponentModel.DataAnnotations;
using InvestLab.Models.Validation;

public class RegisterDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [MinLength(2, ErrorMessage = "El nombre debe tener al menos 2 caracteres")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; }

    [RegularExpression(@"^[+]?[\d\s\-\(\)]+$", ErrorMessage = "Formato de teléfono inválido")]
    public string Phone { get; set; }

    [Required(ErrorMessage = "La contraseña es requerida")]
    [StrongPassword]
    public string Password { get; set; }

    [Required(ErrorMessage = "Debe confirmar la contraseña")]
    public string ConfirmPassword { get; set; }
}