using DnsClient.Internal;
using InvestLab.Business.Interfaces.Workers;

namespace InvestLab.Workers.Workers;

/// <summary>
/// Worker encargado de evaluar
/// alertas bursátiles.
/// </summary>
public class AlertWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AlertWorker> _logger;

    public AlertWorker(IServiceProvider serviceProvider, ILogger<AlertWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                var service = scope.ServiceProvider.GetRequiredService<IAlertProcessingService>();

                await service.ProcessAlertsAsync();

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Worker de procesamiento de alertas cancelado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en el worker de procesamiento de alertas");

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}