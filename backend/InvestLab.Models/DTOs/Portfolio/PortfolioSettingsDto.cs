namespace InvestLab.Models.DTOs.Portfolio;

/// <summary>
/// Configuración actual del portfolio de simulación de un usuario:
/// nombre, saldo inicial y si ya completó el wizard de configuración.
/// </summary>
public class PortfolioSettingsDto
{
    public string? PortfolioName { get; set; }

    public decimal InitialBalance { get; set; }

    public bool PortfolioConfigured { get; set; }
}
