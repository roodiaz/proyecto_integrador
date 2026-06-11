using System.ComponentModel.DataAnnotations;

namespace InvestLab.Models.Validation;

/// <summary>
/// Valida que una propiedad de tipo <see cref="string"/> cumpla con la
/// <see cref="PortfolioPolicy"/> de InvestLab para nombres de portfolio. Se usa en los
/// DTOs de configuración inicial y reinicio de portfolio para mantener una única fuente
/// de verdad sobre las reglas de nombres válidos.
///
/// Las cadenas vacías o nulas se consideran válidas para este atributo: el
/// requisito de "campo requerido" debe cubrirse con <see cref="RequiredAttribute"/>.
/// </summary>
public class PortfolioNameAttribute : ValidationAttribute
{
    public PortfolioNameAttribute()
    {
        ErrorMessage = PortfolioPolicy.NameErrorMessage;
    }

    public override bool IsValid(object? value)
    {
        if (value is not string name || string.IsNullOrEmpty(name))
            return true;

        return PortfolioPolicy.IsValidName(name);
    }
}
