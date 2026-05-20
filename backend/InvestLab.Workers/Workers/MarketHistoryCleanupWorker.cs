using InvestLab.Business.Interfaces.Workers;

namespace InvestLab.Workers.Workers;

/// <summary>
/// Worker encargado de limpiar
/// históricos viejos de mercado.
/// </summary>
public class MarketHistoryCleanupWorker: BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public MarketHistoryCleanupWorker( IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1).AddHours(1);
            var delay = nextRun - now;

            await Task.Delay( delay,  stoppingToken);

            using var scope = _serviceProvider.CreateScope();

            var service = scope.ServiceProvider .GetRequiredService <IMarketHistoryCleanupService>();
            await service .CleanupOldHistoryAsync();
        }
    }
}