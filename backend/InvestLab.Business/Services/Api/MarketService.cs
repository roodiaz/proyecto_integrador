using InvestLab.Business.Interfaces;
using InvestLab.Business.Interfaces.Api;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Market;
using InvestLab.Models.DTOs.Market;
using InvestLab.Models.DTOs.Market.InvestLab.Models.DTOs.Market;
using Microsoft.Extensions.Logging;
using static InvestLab.Models.MessageCodes;

namespace InvestLab.Business.Services
{
    public class MarketService : IMarketService
    {
        private readonly IMarketPriceCacheService _marketPriceCacheService;
        private readonly IMarketProviderResolver _providerResolver;
        private readonly IAssetService _assetService;
        private readonly IMarketStatusService _marketStatusService;
        private readonly ILogger<MarketService> _logger;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="MarketService"/> con el proveedor externo de datos de mercado, el logger, el servicio de activos y el servicio de caché de precios.
        /// </summary>
        /// <param name="externalProvider">Proveedor externo de información de mercado (precios, perfiles, históricos y noticias).</param>
        /// <param name="logger">Logger para registrar información y errores del servicio.</param>
        /// <param name="assetService">Servicio para obtener o crear activos.</param>
        /// <param name="marketPriceCacheService">Servicio de caché de precios de mercado.</param>
        /// <param name="marketStatusService">Servicio que determina si el mercado está abierto o cerrado.</param>
        public MarketService(IMarketProviderResolver providerResolver, ILogger<MarketService> logger, IAssetService assetService, IMarketPriceCacheService marketPriceCacheService, IMarketStatusService marketStatusService)
        {
            _providerResolver = providerResolver;
            _logger = logger;
            _assetService = assetService;
            _marketPriceCacheService = marketPriceCacheService;
            _marketStatusService = marketStatusService;
        }

        /// <summary>
        /// Obtiene el panorama general del mercado, incluyendo el estado del mercado y los valores actuales de los principales índices (S&amp;P 500, NASDAQ y Dow Jones).
        /// </summary>
        /// <returns>Una respuesta con el panorama de mercado, o un mensaje de error si no se pudieron obtener los índices o ocurre un fallo interno.</returns>
        public async Task<Response> GetMarketOverviewAsync()
        {
            try
            {
                var symbols = new List<string> { "^GSPC", "^IXIC", "^DJI", "^RUT" };
                var marketPricesResponse = symbols.Count == 0 ? new MarketPricesResponseDto() : await _marketPriceCacheService.GetPricesAsync(symbols);

                if (marketPricesResponse.Prices == null || marketPricesResponse.Prices.Count == 0)
                    return Response.Fail("No se pudieron obtener los índices del mercado", MARKET_INDICES_UNAVAILABLE);

                var indices = marketPricesResponse.Prices.Select(x => new MarketIndexDto
                {
                    Symbol = x.Symbol,
                    Name = GetIndexName(x.Symbol),
                    Value = x.Price,
                    PreviousClose = x.PreviousClose,
                    Change = Math.Round(x.Price - x.PreviousClose, 2),
                    ChangePercent = x.VariationPercent,
                    Trend = x.VariationPercent >= 0 ? "up" : "down"
                }).ToList();

                var overview = new MarketOverviewDto
                {
                    MarketStatus = _marketStatusService.GetStatus(),
                    Indices = indices
                };

                return Response.Ok(overview, "Panorama de mercado obtenido correctamente", MARKET_OVERVIEW_SUCCESS);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener panorama de mercado");
                return Response.Fail("Ocurrió un error al obtener el panorama de mercado", MARKET_OVERVIEW_ERROR);
            }
        }

        /// <summary>
        /// Obtiene el detalle completo de un activo a partir de su símbolo, combinando información de precio, perfil y otros indicadores de mercado.
        /// </summary>
        /// <param name="symbol">Símbolo del activo a consultar.</param>
        /// <returns>Una respuesta con el detalle del activo solicitado, o un mensaje de error si el símbolo es inválido, el activo no existe, no hay información de precio o ocurre un fallo interno.</returns>
        public async Task<Response> GetAssetDetailAsync(string symbol)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(symbol))
                    return Response.Fail("Debe ingresar un símbolo válido", INVALID_SYMBOL_FORMAT);

                symbol = symbol.Trim().ToUpper();

                var asset = await _assetService.GetOrCreateAsync(symbol);
                if (asset == null)
                    return Response.Fail("Activo no encontrado", ASSET_NOT_FOUND);

                var marketPricesResponse = await _marketPriceCacheService.GetPricesAsync(new List<string> { symbol });
                var price = marketPricesResponse.Prices.FirstOrDefault(x => x.Symbol == symbol);

                if (price == null)
                    return Response.Fail("No se encontró información de precio para el activo solicitado", PRICE_NOT_AVAILABLE);

                var profile = await _providerResolver.GetProvider().GetProfileAsync(symbol);
                var change = Math.Round(price.Price - price.PreviousClose, 2);

                var result = new MarketAssetDetailDto
                {
                    Symbol = profile?.Symbol ?? price.Symbol,
                    Name = profile?.Name ?? price.Symbol,
                    Exchange = string.Empty,
                    Price = price.Price,
                    Change = change,
                    ChangePercent = price.VariationPercent,
                    Open = price.Open,
                    Volume = price.Volume,
                    AvgVolume = price.AvgVolume,
                    DayHigh = price.DayHigh,
                    DayLow = price.DayLow,
                    MarketCap = price.MarketCap,
                    PeRatio = price.PeRatio,
                    DividendYield = price.DividendYield,
                    Sector = profile?.Sector ?? "Unknown"
                };

                return Response.Ok(result, "Activo obtenido correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalle del activo {Symbol}", symbol);
                return Response.Fail("Ocurrió un error al obtener el detalle del activo", INTERNAL_ERROR);
            }
        }

        /// <summary>
        /// Obtiene los activos en tendencia (más activos) del mercado.
        /// </summary>
        /// <returns>Una respuesta con la lista de activos en tendencia, o un mensaje de error si ocurre un fallo interno.</returns>
        public async Task<Response> GetTrendingAsync()
        {
            try
            {
                var result = await _providerResolver.GetProvider().GetMarketMoversAsync("most_actives", 6);
                return Response.Ok(result, "Tendencias obtenidas correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tendencias del mercado");
                return Response.Fail("Ocurrió un error al obtener las tendencias del mercado", INTERNAL_ERROR);
            }
        }

        /// <summary>
        /// Obtiene los activos con mayor ganancia del día en el mercado.
        /// </summary>
        /// <returns>Una respuesta con la lista de activos ganadores del día, o un mensaje de error si ocurre un fallo interno.</returns>
        public async Task<Response> GetGainersAsync()
        {
            try
            {
                var result = await _providerResolver.GetProvider().GetMarketMoversAsync("day_gainers", 6);
                return Response.Ok(result, "Ganadores obtenidos correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ganadores del mercado");
                return Response.Fail("Ocurrió un error al obtener los ganadores del mercado", INTERNAL_ERROR);
            }
        }

        /// <summary>
        /// Obtiene los activos con mayor pérdida del día en el mercado.
        /// </summary>
        /// <returns>Una respuesta con la lista de activos perdedores del día, o un mensaje de error si ocurre un fallo interno.</returns>
        public async Task<Response> GetLosersAsync()
        {
            try
            {
                var result = await _providerResolver.GetProvider().GetMarketMoversAsync("day_losers", 6);
                return Response.Ok(result, "Perdedores obtenidos correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener perdedores del mercado");
                return Response.Fail("Ocurrió un error al obtener los perdedores del mercado", INTERNAL_ERROR);
            }

        }

        /// <summary>
        /// Obtiene las noticias más recientes relacionadas con el mercado.
        /// </summary>
        /// <returns>Una respuesta con la lista de noticias del mercado, o un mensaje de error si ocurre un fallo interno.</returns>
        public async Task<Response> GetMarketNewsAsync()
        {
            try
            {
                var result = await _providerResolver.GetProvider().GetMarketNewsAsync(6);
                return Response.Ok(result, "Noticias obtenidas correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener noticias del mercado");
                return Response.Fail("Ocurrió un error al obtener las noticias del mercado", INTERNAL_ERROR);
            }
        }

        /// <summary>
        /// Obtiene el estado de actualización de la caché de precios de mercado, indicando la fecha y hora de la última actualización.
        /// </summary>
        /// <returns>Una respuesta con el estado de la caché de precios, o un mensaje de error si ocurre un fallo interno.</returns>
        public async Task<Response> GetMarketPriceStatusAsync()
        {
            try
            {
                var updatedAt = await _marketPriceCacheService.GetLastUpdatedAtAsync();

                return Response.Ok(new MarketPriceCacheStatusDto
                {
                    UpdatedAt = updatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estado de actualización de precios");
                return Response.Fail("Error interno", INTERNAL_ERROR);
            }
        }

        /// <summary>
        /// Obtiene el histórico de precios de un activo para un símbolo y rango de tiempo determinados.
        /// </summary>
        /// <param name="symbol">Símbolo del activo a consultar.</param>
        /// <param name="range">Rango de tiempo del histórico solicitado (por ejemplo "1d", "1w", "1m", "3m", "6m" o "1y").</param>
        /// <returns>Una respuesta con el histórico del activo solicitado, o un mensaje de error si el símbolo o el rango son inválidos, no se encuentran datos o ocurre un fallo interno.</returns>
        public async Task<Response> GetAssetHistoryAsync(string symbol, string range)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(symbol))
                    return Response.Fail("Debe ingresar un símbolo válido", INVALID_SYMBOL_FORMAT);

                symbol = symbol.Trim().ToUpper();
                range = NormalizeHistoryRange(range);

                if (!IsValidHistoryRange(range))
                    return Response.Fail("Rango inválido. Los valores permitidos son: 1d, 1w, 1m, 3m, 6m, 1y", INVALID_RANGE);

                var history = await _providerResolver.GetProvider().GetChartHistoryAsync(symbol, range);

                if (history == null || history.Count == 0)
                    return Response.Fail("No se encontraron datos históricos para el activo", ASSET_NOT_FOUND);

                var result = new MarketAssetHistoryDto
                {
                    Symbol = symbol,
                    Range = range,
                    Series = new MarketHistorySeriesDto
                    {
                        Symbol = symbol,
                        Name = symbol,
                        Points = history.Select(MapHistoryPoint).ToList()
                    }
                };

                return Response.Ok(result, "Histórico del activo obtenido correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener histórico del activo {Symbol} con rango {Range}", symbol, range);
                return Response.Fail("Ocurrió un error al obtener el histórico del activo", INTERNAL_ERROR);
            }
        }

        /// <summary>
        /// Obtiene el histórico de precios de los principales índices de mercado (S&amp;P 500, NASDAQ y Dow Jones) para un rango de tiempo determinado, a fin de compararlos.
        /// </summary>
        /// <param name="range">Rango de tiempo del histórico solicitado (por ejemplo "1d", "1w", "1m", "3m", "6m" o "1y").</param>
        /// <returns>Una respuesta con el histórico de comparación de los índices, o un mensaje de error si el rango es inválido o ocurre un fallo interno.</returns>
        public async Task<Response> GetComparisonHistoryAsync(string range)
        {
            try
            {
                range = NormalizeHistoryRange(range);

                if (!IsValidHistoryRange(range))
                    return Response.Fail("Rango inválido. Los valores permitidos son: 1d, 1w, 1m, 3m, 6m, 1y", INVALID_RANGE);

                var symbols = new List<string> { "^GSPC", "^IXIC", "^DJI", "^RUT" };
                var series = new List<MarketHistorySeriesDto>();

                foreach (var symbol in symbols)
                {
                    var history = await _providerResolver.GetProvider().GetChartHistoryAsync(symbol, range);

                    series.Add(new MarketHistorySeriesDto
                    {
                        Symbol = symbol,
                        Name = GetIndexName(symbol),
                        Points = history?.Select(MapHistoryPoint).ToList() ?? new List<MarketHistoryPointDto>()
                    });
                }

                var result = new MarketComparisonHistoryDto
                {
                    Range = range,
                    Series = series
                };

                return Response.Ok(result, "Histórico de comparación obtenido correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener histórico de comparación con rango {Range}", range);
                return Response.Fail("Ocurrió un error al obtener el histórico de comparación", INTERNAL_ERROR);
            }
        }


        // Metodos auxiliares
        /// <summary>
        /// Traduce el símbolo de un índice de mercado a su nombre descriptivo.
        /// </summary>
        /// <param name="symbol">Símbolo del índice de mercado.</param>
        /// <returns>El nombre descriptivo del índice (por ejemplo "S&amp;P 500", "NASDAQ" o "Dow Jones"), o el mismo símbolo si no se reconoce.</returns>
        private static string GetIndexName(string symbol)
        {
            return symbol switch
            {
                "^GSPC" => "S&P 500",
                "^IXIC" => "NASDAQ",
                "^DJI" => "Dow Jones",
                "^RUT" => "Russell 2000",
                _ => symbol
            };
        }

        /// <summary>
        /// Normaliza el rango de histórico recibido, asignando un valor por defecto cuando viene vacío y convirtiéndolo a minúsculas sin espacios.
        /// </summary>
        /// <param name="range">Rango de tiempo a normalizar.</param>
        /// <returns>El rango normalizado en minúsculas, o "1m" si el valor recibido es nulo o vacío.</returns>
        private static string NormalizeHistoryRange(string range)
        {
            return string.IsNullOrWhiteSpace(range) ? "1m" : range.Trim().ToLower();
        }

        /// <summary>
        /// Verifica si el rango de histórico recibido es uno de los valores permitidos.
        /// </summary>
        /// <param name="range">Rango de tiempo a validar.</param>
        /// <returns><c>true</c> si el rango es válido ("1d", "1w", "1m", "3m", "6m" o "1y"); en caso contrario, <c>false</c>.</returns>
        private static bool IsValidHistoryRange(string range)
        {
            return range is "1d" or "1w" or "1m" or "3m" or "6m" or "1y";
        }

        /// <summary>
        /// Calcula la fecha de inicio del histórico a partir de la fecha y hora actuales, según el rango de tiempo solicitado.
        /// </summary>
        /// <param name="range">Rango de tiempo del histórico (por ejemplo "1d", "1w", "1m", "3m", "6m" o "1y").</param>
        /// <returns>La fecha y hora de inicio correspondiente al rango indicado, calculada a partir de la fecha y hora actuales en UTC.</returns>
        private static DateTime GetHistoryFromDate(string range)
        {
            var now = DateTime.UtcNow;

            return range switch
            {
                "1d" => now.AddDays(-1),
                "1w" => now.AddDays(-7),
                "1m" => now.AddMonths(-1),
                "3m" => now.AddMonths(-3),
                "6m" => now.AddMonths(-6),
                "1y" => now.AddYears(-1),
                _ => now.AddMonths(-1)
            };
        }

        /// <summary>
        /// Convierte un punto de precio histórico proveniente del proveedor externo en un punto de histórico de mercado para su uso en la respuesta del servicio.
        /// </summary>
        /// <param name="item">Punto de precio histórico a convertir.</param>
        /// <returns>El punto de histórico de mercado equivalente, con la fecha, los valores de apertura, máximo, mínimo, cierre y volumen.</returns>
        private static MarketHistoryPointDto MapHistoryPoint(HistoricalPriceDto item)
        {
            return new MarketHistoryPointDto
            {
                Date = item.Date,
                Open = item.Open,
                High = item.High,
                Low = item.Low,
                Close = item.Close,
                Volume = item.Volume
            };
        }
    }
}