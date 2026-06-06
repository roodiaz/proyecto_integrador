using InvestLab.Business.Interfaces.Workers;

namespace InvestLab.Workers.Workers;

/// <summary>
/// Worker en segundo plano que reinicia los límites diarios de todos los usuarios una vez al día.
/// 
/// Este servicio:
/// - Calcula el tiempo restante hasta la próxima medianoche UTC y espera respetando el token de cancelación.
/// - Crea un alcance de DI por ejecución para resolver `IUserDailyLimitsResetService`.
/// - Llama a `ResetDailyLimitsAsync` dentro del alcance para realizar el reinicio de límites.
/// </summary>
/// <remarks>
/// Diseñado para ejecutarse como un servicio de tipo Worker en .NET (hereda de <see cref="BackgroundService"/>).
/// Uso de DI: se recibe un <see cref="IServiceProvider"/> y se crean alcances temporales con <see cref="IServiceScope"/>.
/// </remarks>
public class UserDailyLimitsResetWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserDailyLimitsResetWorker> _logger;

    public UserDailyLimitsResetWorker(IServiceProvider serviceProvider, ILogger<UserDailyLimitsResetWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker de reinicio de límites diarios iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextRun = now.Date.AddDays(1);
                var delay = nextRun - now;

                _logger.LogInformation("Reinicio de límites diarios programado para {NextRunUtc} UTC. Delay: {Delay}", nextRun, delay);

                await Task.Delay(delay, stoppingToken);

                using var scope = _serviceProvider.CreateScope();

                var service = scope.ServiceProvider.GetRequiredService<IUserDailyLimitsResetService>();
                await service.ResetDailyLimitsAsync();

                _logger.LogInformation("Límites diarios reiniciados correctamente");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Worker de reinicio de límites diarios cancelado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en el worker de reinicio de límites diarios");

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Worker de reinicio de límites diarios cancelado durante la espera posterior al error");
                }
            }
        }
    }
}