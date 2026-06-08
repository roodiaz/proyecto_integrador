using System.ComponentModel.DataAnnotations;

namespace InvestLab.Models.DTOs.User;

public class ConfirmEmailChangeDto
{
    [Required(ErrorMessage = "El código es requerido")]
    public string Code { get; set; }
}
