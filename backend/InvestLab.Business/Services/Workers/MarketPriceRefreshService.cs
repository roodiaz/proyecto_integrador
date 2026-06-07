using InvestLab.Business.Interfaces.Api;
using InvestLab.Business.Interfaces.Workers;
using InvestLab.Data.Interfaces;

namespace InvestLab.Business.Services.Workers
{
    public class MarketPriceRefreshService : IMarketPriceRefreshService
    {
        private readonly IAssetRepository _assetRepository;
        private readonly IMarketPriceCacheService _marketPriceCacheService;

        /// <summary>
        /// Inicializa una nueva instancia del servicio de actualización de precios de mercado.
        /// </summary>
        /// <param name="assetRepository">Repositorio utilizado para obtener los activos registrados.</param>
        /// <param name="marketPriceCacheService">Servicio de caché encargado de actualizar los precios de mercado.</param>
        public MarketPriceRefreshService(IAssetRepository assetRepository, IMarketPriceCacheService marketPriceCacheService)
        {
            _assetRepository = assetRepository;
            _marketPriceCacheService = marketPriceCacheService;
        }

        /// <summary>
        /// Obtiene todos los activos registrados, extrae sus símbolos válidos y únicos,
        /// y solicita al servicio de caché que actualice los precios de mercado correspondientes.
        /// </summary>
        /// <returns>Una tarea que representa la operación asincrónica de actualización de precios.</returns>
        public async Task RefreshAsync()
        {
            var assets = await _assetRepository.GetAllAsync();
            var symbols = assets.Where(x => !string.IsNullOrWhiteSpace(x.Symbol)).Select(x => x.Symbol.Trim().ToUpper()).Distinct().ToList();

            if (symbols.Any())
                await _marketPriceCacheService.RefreshPricesAsync(symbols);
        }


    }
}