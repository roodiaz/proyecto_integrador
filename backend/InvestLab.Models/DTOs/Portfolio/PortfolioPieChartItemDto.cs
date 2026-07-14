namespace InvestLab.Models.DTOs.Portfolio;

/// <summary>
/// Participación de un activo dentro del portfolio, usada para el gráfico de torta.
/// </summary>
public class PortfolioPieChartItemDto
{
    /// <summary>Símbolo (ticker) del activo.</summary>
    public string Symbol { get; set; } = default!;

    /// <summary>Valor actual de la posición en el portfolio.</summary>
    public decimal CurrentValue { get; set; }

    /// <summary>Porcentaje que representa dentro del valor total del portfolio.</summary>
    public decimal Percentage { get; set; }
}