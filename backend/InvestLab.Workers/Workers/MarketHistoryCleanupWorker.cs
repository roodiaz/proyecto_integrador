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

    /// <summary>
    /// Inicializa una nueva instancia del worker de limpieza de históricos, recibiendo
    /// las dependencias necesarias para crear ámbitos de servicios y registrar logs.
    /// </summary>
    /// <param name="serviceProvider">Proveedor de servicios utilizado para crear ámbitos de inyección de dependencias en cada ejecución.</param>
    /// <param name="logger">Logger utilizado para registrar información, advertencias y errores del worker.</param>
    public MarketHistoryCleanupWorker(IServiceProvider serviceProvider, ILogger<MarketHistoryCleanupWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta el ciclo principal del worker en segundo plano: calcula la próxima ejecución
    /// programada para la 01:00 UTC del día siguiente, espera hasta ese momento, crea un
    /// ámbito de servicios y ejecuta la limpieza de históricos viejos, manejando cancelaciones
    /// y errores de forma controlada hasta que se solicite la detención del servicio.
    /// </summary>
    /// <param name="stoppingToken">Token de cancelación que indica cuándo debe detenerse la ejecución del worker.</param>
    /// <returns>Una tarea que representa la ejecución continua y asincrónica del worker.</returns>
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