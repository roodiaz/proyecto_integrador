using System.ComponentModel.DataAnnotations;

namespace InvestLab.Models.DTOs.User;

public class UpdateProfileDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [MinLength(3, ErrorMessage = "El nombre debe tener al menos 3 caracteres")]
    public string UserName { get; set; }

    public DateTime? BirthDate { get; set; }

    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Teléfono inválido")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "La moneda es requerida")]
    public string Currency { get; set; }

    public bool EmailNotifications { get; set; }
}