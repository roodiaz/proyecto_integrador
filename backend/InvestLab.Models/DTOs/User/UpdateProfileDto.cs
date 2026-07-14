using System.ComponentModel.DataAnnotations;

namespace InvestLab.Models.DTOs.User;

/// <summary>
/// Datos actualizables del perfil del usuario.
/// </summary>
public class UpdateProfileDto
{
    /// <summary>Nombre de usuario a mostrar.</summary>
    [Required(ErrorMessage = "El nombre es requerido")]
    [MinLength(3, ErrorMessage = "El nombre debe tener al menos 3 caracteres")]
    public string UserName { get; set; }

    /// <summary>Fecha de nacimiento del usuario (opcional).</summary>
    public DateTime? BirthDate { get; set; }

    /// <summary>Teléfono de contacto (opcional).</summary>
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Teléfono inválido")]
    public string? Phone { get; set; }

    /// <summary>Moneda de referencia elegida por el usuario.</summary>
    [Required(ErrorMessage = "La moneda es requerida")]
    public string Currency { get; set; }

    /// <summary>Indica si el usuario desea recibir notificaciones por email.</summary>
    public bool EmailNotifications { get; set; }

    /// <summary>Tema visual preferido (claro/oscuro).</summary>
    public string? Theme { get; set; }

    /// <summary>Idioma preferido de la interfaz.</summary>
    public string? Language { get; set; }
}