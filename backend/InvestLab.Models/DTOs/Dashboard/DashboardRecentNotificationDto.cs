namespace InvestLab.Models.DTOs.Dashboard;

public class DashboardRecentNotificationDto
{
    public string Message { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }
}