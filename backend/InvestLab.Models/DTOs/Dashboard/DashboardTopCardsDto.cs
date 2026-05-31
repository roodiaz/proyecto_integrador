namespace InvestLab.Models.DTOs.Dashboard;

public class DashboardTopCardsDto
{
    public decimal TotalValue { get; set; }

    public decimal TodayProfit { get; set; }

    public decimal TodayProfitPercent { get; set; }

    public int ActiveAssets { get; set; }
}