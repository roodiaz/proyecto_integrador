using InvestLab.Business.Interfaces;
using InvestLab.Business.Interfaces.Api;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models.DTOs.Market;
using Microsoft.Extensions.Caching.Memory;

namespace InvestLab.Business.Services.Api
{
    public class MarketPriceCacheService : IMarketPriceCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly IExternalProvider _externalProvider;
        private const int CacheExpirationMinutes = 5;
        private const string LastUpdatedAtCacheKey = "market-prices:last-updated-at";

        public MarketPriceCacheService(IMemoryCache cache, IExternalProvider marketPriceService)
        {
            _cache = cache;
            _externalProvider = marketPriceService;
        }

        public async Task<MarketPriceDto?> GetPriceAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return null;

            symbol = symbol.Trim().ToUpper();

            if (_cache.TryGetValue(GetCacheKey(symbol), out MarketPriceDto? cachedPrice) && cachedPrice != null)
                return cachedPrice;

            var freshPrice = await _externalProvider.GetPriceAsync(symbol);

            if (freshPrice == null)
                return null;

            freshPrice.Symbol = freshPrice.Symbol.Trim().ToUpper();
            freshPrice.UpdatedAt = DateTime.UtcNow;
            _cache.Set(GetCacheKey(freshPrice.Symbol), freshPrice, TimeSpan.FromMinutes(CacheExpirationMinutes));

            return freshPrice;
        }

        public async Task<MarketPricesResponseDto> GetPricesAsync(List<string> symbols)
        {
            if (symbols == null || !symbols.Any())
                return new MarketPricesResponseDto();

            var cleanSymbols = symbols.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToUpper()).Distinct().ToList();

            if (!cleanSymbols.Any())
                return new MarketPricesResponseDto();

            var prices = new List<MarketPriceDto>();
            var missingSymbols = new List<string>();

            foreach (var symbol in cleanSymbols)
            {
                if (_cache.TryGetValue(GetCacheKey(symbol), out MarketPriceDto? cachedPrice) && cachedPrice != null)
                    prices.Add(cachedPrice);
                else
                    missingSymbols.Add(symbol);
            }

            if (missingSymbols.Any())
            {
                var freshPrices = await _externalProvider.GetPricesAsync(missingSymbols);
                var updatedAt = DateTime.UtcNow;

                foreach (var price in freshPrices)
                {
                    price.Symbol = price.Symbol.Trim().ToUpper();
                    price.UpdatedAt = updatedAt;
                    _cache.Set(GetCacheKey(price.Symbol), price, TimeSpan.FromMinutes(CacheExpirationMinutes));
                    prices.Add(price);
                }
            }

            prices = prices.OrderBy(x => cleanSymbols.IndexOf(x.Symbol.Trim().ToUpper())).ToList();

            return new MarketPricesResponseDto
            {
                Prices = prices,
                UpdatedAt = prices.Any() ? prices.Min(x => x.UpdatedAt) : null
            };
        }

        public async Task RefreshPricesAsync(List<string> symbols)
        {
            if (symbols == null || !symbols.Any())
                return;

            var cleanSymbols = symbols.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToUpper()).Distinct().ToList();

            if (!cleanSymbols.Any())
                return;

            var freshPrices = await _externalProvider.GetPricesAsync(cleanSymbols);
            var updatedAt = DateTime.UtcNow;

            foreach (var price in freshPrices)
            {
                price.Symbol = price.Symbol.Trim().ToUpper();
                price.UpdatedAt = updatedAt;
                _cache.Set(GetCacheKey(price.Symbol), price, TimeSpan.FromMinutes(CacheExpirationMinutes));
            }

            _cache.Set(LastUpdatedAtCacheKey, updatedAt, TimeSpan.FromMinutes(CacheExpirationMinutes));
        }

        public Task<DateTime?> GetLastUpdatedAtAsync()
        {
            if (_cache.TryGetValue(LastUpdatedAtCacheKey, out DateTime updatedAt))
                return Task.FromResult<DateTime?>(updatedAt);

            return Task.FromResult<DateTime?>(null);
        }

        private static string GetCacheKey(string symbol)
        {
            return $"market-price:{symbol.Trim().ToUpper()}";
        }
    }
}