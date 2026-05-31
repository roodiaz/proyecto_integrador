namespace InvestLab.Models.DTOs.Dashboard;

public class DashboardActiveAlertDto
{
    public string Symbol { get; set; } = default!;

    public string Condition { get; set; } = default!;

    public decimal Value { get; set; }

    public bool IsActive { get; set; }
}