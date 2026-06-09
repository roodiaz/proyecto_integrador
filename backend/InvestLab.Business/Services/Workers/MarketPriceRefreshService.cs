using InvestLab.Business.Interfaces.Api;
using InvestLab.Business.Interfaces.Workers;
using InvestLab.Data.Interfaces;

namespace InvestLab.Business.Services.Workers
{
    public class MarketPriceRefreshService : IMarketPriceRefreshService
    {
        private readonly IAssetRepository _assetRepository;
        private readonly IMarketPriceCacheService _marketPriceCacheService;
        private readonly IMarketStatusService _marketStatusService;

        /// <summary>
        /// Inicializa una nueva instancia del servicio de actualización de precios de mercado.
        /// </summary>
        /// <param name="assetRepository">Repositorio utilizado para obtener los activos registrados.</param>
        /// <param name="marketPriceCacheService">Servicio de caché encargado de actualizar los precios de mercado.</param>
        /// <param name="marketStatusService">Servicio que determina si el mercado está abierto o cerrado.</param>
        public MarketPriceRefreshService(IAssetRepository assetRepository, IMarketPriceCacheService marketPriceCacheService, IMarketStatusService marketStatusService)
        {
            _assetRepository = assetRepository;
            _marketPriceCacheService = marketPriceCacheService;
            _marketStatusService = marketStatusService;
        }

        /// <summary>
        /// Obtiene todos los activos registrados, extrae sus símbolos válidos y únicos,
        /// y solicita al servicio de caché que actualice los precios de mercado correspondientes.
        /// Si el mercado está cerrado, no realiza ninguna consulta al proveedor externo y
        /// mantiene los precios cacheados congelados hasta la próxima apertura.
        /// </summary>
        /// <returns>Una tarea que representa la operación asincrónica de actualización de precios.</returns>
        public async Task RefreshAsync()
        {
            if (!_marketStatusService.IsMarketOpen())
                return;

            var assets = await _assetRepository.GetAllAsync();
            var symbols = assets.Where(x => !string.IsNullOrWhiteSpace(x.Symbol)).Select(x => x.Symbol.Trim().ToUpper()).Distinct().ToList();

            if (symbols.Any())
                await _marketPriceCacheService.RefreshPricesAsync(symbols);
        }
    }
}