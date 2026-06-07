using InvestLab.Business.Services.Api;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Models.DTOs.Notifications;
using Moq;
using Xunit;

namespace InvestLab.Tests.Business.Services.Api;

/// <summary>
/// Pruebas unitarias de <see cref="NotificationService"/>, cubriendo los métodos invocados desde <c>NotificationController</c>
/// para listar, marcar como leídas, eliminar y consultar la cantidad de notificaciones no leídas del usuario.
/// </summary>
public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private NotificationService CreateService() => new(_notificationRepository.Object, _unitOfWork.Object);

    private static Notification NotificationEntity(int id = 1, int userId = 1, bool isRead = false) =>
        new() { Id = id, AlertId = 1, UserId = userId, Message = "AAPL superó los $150", Price = 150, CreatedAt = DateTime.UtcNow, IsRead = isRead };

    // ---------- GetAsync ----------

    /// <summary>Verifica que, cuando existen notificaciones, se devuelva una respuesta exitosa con los datos mapeados y el total de registros.</summary>
    [Fact]
    public async Task GetAsync_WhenNotificationsExist_ShouldReturnSuccessResponseWithMappedDataAndTotal()
    {
        var notifications = new List<Notification> { NotificationEntity(1), NotificationEntity(2) };
        _notificationRepository.Setup(r => r.GetAsync(1, It.IsAny<NotificationFilterDto>())).ReturnsAsync((notifications, 2));

        var result = await CreateService().GetAsync(1, new NotificationFilterDto());

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    /// <summary>Verifica que, cuando el usuario no tiene notificaciones, se devuelva una respuesta exitosa con una colección vacía.</summary>
    [Fact]
    public async Task GetAsync_WhenUserHasNoNotifications_ShouldReturnSuccessResponseWithEmptyData()
    {
        _notificationRepository.Setup(r => r.GetAsync(1, It.IsAny<NotificationFilterDto>())).ReturnsAsync((new List<Notification>(), 0));

        var result = await CreateService().GetAsync(1, new NotificationFilterDto());

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    // ---------- MarkAsReadAsync ----------

    /// <summary>Verifica que, si la notificación no existe, se devuelva una respuesta de error sin guardar cambios.</summary>
    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationDoesNotExist_ShouldReturnErrorResponse()
    {
        _notificationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Notification?)null);

        var result = await CreateService().MarkAsReadAsync(1, 99);

        Assert.False(result.Success);
        Assert.Equal("Notificación no encontrada", result.Message);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    /// <summary>Verifica que, si la notificación pertenece a otro usuario, se devuelva una respuesta de error sin modificarla.</summary>
    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationBelongsToAnotherUser_ShouldReturnErrorResponse()
    {
        _notificationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(NotificationEntity(1, userId: 2));

        var result = await CreateService().MarkAsReadAsync(1, 1);

        Assert.False(result.Success);
        Assert.Equal("Notificación no encontrada", result.Message);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    /// <summary>Verifica que, con datos válidos, se marque la notificación como leída y se confirmen los cambios.</summary>
    [Fact]
    public async Task MarkAsReadAsync_WhenDataIsValid_ShouldMarkNotificationAsReadAndSaveChanges()
    {
        var notification = NotificationEntity(1, userId: 1, isRead: false);
        _notificationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notification);

        var result = await CreateService().MarkAsReadAsync(1, 1);

        Assert.True(result.Success);
        Assert.True(notification.IsRead);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ---------- MarkAllAsReadAsync ----------

    /// <summary>Verifica que se marquen todas las notificaciones del usuario como leídas y se confirmen los cambios.</summary>
    [Fact]
    public async Task MarkAllAsReadAsync_WhenInvoked_ShouldMarkAllNotificationsAsReadAndSaveChanges()
    {
        var result = await CreateService().MarkAllAsReadAsync(1);

        Assert.True(result.Success);
        _notificationRepository.Verify(r => r.MarkAllAsReadAsync(1), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ---------- DeleteAsync ----------

    /// <summary>Verifica que, si la notificación no existe, se devuelva una respuesta de error sin eliminarla.</summary>
    [Fact]
    public async Task DeleteAsync_WhenNotificationDoesNotExist_ShouldReturnErrorResponse()
    {
        _notificationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Notification?)null);

        var result = await CreateService().DeleteAsync(1, 99);

        Assert.False(result.Success);
        Assert.Equal("Notificación no encontrada", result.Message);
        _notificationRepository.Verify(r => r.Remove(It.IsAny<Notification>()), Times.Never);
    }

    /// <summary>Verifica que, si la notificación pertenece a otro usuario, se devuelva una respuesta de error sin eliminarla.</summary>
    [Fact]
    public async Task DeleteAsync_WhenNotificationBelongsToAnotherUser_ShouldReturnErrorResponse()
    {
        _notificationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(NotificationEntity(1, userId: 2));

        var result = await CreateService().DeleteAsync(1, 1);

        Assert.False(result.Success);
        Assert.Equal("Notificación no encontrada", result.Message);
        _notificationRepository.Verify(r => r.Remove(It.IsAny<Notification>()), Times.Never);
    }

    /// <summary>Verifica que, con datos válidos, se elimine la notificación y se confirmen los cambios.</summary>
    [Fact]
    public async Task DeleteAsync_WhenDataIsValid_ShouldRemoveNotificationAndSaveChanges()
    {
        var notification = NotificationEntity(1, userId: 1);
        _notificationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notification);

        var result = await CreateService().DeleteAsync(1, 1);

        Assert.True(result.Success);
        _notificationRepository.Verify(r => r.Remove(notification), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ---------- GetUnreadCountAsync ----------

    /// <summary>Verifica que, cuando el usuario tiene notificaciones sin leer, se devuelva la cantidad correspondiente.</summary>
    [Fact]
    public async Task GetUnreadCountAsync_WhenUserHasUnreadNotifications_ShouldReturnCount()
    {
        _notificationRepository.Setup(r => r.GetUnreadCountAsync(1)).ReturnsAsync(4);

        var result = await CreateService().GetUnreadCountAsync(1);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    /// <summary>Verifica que, cuando el usuario no tiene notificaciones sin leer, se devuelva una cantidad igual a cero.</summary>
    [Fact]
    public async Task GetUnreadCountAsync_WhenUserHasNoUnreadNotifications_ShouldReturnZero()
    {
        _notificationRepository.Setup(r => r.GetUnreadCountAsync(1)).ReturnsAsync(0);

        var result = await CreateService().GetUnreadCountAsync(1);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }
}
