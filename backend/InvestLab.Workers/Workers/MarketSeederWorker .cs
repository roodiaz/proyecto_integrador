using InvestLab.Business.Interfaces.Workers;

namespace InvestLab.Workers;

/// <summary>
/// Worker encargado de monitorear continuamente
/// los activos registrados en el sistema y cargar
/// el histórico inicial de aquellos que aún no
/// poseen información histórica en MongoDB.
/// </summary>
/// <remarks>
/// Este proceso se ejecuta periódicamente mientras
/// la aplicación está en funcionamiento.
///
/// Su responsabilidad es detectar activos con
/// HistoryLoaded = false, obtener el último año
/// de cotizaciones desde el proveedor externo y
/// almacenarlas en MongoDB.
///
/// Una vez finalizada la carga, el activo es
/// marcado como inicializado para evitar futuras
/// recargas.
/// </remarks>
public class MarketSeederWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MarketSeederWorker> _logger;

    public MarketSeederWorker(IServiceProvider serviceProvider, ILogger<MarketSeederWorker> logger)
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

                var service = scope.ServiceProvider.GetRequiredService<IMarketHistoryService>();

                await service.SeedMissingHistoryAsync();

                _logger.LogInformation("Validación de históricos ejecutada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ejecutando carga de históricos");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}