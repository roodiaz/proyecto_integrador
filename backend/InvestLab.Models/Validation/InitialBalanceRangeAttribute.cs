using System.ComponentModel.DataAnnotations;

namespace InvestLab.Models.Validation;

/// <summary>
/// Valida que una propiedad de tipo <see cref="decimal"/> cumpla con el rango de saldo
/// inicial definido por <see cref="PortfolioPolicy"/>. Se usa en los DTOs de configuración
/// inicial y reinicio de portfolio para mantener una única fuente de verdad sobre los
/// límites de saldo inicial permitidos.
/// </summary>
public class InitialBalanceRangeAttribute : ValidationAttribute
{
    public InitialBalanceRangeAttribute()
    {
        ErrorMessage = PortfolioPolicy.BalanceErrorMessage;
    }

    public override bool IsValid(object? value)
    {
        if (value is not decimal balance)
            return false;

        return PortfolioPolicy.IsValidInitialBalance(balance);
    }
}
