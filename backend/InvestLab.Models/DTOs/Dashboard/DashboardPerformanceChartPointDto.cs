namespace InvestLab.Models.DTOs.Dashboard;

public class DashboardPerformanceChartPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Portfolio { get; set; }
    public decimal Sp500 { get; set; }
    public decimal Nasdaq { get; set; }
    public decimal PortfolioValue { get; set; }
    public decimal Sp500Value { get; set; }
    public decimal NasdaqValue { get; set; }
}
