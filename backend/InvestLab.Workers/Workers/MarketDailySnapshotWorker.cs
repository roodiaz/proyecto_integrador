using InvestLab.Business.Interfaces.Workers;

namespace InvestLab.Workers.Workers;

/// <summary>
/// Worker encargado de almacenar
/// cierres diarios de mercado.
/// </summary>
public class MarketDailySnapshotWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public MarketDailySnapshotWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddHours(21).AddMinutes(30);

            if (now > nextRun)
                nextRun = nextRun.AddDays(1);

            var delay = nextRun - now;

            await Task.Delay(delay, stoppingToken);

            using var scope = _serviceProvider.CreateScope();

            var service = scope.ServiceProvider.GetRequiredService<IMarketHistoryService>();

            await service.SaveDailyMarketHistoryAsync();
        }
    }
}