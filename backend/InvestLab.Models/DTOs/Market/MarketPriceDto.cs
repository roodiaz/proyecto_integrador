namespace InvestLab.Models.DTOs.Market;

public class MarketPriceDto
{
    public string Symbol { get; set; }
    public decimal Price { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal VariationPercent { get; set; }
}
