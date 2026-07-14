namespace InvestLab.Models.DTOs.Portfolio;

/// <summary>
/// Datos para ejecutar una compra de un activo dentro de un portfolio.
/// </summary>
public class BuyAssetDto
{
    /// <summary>Símbolo (ticker) del activo a comprar.</summary>
    public string Symbol { get; set; }

    /// <summary>Cantidad de unidades a comprar.</summary>
    public decimal Quantity { get; set; }
}