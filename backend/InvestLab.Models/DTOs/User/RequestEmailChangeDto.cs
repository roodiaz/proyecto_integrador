using System.ComponentModel.DataAnnotations;

namespace InvestLab.Models.DTOs.User;

public class RequestEmailChangeDto
{
    [Required(ErrorMessage = "El nuevo email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string NewEmail { get; set; }
}
