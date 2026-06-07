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

    /// <summary>
    /// Inicializa una nueva instancia del worker, recibiendo las dependencias necesarias
    /// para crear ámbitos de servicios y registrar información de diagnóstico.
    /// </summary>
    /// <param name="serviceProvider">Proveedor de servicios utilizado para crear ámbitos de inyección de dependencias en cada ejecución.</param>
    /// <param name="logger">Logger utilizado para registrar información, advertencias y errores del worker.</param>
    public MarketSeederWorker(IServiceProvider serviceProvider, ILogger<MarketSeederWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta el ciclo principal del worker en segundo plano: crea un ámbito de servicios,
    /// invoca la carga de históricos faltantes para los activos sin información histórica
    /// y espera un minuto antes de repetir el proceso, manejando cancelaciones y errores
    /// de forma controlada hasta que se solicite la detención del servicio.
    /// </summary>
    /// <param name="stoppingToken">Token de cancelación que indica cuándo debe detenerse la ejecución del worker.</param>
    /// <returns>Una tarea que representa la ejecución continua y asincrónica del worker.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker de carga inicial de históricos iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                var service = scope.ServiceProvider.GetRequiredService<IMarketHistoryService>();
                await service.SeedMissingHistoryAsync();

                _logger.LogDebug("Validación de históricos ejecutada");

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Worker de carga inicial de históricos cancelado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ejecutando carga de históricos");

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Worker de carga inicial de históricos cancelado durante la espera posterior al error");
                }
            }
        }
    }
}