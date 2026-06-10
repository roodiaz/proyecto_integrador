using System.ComponentModel.DataAnnotations;

namespace InvestLab.Models.Validation;

/// <summary>
/// Valida que una propiedad de tipo <see cref="string"/> cumpla con la
/// <see cref="PasswordPolicy"/> de InvestLab. Se usa en los DTOs de registro,
/// cambio de contraseña y recuperación de contraseña para mantener una única
/// fuente de verdad sobre las reglas de contraseñas seguras.
///
/// Las cadenas vacías o nulas se consideran válidas para este atributo: el
/// requisito de "campo requerido" debe cubrirse con <see cref="RequiredAttribute"/>.
/// </summary>
public class StrongPasswordAttribute : ValidationAttribute
{
    public StrongPasswordAttribute()
    {
        ErrorMessage = PasswordPolicy.ErrorMessage;
    }

    public override bool IsValid(object? value)
    {
        if (value is not string password || string.IsNullOrEmpty(password))
            return true;

        return PasswordPolicy.IsValid(password);
    }
}
