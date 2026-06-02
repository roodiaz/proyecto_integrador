namespace InvestLab.Models.DTOs.Notifications;

public class NotificationFilterDto
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public bool? IsRead { get; set; }

    public string? Search { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }
}
