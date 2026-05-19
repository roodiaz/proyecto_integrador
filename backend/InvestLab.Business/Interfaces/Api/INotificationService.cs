using InvestLab.Models;
using InvestLab.Models.DTOs.Notifications;

namespace InvestLab.Business.Interfaces.Api;

public interface INotificationService
{
    Task<Response> GetAsync(int userId, NotificationFilterDto filter);

    Task<Response> MarkAsReadAsync(int userId, int id);

    Task<Response> MarkAllAsReadAsync(int userId);

    Task<Response> DeleteAsync(int userId, int id);

    Task<Response> GetUnreadCountAsync(int userId);
}
