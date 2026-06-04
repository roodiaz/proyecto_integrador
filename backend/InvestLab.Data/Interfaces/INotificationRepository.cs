using InvestLab.Models.DTOs.Notifications;

namespace InvestLab.Data.Interfaces;

public interface INotificationRepository
{
    Task<(List<Notification>, int total)> GetAsync(int userId, NotificationFilterDto filter);

    Task<Notification?> GetByIdAsync(int id);

    Task<int> GetUnreadCountAsync(int userId);

    Task MarkAllAsReadAsync(int userId);

    void Remove(Notification notification);

    Task InsertAsync(Notification notification);

    Task<List<Notification>> GetLatestByUserAsync(int userId, int limit);
}
