using InvestLab.Business.Interfaces.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InvestLab.Workers
{
    public class MarketPriceRefreshWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MarketPriceRefreshWorker> _logger;

        public MarketPriceRefreshWorker(IServiceScopeFactory scopeFactory, ILogger<MarketPriceRefreshWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker de actualización de precios de mercado iniciado");

            try
            {
                await RefreshAsync();

                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

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