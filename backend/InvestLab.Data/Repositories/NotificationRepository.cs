using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using InvestLab.Models.DTOs.Notifications;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly InvestLabDbContext _context;

    public NotificationRepository(InvestLabDbContext context)
    {
        _context = context;
    }

    public async Task<(List<Notification>, int)> GetAsync(int userId, NotificationFilterDto filter)
    {
        var query = _context.Notifications
            .Where(x => x.UserId == userId);

        if (filter.IsRead.HasValue)
            query = query.Where(x => x.IsRead == filter.IsRead.Value);

        var total = await query.CountAsync();

        var data = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (data, total);
    }

    public async Task<Notification?> GetByIdAsync(int id)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .CountAsync(x => x.UserId == userId && !x.IsRead);
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var list = await _context.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ToListAsync();

        foreach (var n in list)
            n.IsRead = true;
    }

    public void Remove(Notification notification)
    {
        _context.Notifications.Remove(notification);
    }
}

