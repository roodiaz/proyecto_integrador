using InvestLab.Business.Interfaces.Api;
using InvestLab.Data.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Notifications;

namespace InvestLab.Business.Services.Api;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepo;
    private readonly IUnitOfWork _uow;

    /// <summary>
    /// Inicializa una nueva instancia del servicio de notificaciones.
    /// </summary>
    /// <param name="repo">Repositorio de notificaciones.</param>
    /// <param name="uow">Unidad de trabajo para confirmar los cambios en la base de datos.</param>
    public NotificationService(INotificationRepository repo, IUnitOfWork uow)
    {
        _notificationRepo = repo;
        _uow = uow;
    }

    /// <summary>
    /// Obtiene el listado paginado de notificaciones de un usuario aplicando los filtros indicados.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="filter">Filtros a aplicar sobre el listado de notificaciones.</param>
    /// <returns>Respuesta con los datos de las notificaciones y el total de registros encontrados.</returns>
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

    /// <summary>
    /// Marca una notificación específica como leída, validando que pertenezca al usuario indicado.
    /// </summary>
    /// <param name="userId">Identificador del usuario propietario de la notificación.</param>
    /// <param name="id">Identificador de la notificación a marcar como leída.</param>
    /// <returns>Respuesta indicando si la operación fue exitosa o si la notificación no fue encontrada.</returns>
    public async Task<Response> MarkAsReadAsync(int userId, int id)
    {
        var n = await _notificationRepo.GetByIdAsync(id);

        if (n == null || n.UserId != userId)
            return Response.Fail("Notificación no encontrada");

        n.IsRead = true;

        await _uow.SaveChangesAsync();

        return Response.Ok(null);
    }

    /// <summary>
    /// Marca todas las notificaciones de un usuario como leídas.
    /// </summary>
    /// <param name="userId">Identificador del usuario cuyas notificaciones se marcarán como leídas.</param>
    /// <returns>Respuesta indicando que la operación se completó correctamente.</returns>
    public async Task<Response> MarkAllAsReadAsync(int userId)
    {
        await _notificationRepo.MarkAllAsReadAsync(userId);

        await _uow.SaveChangesAsync();

        return Response.Ok(null);
    }

    /// <summary>
    /// Elimina una notificación específica, validando que pertenezca al usuario indicado.
    /// </summary>
    /// <param name="userId">Identificador del usuario propietario de la notificación.</param>
    /// <param name="id">Identificador de la notificación a eliminar.</param>
    /// <returns>Respuesta indicando si la operación fue exitosa o si la notificación no fue encontrada.</returns>
    public async Task<Response> DeleteAsync(int userId, int id)
    {
        var n = await _notificationRepo.GetByIdAsync(id);

        if (n == null || n.UserId != userId)
            return Response.Fail("Notificación no encontrada");

        _notificationRepo.Remove(n);

        await _uow.SaveChangesAsync();

        return Response.Ok(null);
    }

    /// <summary>
    /// Obtiene la cantidad de notificaciones no leídas de un usuario.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <returns>Respuesta con la cantidad de notificaciones no leídas.</returns>
    public async Task<Response> GetUnreadCountAsync(int userId)
    {
        var count = await _notificationRepo.GetUnreadCountAsync(userId);

        return Response.Ok(new { count });
    }
}
