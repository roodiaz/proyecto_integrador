namespace InvestLab.Models.DTOs.Portfolio;

/// <summary>
/// Resumen de saldo y rendimiento de un portfolio, utilizado en las cards principales.
/// </summary>
public class PortfolioBalanceCardsDto
{
    /// <summary>Nombre del portfolio.</summary>
    public string? PortfolioName { get; set; }

    /// <summary>Saldo disponible actual (efectivo, sin invertir).</summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>Valor total del portfolio (efectivo + posiciones abiertas).</summary>
    public decimal TotalBalance { get; set; }

    /// <summary>Ganancia o pérdida total respecto al saldo inicial.</summary>
    public decimal ProfitLoss { get; set; }

    /// <summary>Ganancia o pérdida total expresada en porcentaje.</summary>
    public decimal ProfitLossPercent { get; set; }

    /// <summary>Ganancia o pérdida ya realizada (posiciones cerradas/vendidas).</summary>
    public decimal RealizedProfitLoss { get; set; }

    /// <summary>Ganancia o pérdida no realizada (posiciones abiertas actualmente).</summary>
    public decimal UnrealizedProfitLoss { get; set; }

    /// <summary>Cantidad de operaciones realizadas en el día.</summary>
    public int TotalOperations { get; set; }

    /// <summary>Cantidad máxima de operaciones diarias permitidas.</summary>
    public int MaxOperations { get; set; }

    /// <summary>Fecha del último cierre de mercado considerado para el cálculo.</summary>
    public DateTime? LastMarketCloseDate { get; set; } = DateTime.UtcNow;
}