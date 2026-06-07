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

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="MarketPriceCacheService"/> con la caché en memoria y el proveedor externo de precios.
        /// </summary>
        /// <param name="cache">Caché en memoria utilizada para almacenar los precios de mercado.</param>
        /// <param name="marketPriceService">Proveedor externo encargado de obtener los precios de mercado.</param>
        public MarketPriceCacheService(IMemoryCache cache, IExternalProvider marketPriceService)
        {
            _cache = cache;
            _externalProvider = marketPriceService;
        }

        /// <summary>
        /// Obtiene el precio de mercado de un símbolo, devolviendo el valor cacheado si está disponible o consultando al proveedor externo y almacenándolo en caché en caso contrario.
        /// </summary>
        /// <param name="symbol">Símbolo del activo a consultar.</param>
        /// <returns>El precio de mercado del símbolo, o <c>null</c> si el símbolo es inválido o no se pudo obtener información.</returns>
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

        /// <summary>
        /// Obtiene los precios de mercado de una lista de símbolos, utilizando los valores cacheados cuando existen y consultando al proveedor externo solo para los símbolos faltantes, actualizando luego la caché.
        /// </summary>
        /// <param name="symbols">Lista de símbolos de activos a consultar.</param>
        /// <returns>Una respuesta con los precios de mercado encontrados y la fecha de la actualización más antigua entre ellos.</returns>
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

        /// <summary>
        /// Refresca en la caché los precios de mercado de los símbolos indicados consultando directamente al proveedor externo, sin importar si ya existían valores cacheados, y actualiza la marca de tiempo de la última actualización.
        /// </summary>
        /// <param name="symbols">Lista de símbolos de activos cuyos precios deben refrescarse.</param>
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

        /// <summary>
        /// Obtiene la fecha y hora de la última actualización de precios de mercado almacenada en la caché.
        /// </summary>
        /// <returns>La fecha y hora de la última actualización, o <c>null</c> si no hay información disponible en la caché.</returns>
        public Task<DateTime?> GetLastUpdatedAtAsync()
        {
            if (_cache.TryGetValue(LastUpdatedAtCacheKey, out DateTime updatedAt))
                return Task.FromResult<DateTime?>(updatedAt);

            return Task.FromResult<DateTime?>(null);
        }

        /// <summary>
        /// Genera la clave de caché correspondiente a un símbolo de activo, normalizándolo a mayúsculas y sin espacios.
        /// </summary>
        /// <param name="symbol">Símbolo del activo.</param>
        /// <returns>La clave de caché generada para el símbolo indicado.</returns>
        private static string GetCacheKey(string symbol)
        {
            return $"market-price:{symbol.Trim().ToUpper()}";
        }
    }
}