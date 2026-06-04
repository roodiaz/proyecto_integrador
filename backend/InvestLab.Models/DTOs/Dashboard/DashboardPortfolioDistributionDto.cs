namespace InvestLab.Models.DTOs.Dashboard;

public class DashboardPortfolioDistributionDto
{
    public string Sector { get; set; } = string.Empty;

    public decimal Value { get; set; }

    public decimal Percentage { get; set; }
}