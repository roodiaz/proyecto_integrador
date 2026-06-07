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

    /// <summary>
    /// Inicializa una nueva instancia del servicio de procesamiento de alertas.
    /// </summary>
    /// <param name="alertRepository">Repositorio utilizado para obtener y actualizar las alertas.</param>
    /// <param name="userSettingRepository">Repositorio utilizado para obtener la configuración del usuario.</param>
    /// <param name="notificationRepository">Repositorio utilizado para registrar las notificaciones generadas.</param>
    /// <param name="externalProvider">Proveedor externo utilizado para obtener los precios de mercado.</param>
    /// <param name="emailService">Servicio utilizado para el envío de correos electrónicos de notificación.</param>
    /// <param name="unitOfWork">Unidad de trabajo utilizada para confirmar los cambios en la base de datos.</param>
    public AlertProcessingService(IAlertRepository alertRepository, IUserSettingRepository userSettingRepository, INotificationRepository notificationRepository, IExternalProvider externalProvider, IEmailService emailService, IUnitOfWork unitOfWork)
    {
        _alertRepository = alertRepository;
        _userSettingRepository = userSettingRepository;
        _notificationRepository = notificationRepository;
        _externalProvider = externalProvider;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Procesa las alertas activas: consulta el precio de mercado actual de cada activo,
    /// evalúa si se cumple la condición configurada y, en caso afirmativo, genera una notificación,
    /// actualiza la alerta y, si corresponde, envía un correo electrónico al usuario.
    /// </summary>
    /// <returns>Una tarea que representa la operación asincrónica de procesamiento de alertas.</returns>
    public async Task ProcessAlertsAsync()
    {
        var alerts = await _alertRepository.GetActiveAlertsAsync();

        foreach (var alert in alerts)
        {
            try
            {
                var market = await _externalProvider.GetPriceAsync(alert.Asset.Symbol);
                if (market == null)
                    continue;

                var value = alert.ConditionType == ConditionType.Price ? market.Price : market.VariationPercent;
                var triggered = EvaluateCondition(value, alert.Operator, alert.Value);

                if (!triggered)
                    continue;

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

                alert.LastTriggered = DateTime.UtcNow;

                await _alertRepository.UpdateAsync(alert);
                await _unitOfWork.SaveChangesAsync();

                var settings = await _userSettingRepository.GetByUserIdAsync(alert.UserId);

                if (settings?.EmailNotifications == true)
                {
                    try
                    {
                        await _emailService.SendAsync("mail@test.com", "Alerta InvestLab", notification.Message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al enviar email de alerta para el usuario {UserId} y alerta {AlertId}", alert.UserId, alert.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar la alerta {AlertId}", alert.Id);
            }
        }
    }
    /// <summary>
    /// Evalúa si el valor actual cumple la condición definida por el operador de la alerta
    /// en comparación con el valor objetivo.
    /// </summary>
    /// <param name="currentValue">Valor actual obtenido del mercado (precio o variación porcentual).</param>
    /// <param name="op">Operador de comparación configurado en la alerta.</param>
    /// <param name="target">Valor objetivo contra el cual se compara el valor actual.</param>
    /// <returns>true si la condición se cumple; en caso contrario, false.</returns>
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

    /// <summary>
    /// Construye el mensaje descriptivo de la notificación que se enviará al usuario
    /// según el tipo de condición y el operador configurados en la alerta.
    /// </summary>
    /// <param name="alert">Alerta que se activó y para la cual se debe generar el mensaje.</param>
    /// <param name="currentValue">Valor actual (precio o variación porcentual) que originó la activación de la alerta.</param>
    /// <returns>El texto del mensaje de notificación correspondiente a la alerta activada.</returns>
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