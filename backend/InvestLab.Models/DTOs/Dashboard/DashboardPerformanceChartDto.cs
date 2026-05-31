namespace InvestLab.Models.DTOs.Dashboard;

public class DashboardPerformanceChartDto
{
    public decimal CurrentValue { get; set; }

    public decimal VariationPercent { get; set; }

    public string VariationText { get; set; } = string.Empty;

    public List<DashboardPerformanceChartPointDto> Data { get; set; } = [];
}