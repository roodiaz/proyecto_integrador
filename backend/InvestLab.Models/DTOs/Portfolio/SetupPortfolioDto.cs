using System.ComponentModel.DataAnnotations;
using InvestLab.Models.Validation;

namespace InvestLab.Models.DTOs.Portfolio;

/// <summary>
/// Datos para configurar (o reconfigurar, en caso de reinicio) el portfolio
/// de simulación de un usuario: nombre del portfolio y saldo inicial.
/// </summary>
public class SetupPortfolioDto
{
    /// <summary>Nombre elegido para el portfolio.</summary>
    [Required(ErrorMessage = "El nombre del portfolio es obligatorio")]
    [PortfolioName]
    public string PortfolioName { get; set; } = null!;

    /// <summary>Saldo virtual inicial con el que arranca el portfolio.</summary>
    [InitialBalanceRange]
    public decimal InitialBalance { get; set; }
}
