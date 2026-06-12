using System.Text.RegularExpressions;

namespace InvestLab.Models.Validation;

/// <summary>
/// Política de configuración de portfolio de InvestLab, utilizada por
/// <see cref="PortfolioNameAttribute"/> e <see cref="InitialBalanceRangeAttribute"/>
/// para validar de forma centralizada el nombre y el saldo inicial del portfolio
/// en los flujos de configuración inicial y reinicio de simulación.
/// </summary>
public static class PortfolioPolicy
{
    public const int NameMinLength = 3;
    public const int NameMaxLength = 100;

    public const int MaxPortfolios = 3;

    public const decimal MinInitialBalance = 1000m;
    public const decimal MaxInitialBalance = 1_000_000m;

    public const string NameErrorMessage =
        "El nombre del portfolio debe tener entre 3 y 100 caracteres y contener solo letras, números y espacios.";

    public const string BalanceErrorMessage =
        "El saldo inicial debe estar entre 1.000 y 1.000.000 USD.";

    private static readonly Regex NamePattern = new(@"^[\p{L}\p{N}\s]+$", RegexOptions.Compiled);

    /// <summary>Indica si el nombre del portfolio cumple con la política (longitud y caracteres permitidos).</summary>
    public static bool IsValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name)
        && name.Length >= NameMinLength
        && name.Length <= NameMaxLength
        && NamePattern.IsMatch(name);

    /// <summary>Indica si el saldo inicial está dentro del rango permitido.</summary>
    public static bool IsValidInitialBalance(decimal value) =>
        value >= MinInitialBalance && value <= MaxInitialBalance;
}
