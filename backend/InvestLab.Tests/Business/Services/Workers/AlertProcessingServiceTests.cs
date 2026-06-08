using InvestLab.Business.Services.Workers;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models.DTOs.Market;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static InvestLab.Models.Enums;

namespace InvestLab.Tests.Business.Services.Workers;

/// <summary>
/// Pruebas unitarias de <see cref="AlertProcessingService"/>, cubriendo el procesamiento de alertas activas:
/// la evaluación de condiciones de mercado, la generación de notificaciones, la actualización de la alerta disparada
/// y el envío de correos electrónicos a los usuarios.
/// </summary>
public class AlertProcessingServiceTests
{
    private readonly Mock<IAlertRepository> _alertRepository = new();
    private readonly Mock<IUserSettingRepository> _userSettingRepository = new();
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<IExternalProvider> _externalProvider = new();
    private readonly Mock<IMarketProviderResolver> _providerResolver = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<AlertProcessingService>> _logger = new();

    private AlertProcessingService CreateService()
    {
        _providerResolver.Setup(x => x.GetProvider()).Returns(_externalProvider.Object);
        return new(_alertRepository.Object, _userSettingRepository.Object, _notificationRepository.Object, _providerResolver.Object, _emailService.Object, _unitOfWork.Object, _logger.Object);
    }

    private static Asset AssetEntity(string symbol = "AAPL") => new() { Id = 1, Symbol = symbol };

    private static Alert AlertEntity(ConditionType conditionType = ConditionType.Price, AlertOperator op = AlertOperator.GreaterThan, decimal value = 100, DateTime? lastTriggered = null) =>
        new() { Id = 1, UserId = 1, AssetId = 1, ConditionType = conditionType, Operator = op, Value = value, IsActive = true, LastTriggered = lastTriggered, Asset = AssetEntity() };

    private static MarketPriceDto Price(decimal price = 150, decimal variationPercent = 5) => new() { Symbol = "AAPL", Price = price, VariationPercent = variationPercent };

    private static UserSetting Settings(bool emailNotifications) => new() { Id = 1, UserId = 1, EmailNotifications = emailNotifications };

    // ---------- ProcessAlertsAsync ----------

    /// <summary>Verifica que, cuando no hay alertas activas, no se genere ninguna notificación ni se consulte el precio de mercado.</summary>
    [Fact]
    public async Task ProcessAlertsAsync_WhenThereAreNoActiveAlerts_ShouldNotGenerateNotifications()
    {
        _alertRepository.Setup(r => r.GetActiveAlertsAsync()).ReturnsAsync(new List<Alert>());

        await CreateService().ProcessAlertsAsync();

        _externalProvider.Verify(p => p.GetPriceAsync(It.IsAny<string>()), Times.Never);
        _notificationRepository.Verify(r => r.InsertAsync(It.IsAny<Notification>()), Times.Never);
    }

    /// <summary>Verifica que, si no se puede obtener el precio de mercado del activo, la alerta se omita sin generar notificación.</summary>
    [Fact]
    public async Task ProcessAlertsAsync_WhenMarketPriceIsUnavailable_ShouldSkipAlert()
    {
        _alertRepository.Setup(r => r.GetActiveAlertsAsync()).ReturnsAsync(new List<Alert> { AlertEntity() });
        _externalProvider.Setup(p => p.GetPriceAsync(It.IsAny<string>())).ReturnsAsync((MarketPriceDto?)null);

        await CreateService().ProcessAlertsAsync();

        _notificationRepository.Verify(r => r.InsertAsync(It.IsAny<Notification>()), Times.Never);
    }

    /// <summary>Verifica que, si la condición configurada en la alerta no se cumple con el valor actual de mercado, no se genere notificación.</summary>
    [Fact]
    public async Task ProcessAlertsAsync_WhenConditionIsNotMet_ShouldSkipAlert()
    {
        _alertRepository.Setup(r => r.GetActiveAlertsAsync()).ReturnsAsync(new List<Alert> { AlertEntity(op: AlertOperator.GreaterThan, value: 200) });
        _externalProvider.Setup(p => p.GetPriceAsync(It.IsAny<string>())).ReturnsAsync(Price(price: 150));

        await CreateService().ProcessAlertsAsync();

        _notificationRepository.Verify(r => r.InsertAsync(It.IsAny<Notification>()), Times.Never);
    }

    /// <summary>Verifica que, si la alerta ya se disparó en el día actual, se omita para evitar notificaciones duplicadas.</summary>
    [Fact]
    public async Task ProcessAlertsAsync_WhenAlertAlreadyTriggeredToday_ShouldSkipAlert()
    {
        _alertRepository.Setup(r => r.GetActiveAlertsAsync()).ReturnsAsync(new List<Alert> { AlertEntity(lastTriggered: DateTime.UtcNow) });
        _externalProvider.Setup(p => p.GetPriceAsync(It.IsAny<string>())).ReturnsAsync(Price(price: 150));

        await CreateService().ProcessAlertsAsync();

        _notificationRepository.Verify(r => r.InsertAsync(It.IsAny<Notification>()), Times.Never);
        _alertRepository.Verify(r => r.UpdateAsync(It.IsAny<Alert>()), Times.Never);
    }

    /// <summary>Verifica que, cuando se cumple la condición de la alerta, se genere la notificación correspondiente, se actualice la fecha de disparo y se confirmen los cambios.</summary>
    [Fact]
    public async Task ProcessAlertsAsync_WhenConditionIsMet_ShouldCreateNotificationAndUpdateAlert()
    {
        var alert = AlertEntity(conditionType: ConditionType.Price, op: AlertOperator.GreaterThan, value: 100);
        _alertRepository.Setup(r => r.GetActiveAlertsAsync()).ReturnsAsync(new List<Alert> { alert });
        _externalProvider.Setup(p => p.GetPriceAsync("AAPL")).ReturnsAsync(Price(price: 150));
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings(emailNotifications: false));

        await CreateService().ProcessAlertsAsync();

        _notificationRepository.Verify(r => r.InsertAsync(It.Is<Notification>(n => n.UserId == 1 && n.AlertId == 1 && n.Price == 150 && n.Message!.Contains("AAPL"))), Times.Once);
        Assert.NotNull(alert.LastTriggered);
        _alertRepository.Verify(r => r.UpdateAsync(alert), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /// <summary>Verifica que, si el usuario tiene habilitadas las notificaciones por correo, se envíe el email de alerta.</summary>
    [Fact]
    public async Task ProcessAlertsAsync_WhenUserHasEmailNotificationsEnabled_ShouldSendEmail()
    {
        _alertRepository.Setup(r => r.GetActiveAlertsAsync()).ReturnsAsync(new List<Alert> { AlertEntity() });
        _externalProvider.Setup(p => p.GetPriceAsync(It.IsAny<string>())).ReturnsAsync(Price(price: 150));
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings(emailNotifications: true));

        await CreateService().ProcessAlertsAsync();

        _emailService.Verify(e => e.SendAsync(It.IsAny<string>(), "Alerta InvestLab", It.IsAny<string>()), Times.Once);
    }

    /// <summary>Verifica que, si el usuario no tiene habilitadas las notificaciones por correo, no se envíe ningún email.</summary>
    [Fact]
    public async Task ProcessAlertsAsync_WhenUserHasEmailNotificationsDisabled_ShouldNotSendEmail()
    {
        _alertRepository.Setup(r => r.GetActiveAlertsAsync()).ReturnsAsync(new List<Alert> { AlertEntity() });
        _externalProvider.Setup(p => p.GetPriceAsync(It.IsAny<string>())).ReturnsAsync(Price(price: 150));
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings(emailNotifications: false));

        await CreateService().ProcessAlertsAsync();

        _emailService.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>Verifica que, si el envío del email de alerta falla, el error se registre sin interrumpir el procesamiento de la alerta.</summary>
    [Fact]
    public async Task ProcessAlertsAsync_WhenEmailServiceThrows_ShouldLogErrorAndContinue()
    {
        var alert = AlertEntity();
        _alertRepository.Setup(r => r.GetActiveAlertsAsync()).ReturnsAsync(new List<Alert> { alert });
        _externalProvider.Setup(p => p.GetPriceAsync(It.IsAny<string>())).ReturnsAsync(Price(price: 150));
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings(emailNotifications: true));
        _emailService.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new Exception("smtp error"));

        var exception = await Record.ExceptionAsync(() => CreateService().ProcessAlertsAsync());

        Assert.Null(exception);
        _notificationRepository.Verify(r => r.InsertAsync(It.IsAny<Notification>()), Times.Once);
    }

    /// <summary>Verifica que, si ocurre un error al procesar una alerta, este se registre y se continúe procesando el resto de las alertas activas.</summary>
    [Fact]
    public async Task ProcessAlertsAsync_WhenProcessingOneAlertThrows_ShouldLogErrorAndContinueWithOthers()
    {
        var failingAlert = AlertEntity();
        var workingAlert = new Alert { Id = 2, UserId = 2, AssetId = 2, ConditionType = ConditionType.Price, Operator = AlertOperator.GreaterThan, Value = 100, IsActive = true, Asset = new Asset { Id = 2, Symbol = "MSFT" } };

        _alertRepository.Setup(r => r.GetActiveAlertsAsync()).ReturnsAsync(new List<Alert> { failingAlert, workingAlert });
        _externalProvider.Setup(p => p.GetPriceAsync("AAPL")).ThrowsAsync(new Exception("provider error"));
        _externalProvider.Setup(p => p.GetPriceAsync("MSFT")).ReturnsAsync(new MarketPriceDto { Symbol = "MSFT", Price = 150 });
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(It.IsAny<int>())).ReturnsAsync((UserSetting?)null);

        await CreateService().ProcessAlertsAsync();

        _notificationRepository.Verify(r => r.InsertAsync(It.Is<Notification>(n => n.AlertId == 2)), Times.Once);
    }
}
