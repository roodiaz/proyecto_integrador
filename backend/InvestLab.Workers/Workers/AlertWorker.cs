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

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AlertWorker"/>.
    /// </summary>
    /// <param name="serviceProvider">Proveedor de servicios utilizado para crear los scopes necesarios para resolver dependencias.</param>
    /// <param name="logger">Logger utilizado para registrar información y errores del worker.</param>
    public AlertWorker(IServiceProvider serviceProvider, ILogger<AlertWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta el ciclo principal del worker, procesando las alertas bursátiles
    /// de forma periódica cada 5 minutos hasta que se solicite la cancelación.
    /// En caso de error inesperado, espera 1 minuto antes de reintentar.
    /// </summary>
    /// <param name="stoppingToken">Token utilizado para señalar la cancelación de la ejecución del worker.</param>
    /// <returns>Una tarea que representa la ejecución asincrónica continua del worker.</returns>
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