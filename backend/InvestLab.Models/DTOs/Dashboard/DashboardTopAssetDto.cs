namespace InvestLab.Models.DTOs.Dashboard;

public class DashboardTopAssetDto
{
    public string Symbol { get; set; } = default!;

    public decimal CurrentPrice { get; set; }

    public decimal ProfitLoss { get; set; }

    public decimal ProfitLossPercent { get; set; }
}