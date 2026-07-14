using System.ComponentModel.DataAnnotations;

/// <summary>
/// Credenciales utilizadas para iniciar sesión.
/// </summary>
public class LoginDto
{
    /// <summary>Email registrado del usuario.</summary>
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; }

    /// <summary>Contraseña del usuario.</summary>
    [Required(ErrorMessage = "La contraseña es requerida")]
    public string Password { get; set; }
}