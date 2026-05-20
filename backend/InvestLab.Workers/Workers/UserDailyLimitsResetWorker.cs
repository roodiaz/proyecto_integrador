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

    public UserDailyLimitsResetWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1);
            var delay = nextRun - now;

            await Task.Delay(delay, stoppingToken);

            using var scope = _serviceProvider.CreateScope();

            var service = scope.ServiceProvider.GetRequiredService<IUserDailyLimitsResetService>();
            await service.ResetDailyLimitsAsync();
        }
    }
}