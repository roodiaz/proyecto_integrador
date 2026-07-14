using System.ComponentModel.DataAnnotations;
using InvestLab.Models.Validation;

/// <summary>
/// Datos requeridos para registrar un nuevo usuario en el sistema.
/// </summary>
public class RegisterDto
{
    /// <summary>Nombre completo del usuario.</summary>
    [Required(ErrorMessage = "El nombre es requerido")]
    [MinLength(2, ErrorMessage = "El nombre debe tener al menos 2 caracteres")]
    public string FullName { get; set; }

    /// <summary>Email del usuario. Se utiliza como identificador de login.</summary>
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; }

    /// <summary>Teléfono de contacto (opcional).</summary>
    [RegularExpression(@"^[+]?[\d\s\-\(\)]+$", ErrorMessage = "Formato de teléfono inválido")]
    public string Phone { get; set; }

    /// <summary>Contraseña elegida por el usuario. Debe cumplir la política de contraseña fuerte.</summary>
    [Required(ErrorMessage = "La contraseña es requerida")]
    [StrongPassword]
    public string Password { get; set; }

    /// <summary>Confirmación de la contraseña; debe coincidir con <see cref="Password"/>.</summary>
    [Required(ErrorMessage = "Debe confirmar la contraseña")]
    public string ConfirmPassword { get; set; }
}