using System.ComponentModel.DataAnnotations;

namespace InvestLab.Models.DTOs.User;

/// <summary>
/// Datos para confirmar el cambio de email validando el código de verificación.
/// </summary>
public class ConfirmEmailChangeDto
{
    /// <summary>Código de verificación enviado al nuevo email.</summary>
    [Required(ErrorMessage = "El código es requerido")]
    public string Code { get; set; }
}
