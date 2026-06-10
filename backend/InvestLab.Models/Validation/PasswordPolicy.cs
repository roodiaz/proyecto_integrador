using System.Text.RegularExpressions;

namespace InvestLab.Models.Validation;

/// <summary>
/// Política de contraseñas seguras de InvestLab, utilizada por
/// <see cref="StrongPasswordAttribute"/> para validar de forma centralizada
/// las contraseñas en los flujos de registro, cambio y recuperación de contraseña.
///
/// Una contraseña válida debe tener entre <see cref="MinLength"/> y
/// <see cref="MaxLength"/> caracteres, e incluir al menos una letra mayúscula,
/// una minúscula, un número y un carácter especial de <see cref="AllowedSpecialCharacters"/>.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 64;

    /// <summary>Caracteres especiales aceptados como válidos por la política.</summary>
    public const string AllowedSpecialCharacters = "!@#$%^&*()-_+=?.,;:/";

    public const string ErrorMessage =
        "La contraseña debe tener entre 8 y 64 caracteres e incluir al menos una letra mayúscula, " +
        "una minúscula, un número y un carácter especial (! @ # $ % ^ & * ( ) - _ + = ? . , ; : /).";

    private static readonly Regex ValidationPattern = new(
        $@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[{Regex.Escape(AllowedSpecialCharacters)}])[A-Za-z\d{Regex.Escape(AllowedSpecialCharacters)}]{{{MinLength},{MaxLength}}}$",
        RegexOptions.Compiled);

    /// <summary>Indica si la contraseña cumple con la política de seguridad.</summary>
    public static bool IsValid(string? password) =>
        !string.IsNullOrEmpty(password) && ValidationPattern.IsMatch(password);
}
