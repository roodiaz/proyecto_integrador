using InvestLab.Business.Interfaces.Workers;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Integrations.Interfaces;
using static InvestLab.Models.Enums;

namespace InvestLab.Business.Services.Workers;

/// <summary>
/// Servicio encargado de procesar
/// alertas bursátiles.
/// </summary>
public class AlertProcessingService : IAlertProcessingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAlertRepository _alertRepository;
    private readonly IUserSettingRepository _userSettingRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IExternalProvider _externalProvider;
    private readonly IEmailService _emailService;

    public AlertProcessingService(IAlertRepository alertRepository, IUserSettingRepository userSettingRepository, INotificationRepository notificationRepository, IExternalProvider externalProvider, IEmailService emailService, IUnitOfWork unitOfWork)
    {
        _alertRepository = alertRepository;
        _userSettingRepository = userSettingRepository;
        _notificationRepository = notificationRepository;
        _externalProvider = externalProvider;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
    }

    public async Task ProcessAlertsAsync()
    {
        var alerts = await _alertRepository
            .GetActiveAlertsAsync();

        foreach (var alert in alerts)
        {
            var market = await _externalProvider.GetPriceAsync(alert.Asset.Symbol);
            if (market == null)
                continue;

            var value = alert.ConditionType == ConditionType.Price ? market.Price : market.VariationPercent;

            var triggered = EvaluateCondition(value, alert.Operator, alert.Value);

            if (!triggered)
                continue;

            // evita duplicar alertas en el mismo día
            if (alert.LastTriggered?.Date == DateTime.UtcNow.Date)
                continue;

            var notification = new Notification
            {
                UserId = alert.UserId,
                CreatedAt = DateTime.UtcNow,
                Message = BuildAlertMessage(alert, value),
                AlertId = alert.Id,
                Price = market.Price,
            };

            await _notificationRepository.InsertAsync(notification);

            var settings = await _userSettingRepository.GetByUserIdAsync(alert.UserId);

            if (settings?.EmailNotifications == true)
            {
                await _emailService.SendAsync("mail@test.com", "Alerta InvestLab", notification.Message);
            }

            alert.LastTriggered = DateTime.UtcNow;

            await _alertRepository.UpdateAsync(alert);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    private static bool EvaluateCondition(decimal currentValue, AlertOperator op, decimal target)
    {
        return op switch
        {
            AlertOperator.GreaterThan => currentValue > target,
            AlertOperator.LessThan => currentValue < target,
            AlertOperator.GreaterThanOrEqual => currentValue >= target,
            AlertOperator.LessThanOrEqual => currentValue <= target,
            AlertOperator.Equal => currentValue == target,
            _ => false
        };
    }

    private static string BuildAlertMessage(Alert alert, decimal currentValue)
    {
        var symbol = alert.Asset.Symbol;

        if ((ConditionType)alert.ConditionType == ConditionType.Price)
        {
            return alert.Operator switch
            {
                AlertOperator.GreaterThan => $"{symbol} ha superado los " + $"${alert.Value}",
                AlertOperator.LessThan => $"{symbol} ha bajado por debajo de " + $"${alert.Value}",
                AlertOperator.GreaterThanOrEqual => $"{symbol} ha alcanzado o superado " + $"los ${alert.Value}",
                AlertOperator.LessThanOrEqual => $"{symbol} ha alcanzado o caído " + $"por debajo de ${alert.Value}",
                AlertOperator.Equal => $"{symbol} ha alcanzado el precio " + $"objetivo de ${alert.Value}",
                _ => $"{symbol} activó una alerta"
            };
        }

        return alert.Operator switch
        {
            AlertOperator.GreaterThan => $"{symbol} ha subido más de " + $"{alert.Value}%",
            AlertOperator.LessThan => $"{symbol} ha caído más de " + $"{Math.Abs(alert.Value)}%",
            _ => $"{symbol} activó una alerta"
        };
    }
}