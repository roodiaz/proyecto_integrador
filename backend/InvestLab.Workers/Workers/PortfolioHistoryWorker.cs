using InvestLab.Business.Interfaces.Workers;

namespace InvestLab.Workers.Workers;

/// <summary>
/// Servicio en segundo plano que genera instantáneas diarias del historial de cartera a las 21:00 UTC.
/// </summary>
/// <remarks>Hereda de BackgroundService y respeta el CancellationToken para detenerse. En cada ejecución crea un
/// IServiceScope, resuelve IPortfolioHistoryWorkerService y llama a GenerateDailySnapshotsAsync. Comprueba el horario
/// cada 5 minutos y, tras ejecutar, espera 1 hora. Registra excepciones mediante ILogger.</remarks>
public class PortfolioHistoryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PortfolioHistoryWorker> _logger;

    public PortfolioHistoryWorker(IServiceScopeFactory scopeFactory, ILogger<PortfolioHistoryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                if (now.Hour == 21)
                {
                    using var scope = _scopeFactory.CreateScope();

                    var service = scope.ServiceProvider.GetRequiredService<IPortfolioHistoryWorkerService>();

                    await service.GenerateDailySnapshotsAsync();
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en PortfolioHistoryWorker");
            }
        }
    }
}