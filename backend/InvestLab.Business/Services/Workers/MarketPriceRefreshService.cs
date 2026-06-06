using InvestLab.Business.Interfaces.Api;
using InvestLab.Business.Interfaces.Workers;
using InvestLab.Data.Interfaces;

namespace InvestLab.Business.Services.Workers
{
    public class MarketPriceRefreshService : IMarketPriceRefreshService
    {
        private readonly IAssetRepository _assetRepository;
        private readonly IMarketPriceCacheService _marketPriceCacheService;

        public MarketPriceRefreshService(IAssetRepository assetRepository, IMarketPriceCacheService marketPriceCacheService)
        {
            _assetRepository = assetRepository;
            _marketPriceCacheService = marketPriceCacheService;
        }

        public async Task RefreshAsync()
        {
            var assets = await _assetRepository.GetAllAsync();
            var symbols = assets.Where(x => !string.IsNullOrWhiteSpace(x.Symbol)).Select(x => x.Symbol.Trim().ToUpper()).Distinct().ToList();

            if (symbols.Any())
                await _marketPriceCacheService.RefreshPricesAsync(symbols);
        }


    }
}