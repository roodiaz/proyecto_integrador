using System.Text.Json;
using InvestLab.Business.Interfaces;
using InvestLab.Business.Interfaces.Api;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models.DTOs.Market;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace InvestLab.Business.Services.Api
{
    public class MarketPriceCacheService : IMarketPriceCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly IMarketProviderResolver _providerResolver;
        private readonly IMarketStatusService _marketStatusService;
        private readonly ILogger<MarketPriceCacheService> _logger;
        private const int CacheExpirationMinutes = 5;
        private const int MarketClosedCacheExpirationHours = 24;
        private const string LastUpdatedAtCacheKey = "market-prices:last-updated-at";

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="MarketPriceCacheService"/> con la caché distribuida (Redis) y el proveedor externo de precios.
        /// </summary>
        /// <param name="cache">Caché distribuida utilizada para almacenar los precios de mercado.</param>
        /// <param name="providerResolver">Proveedor externo encargado de obtener los precios de mercado.</param>
        /// <param name="marketStatusService">Servicio que determina si el mercado está abierto o cerrado, usado para congelar la caché fuera de horario.</param>
        /// <param name="logger">Logger utilizado para registrar errores de comunicación con la caché.</param>
        public MarketPriceCacheService(IDistributedCache cache, IMarketProviderResolver providerResolver, IMarketStatusService marketStatusService, ILogger<MarketPriceCacheService> logger)
        {
            _cache = cache;
            _providerResolver = providerResolver;
            _marketStatusService = marketStatusService;
            _logger = logger;
        }

        /// <summary>
        /// Calcula el tiempo de expiración a aplicar a las entradas de la caché de precios: el TTL habitual mientras
        /// el mercado está abierto, o un período prolongado mientras está cerrado para "congelar" los precios y
        /// evitar consultas innecesarias al proveedor externo hasta la próxima apertura.
        /// </summary>
        /// <returns>El tiempo de expiración a utilizar en la caché distribuida.</returns>
        private TimeSpan GetCacheExpiration()
        {
            return _marketStatusService.IsMarketOpen()
                ? TimeSpan.FromMinutes(CacheExpirationMinutes)
                : TimeSpan.FromHours(MarketClosedCacheExpirationHours);
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

            var cachedPrice = await TryGetAsync<MarketPriceDto>(GetCacheKey(symbol));
            if (cachedPrice != null)
                return cachedPrice;

            var freshPrice = await _providerResolver.GetProvider().GetPriceAsync(symbol);

            if (freshPrice == null)
                return null;

            freshPrice.Symbol = freshPrice.Symbol.Trim().ToUpper();
            freshPrice.UpdatedAt = DateTime.UtcNow;
            await SetAsync(GetCacheKey(freshPrice.Symbol), freshPrice, GetCacheExpiration());

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
                var cachedPrice = await TryGetAsync<MarketPriceDto>(GetCacheKey(symbol));
                if (cachedPrice != null)
                    prices.Add(cachedPrice);
                else
                    missingSymbols.Add(symbol);
            }

            if (missingSymbols.Any())
            {
                var freshPrices = await _providerResolver.GetProvider().GetPricesAsync(missingSymbols);
                var updatedAt = DateTime.UtcNow;
                var expiration = GetCacheExpiration();

                foreach (var price in freshPrices)
                {
                    price.Symbol = price.Symbol.Trim().ToUpper();
                    price.UpdatedAt = updatedAt;
                    await SetAsync(GetCacheKey(price.Symbol), price, expiration);
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

            var freshPrices = await _providerResolver.GetProvider().GetPricesAsync(cleanSymbols);
            var updatedAt = DateTime.UtcNow;
            var expiration = GetCacheExpiration();

            foreach (var price in freshPrices)
            {
                price.Symbol = price.Symbol.Trim().ToUpper();
                price.UpdatedAt = updatedAt;
                await SetAsync(GetCacheKey(price.Symbol), price, expiration);
            }

            await SetAsync(LastUpdatedAtCacheKey, updatedAt, expiration);
        }

        /// <summary>
        /// Obtiene la fecha y hora de la última actualización de precios de mercado almacenada en la caché.
        /// </summary>
        /// <returns>La fecha y hora de la última actualización, o <c>null</c> si no hay información disponible en la caché.</returns>
        public async Task<DateTime?> GetLastUpdatedAtAsync()
        {
            return await TryGetAsync<DateTime?>(LastUpdatedAtCacheKey);
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

        /// <summary>
        /// Intenta leer y deserializar un valor de Redis. Si Redis no responde, registra el error y trata la
        /// operación como un cache-miss en lugar de propagar la excepción, para no afectar la disponibilidad de la API.
        /// </summary>
        private async Task<T?> TryGetAsync<T>(string key)
        {
            try
            {
                var raw = await _cache.GetStringAsync(key);
                return raw == null ? default : JsonSerializer.Deserialize<T>(raw);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al leer la clave '{Key}' desde Redis", key);
                return default;
            }
        }

        /// <summary>
        /// Serializa y escribe un valor en Redis con el TTL indicado. Si Redis no responde, registra el error
        /// sin propagar la excepción, para no afectar la disponibilidad de la API.
        /// </summary>
        private async Task SetAsync<T>(string key, T value, TimeSpan expiration)
        {
            try
            {
                var raw = JsonSerializer.Serialize(value);
                await _cache.SetStringAsync(key, raw, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al escribir la clave '{Key}' en Redis", key);
            }
        }
    }
}
