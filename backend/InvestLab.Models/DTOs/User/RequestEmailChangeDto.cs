using System.ComponentModel.DataAnnotations;

namespace InvestLab.Models.DTOs.User;

/// <summary>
/// Datos para solicitar el cambio de email de un usuario autenticado.
/// </summary>
public class RequestEmailChangeDto
{
    /// <summary>Nuevo email al cual se enviará el código de verificación.</summary>
    [Required(ErrorMessage = "El nuevo email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string NewEmail { get; set; }
}
