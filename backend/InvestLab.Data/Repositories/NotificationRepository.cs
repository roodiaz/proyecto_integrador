using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using InvestLab.Models.DTOs.Notifications;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly InvestLabDbContext _context;

    /// <summary>
    /// Inicializa una nueva instancia del repositorio de notificaciones.
    /// </summary>
    /// <param name="context">Contexto de base de datos de InvestLab.</param>
    public NotificationRepository(InvestLabDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene las notificaciones de un usuario aplicando filtros de estado de lectura, búsqueda por texto y rango de fechas, devolviendo los resultados paginados.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="filter">Criterios de filtrado y paginación a aplicar sobre la búsqueda.</param>
    /// <returns>Tupla con la lista de notificaciones que cumplen el filtro y el total de registros encontrados.</returns>
    public async Task<(List<Notification>, int)> GetAsync(int userId, NotificationFilterDto filter)
    {
        var query = _context.Notifications
            .Where(x => x.UserId == userId)
            .AsQueryable();

        if (filter.IsRead.HasValue)
            query = query.Where(x => x.IsRead == filter.IsRead.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(x => x.Message.ToLower().Contains(search));
        }

        if (filter.FromDate.HasValue)
            query = query.Where(x => x.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(x => x.CreatedAt <= filter.ToDate.Value);

        var total = await query.CountAsync();

        var data = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (data, total);
    }
    /// <summary>
    /// Busca una notificación por su identificador.
    /// </summary>
    /// <param name="id">Identificador de la notificación.</param>
    /// <returns>La notificación encontrada, o <c>null</c> si no existe.</returns>
    public async Task<Notification?> GetByIdAsync(int id)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <summary>
    /// Cuenta la cantidad de notificaciones no leídas de un usuario.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <returns>Cantidad de notificaciones no leídas.</returns>
    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .CountAsync(x => x.UserId == userId && !x.IsRead);
    }

    /// <summary>
    /// Marca como leídas todas las notificaciones no leídas de un usuario.
    /// </summary>
    /// <param name="userId">Identificador del usuario cuyas notificaciones se marcarán como leídas.</param>
    public async Task MarkAllAsReadAsync(int userId)
    {
        var list = await _context.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ToListAsync();

        foreach (var n in list)
            n.IsRead = true;
    }

    /// <summary>
    /// Elimina una notificación del contexto.
    /// </summary>
    /// <param name="notification">Notificación a eliminar.</param>
    public void Remove(Notification notification)
    {
        _context.Notifications.Remove(notification);
    }

    /// <summary>
    /// Agrega una nueva notificación al contexto para su posterior persistencia.
    /// </summary>
    /// <param name="notification">Notificación a insertar.</param>
    public async Task InsertAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
    }

    /// <summary>
    /// Obtiene las últimas notificaciones de un usuario, ordenadas por fecha de creación descendente.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="limit">Cantidad máxima de notificaciones a obtener.</param>
    /// <returns>Lista de las notificaciones más recientes del usuario.</returns>
    public async Task<List<Notification>> GetLatestByUserAsync(int userId, int limit)
    {
        return await _context.Notifications
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Elimina todas las notificaciones asociadas a un usuario.
    /// </summary>
    /// <param name="userId">Identificador del usuario cuyas notificaciones se eliminarán.</param>
    public async Task DeleteByUserIdAsync(int userId)
    {
        await _context.Notifications.Where(x => x.UserId == userId).ExecuteDeleteAsync();
    }
}

