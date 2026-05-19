using System.ComponentModel.DataAnnotations;

public class RefreshTokenDto
{
    [Required(ErrorMessage = "El refresh token es requerido")]
    public string RefreshToken { get; set; }
}