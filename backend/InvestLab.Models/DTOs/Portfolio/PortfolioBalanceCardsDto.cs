namespace InvestLab.Models.DTOs.Portfolio;

public class PortfolioBalanceCardsDto
{
    public decimal InitialBalance { get; set; }

    public decimal CurrentBalance { get; set; }

    public decimal ProfitLoss { get; set; }

    public decimal ProfitLossPercent { get; set; }
}