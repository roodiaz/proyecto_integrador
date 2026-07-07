using InvestLab.Business.Interfaces.Workers;
using InvestLab.Models.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvestLab.Business.Workers
{
    public class MarketPriceRefreshWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MarketPriceRefreshWorker> _logger;
        private readonly MarketPriceRefreshOptions _options;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="MarketPriceRefreshWorker"/>.
        /// </summary>
        /// <param name="scopeFactory">Fábrica utilizada para crear los scopes necesarios para resolver dependencias.</param>
        /// <param name="logger">Logger utilizado para registrar información y errores del worker.</param>
        /// <param name="options">Opciones de configuración del worker, incluyendo el intervalo de refresco.</param>
        public MarketPriceRefreshWorker(IServiceScopeFactory scopeFactory, ILogger<MarketPriceRefreshWorker> logger, IOptions<MarketPriceRefreshOptions> options)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        /// <summary>
        /// Ejecuta el ciclo principal del worker, realizando una actualización inicial
        /// del cache de precios de mercado y luego repitiendo la actualización cada
        /// N segundos mediante un temporizador periódico, hasta que se solicite la cancelación.
        /// </summary>
        /// <param name="stoppingToken">Token utilizado para señalar la cancelación de la ejecución del worker.</param>
        /// <returns>Una tarea que representa la ejecución asincrónica continua del worker.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker de actualización de precios de mercado iniciado");

            try
            {
                await RefreshAsync();

                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));

                while (await timer.WaitForNextTickAsync(stoppingToken))
                    await RefreshAsync();
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Worker de actualización de precios de mercado cancelado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en el worker de actualización de precios de mercado");
            }
        }

        /// <summary>
        /// Crea un scope de servicios y solicita la actualización del cache de precios
        /// de mercado a través del servicio correspondiente, registrando el resultado
        /// o el error producido durante la operación.
        /// </summary>
        /// <returns>Una tarea que representa la operación asincrónica de actualización del cache de precios.</returns>
        private async Task RefreshAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var refreshService = scope.ServiceProvider.GetRequiredService<IMarketPriceRefreshService>();
                await refreshService.RefreshAsync();

                _logger.LogInformation("Cache de precios de mercado actualizado correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el cache de precios de mercado");
            }
        }
    }
}
