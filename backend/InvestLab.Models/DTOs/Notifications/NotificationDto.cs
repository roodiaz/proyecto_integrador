namespace InvestLab.Models.DTOs.Notifications;

public class NotificationDto
{
    public int Id { get; set; }
    public int AlertId { get; set; }
    public string? Message { get; set; }
    public decimal? Price { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? AlertSymbol { get; set; }
    public string? AlertCondition { get; set; }
}
