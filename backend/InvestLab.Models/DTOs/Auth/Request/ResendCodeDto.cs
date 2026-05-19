using System.ComponentModel.DataAnnotations;

public class ResendCodeDto
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; }
}