namespace InvestLab.Models.DTOs.Portfolio;

/// <summary>
/// Posición actual de un activo dentro de un portfolio, usada para validar una venta.
/// </summary>
public class PortfolioPositionDto
{
    /// <summary>Símbolo (ticker) del activo.</summary>
    public string Symbol { get; set; } = default!;

    /// <summary>Cantidad de unidades en tenencia.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Precio promedio de compra de la posición.</summary>
    public decimal AvgPrice { get; set; }

    /// <summary>Precio actual de mercado del activo.</summary>
    public decimal CurrentPrice { get; set; }
}