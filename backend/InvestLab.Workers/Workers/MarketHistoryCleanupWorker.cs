using InvestLab.Business.Interfaces.Workers;

namespace InvestLab.Workers.Workers;

/// <summary>
/// Worker encargado de limpiar históricos viejos de mercado.
/// Se ejecuta una vez por día a la 01:00 UTC.
/// </summary>
public class MarketHistoryCleanupWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MarketHistoryCleanupWorker> _logger;

    public MarketHistoryCleanupWorker(IServiceProvider serviceProvider, ILogger<MarketHistoryCleanupWorker> logger)
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
                var now = DateTime.UtcNow;
                var nextRun = now.Date.AddDays(1).AddHours(1);
                var delay = nextRun - now;

                _logger.LogInformation("Worker de limpieza de históricos programado para {NextRunUtc} UTC. Delay: {Delay}", nextRun, delay);

                await Task.Delay(delay, stoppingToken);

                using var scope = _serviceProvider.CreateScope();

                var service = scope.ServiceProvider.GetRequiredService<IMarketHistoryCleanupService>();
                await service.CleanupOldHistoryAsync();

                _logger.LogInformation("Limpieza de históricos viejos finalizada correctamente");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Worker de limpieza de históricos cancelado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en el worker de limpieza de históricos");

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Worker de limpieza de históricos cancelado durante la espera posterior al error");
                }
            }
        }
    }
}