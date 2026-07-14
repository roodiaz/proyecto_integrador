namespace InvestLab.Models.DTOs.Portfolio;

/// <summary>
/// Datos para ejecutar una venta de un activo dentro de un portfolio.
/// </summary>
public class SellAssetDto
{
    /// <summary>Símbolo (ticker) del activo a vender.</summary>
    public string Symbol { get; set; }

    /// <summary>Cantidad de unidades a vender.</summary>
    public decimal Quantity { get; set; }
}