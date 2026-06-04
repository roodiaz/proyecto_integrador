using InvestLab.Business.Interfaces.Api;
using InvestLab.Data.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Notifications;

namespace InvestLab.Business.Services.Api;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepo;
    private readonly IUnitOfWork _uow;

    public NotificationService(INotificationRepository repo, IUnitOfWork uow)
    {
        _notificationRepo = repo;
        _uow = uow;
    }

    public async Task<Response> GetAsync(int userId, NotificationFilterDto filter)
    {
        var (data, total) = await _notificationRepo.GetAsync(userId, filter);

        var result = data.Select(x => new NotificationDto
        {
            Id = x.Id,
            AlertId = x.AlertId,
            Message = x.Message,
            Price = x.Price,
            IsRead = x.IsRead,
            CreatedAt = x.CreatedAt
        });

        return Response.Ok(new { data = result, total });
    }

    public async Task<Response> MarkAsReadAsync(int userId, int id)
    {
        var n = await _notificationRepo.GetByIdAsync(id);

        if (n == null || n.UserId != userId)
            return Response.Fail("Notificación no encontrada");

        n.IsRead = true;

        await _uow.SaveChangesAsync();

        return Response.Ok(null);
    }

    public async Task<Response> MarkAllAsReadAsync(int userId)
    {
        await _notificationRepo.MarkAllAsReadAsync(userId);

        await _uow.SaveChangesAsync();

        return Response.Ok(null);
    }

    public async Task<Response> DeleteAsync(int userId, int id)
    {
        var n = await _notificationRepo.GetByIdAsync(id);

        if (n == null || n.UserId != userId)
            return Response.Fail("Notificación no encontrada");

        _notificationRepo.Remove(n);

        await _uow.SaveChangesAsync();

        return Response.Ok(null);
    }

    public async Task<Response> GetUnreadCountAsync(int userId)
    {
        var count = await _notificationRepo.GetUnreadCountAsync(userId);

        return Response.Ok(new { count });
    }
}
