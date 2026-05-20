namespace InvestLab.Models.DTOs.Portfolio;

public class PortfolioPositionDto
{

    public string Symbol { get; set; } = default!;

    public decimal Quantity { get; set; }

    public decimal AvgPrice { get; set; }

    public decimal CurrentPrice { get; set; }
}