using InvestLab.Business.Interfaces.Workers;

namespace InvestLab.Workers.Workers;

/// <summary>
/// Worker encargado de evaluar
/// alertas bursátiles.
/// </summary>
public class AlertWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public AlertWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();

            var service = scope.ServiceProvider.GetRequiredService<IAlertProcessingService>();

            await service.ProcessAlertsAsync();

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}