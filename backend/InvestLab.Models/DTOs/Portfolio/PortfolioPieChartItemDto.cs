namespace InvestLab.Models.DTOs.Portfolio;

public class PortfolioPieChartItemDto
{
    public string Symbol { get; set; } = default!;

    public decimal CurrentValue { get; set; }

    public decimal Percentage { get; set; }
}