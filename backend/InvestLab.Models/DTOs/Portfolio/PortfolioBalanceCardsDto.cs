namespace InvestLab.Models.DTOs.Portfolio;

public class PortfolioBalanceCardsDto
{
    public string? PortfolioName { get; set; }

    public decimal CurrentBalance { get; set; }

    public decimal TotalBalance { get; set; }

    public decimal ProfitLoss { get; set; }

    public decimal ProfitLossPercent { get; set; }

    public decimal RealizedProfitLoss { get; set; }

    public decimal UnrealizedProfitLoss { get; set; }

    public int TotalOperations { get; set; }

    public int MaxOperations { get; set; }

    public DateTime? LastMarketCloseDate { get; set; } = DateTime.UtcNow;
}