using InvestLab.Business.Interfaces;
using InvestLab.Business.Interfaces.Api;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Market;
using InvestLab.Models.DTOs.Market;
using InvestLab.Models.DTOs.Market.InvestLab.Models.DTOs.Market;
using Microsoft.Extensions.Logging;

namespace InvestLab.Business.Services
{
    public class MarketService : IMarketService
    {
        private readonly IMarketPriceCacheService _marketPriceCacheService;
        private readonly IExternalProvider _externalProvider;
        private readonly IAssetService _assetService;
        private readonly ILogger<MarketService> _logger;

        public MarketService(IExternalProvider externalProvider, ILogger<MarketService> logger, IAssetService assetService, IMarketPriceCacheService marketPriceCacheService)
        {
            _externalProvider = externalProvider;
            _logger = logger;
            _assetService = assetService;
            _marketPriceCacheService = marketPriceCacheService;
        }

        public async Task<Response> GetMarketOverviewAsync()
        {
            try
            {
                var symbols = new List<string> { "^GSPC", "^IXIC", "^DJI" };
                var marketPricesResponse = symbols.Count == 0 ? new MarketPricesResponseDto() : await _marketPriceCacheService.GetPricesAsync(symbols);

                if (marketPricesResponse.Prices == null || marketPricesResponse.Prices.Count == 0)
                    return Response.Fail("No se pudieron obtener los índices del mercado");

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
                    MarketStatus = GetMarketStatus(),
                    Indices = indices
                };

                return Response.Ok(overview, "Panorama de mercado obtenido correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener panorama de mercado");
                return Response.Fail("Ocurrió un error al obtener el panorama de mercado");
            }
        }

        public async Task<Response> GetAssetDetailAsync(string symbol)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(symbol))
                    return Response.Fail("Debe ingresar un símbolo válido");

                symbol = symbol.Trim().ToUpper();

                var asset = await _assetService.GetOrCreateAsync(symbol);
                if (asset == null)
                    return Response.Fail("Activo no encontrado");

                var marketPricesResponse = await _marketPriceCacheService.GetPricesAsync(new List<string> { symbol });
                var price = marketPricesResponse.Prices.FirstOrDefault(x => x.Symbol == symbol);

                if (price == null)
                    return Response.Fail("No se encontró información de precio para el activo solicitado");

                var profile = await _externalProvider.GetProfileAsync(symbol);
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
                return Response.Fail("Ocurrió un error al obtener el detalle del activo");
            }
        }

        public async Task<Response> GetTrendingAsync()
        {
            try
            {
                var result = await _externalProvider.GetMarketMoversAsync("most_actives", 6);
                return Response.Ok(result, "Tendencias obtenidas correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tendencias del mercado");
                return Response.Fail("Ocurrió un error al obtener las tendencias del mercado");
            }
        }

        public async Task<Response> GetGainersAsync()
        {
            try
            {
                var result = await _externalProvider.GetMarketMoversAsync("day_gainers", 6);
                return Response.Ok(result, "Ganadores obtenidos correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ganadores del mercado");
                return Response.Fail("Ocurrió un error al obtener los ganadores del mercado");
            }
        }

        public async Task<Response> GetLosersAsync()
        {
            try
            {
                var result = await _externalProvider.GetMarketMoversAsync("day_losers", 6);
                return Response.Ok(result, "Perdedores obtenidos correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener perdedores del mercado");
                return Response.Fail("Ocurrió un error al obtener los perdedores del mercado");
            }

        }

        public async Task<Response> GetMarketNewsAsync()
        {
            try
            {
                var result = await _externalProvider.GetMarketNewsAsync(6);
                return Response.Ok(result, "Noticias obtenidas correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener noticias del mercado");
                return Response.Fail("Ocurrió un error al obtener las noticias del mercado");
            }
        }

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
                return Response.Fail("Error interno");
            }
        }

        public async Task<Response> GetAssetHistoryAsync(string symbol, string range)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(symbol))
                    return Response.Fail("Debe ingresar un símbolo válido");

                symbol = symbol.Trim().ToUpper();
                range = NormalizeHistoryRange(range);

                if (!IsValidHistoryRange(range))
                    return Response.Fail("Rango inválido. Los valores permitidos son: 1d, 1w, 1m, 3m, 6m, 1y");

                var history = await _externalProvider.GetChartHistoryAsync(symbol, range);

                if (history == null || history.Count == 0)
                    return Response.Fail("No se encontraron datos históricos para el activo");

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
                return Response.Fail("Ocurrió un error al obtener el histórico del activo");
            }
        }

        public async Task<Response> GetComparisonHistoryAsync(string range)
        {
            try
            {
                range = NormalizeHistoryRange(range);

                if (!IsValidHistoryRange(range))
                    return Response.Fail("Rango inválido. Los valores permitidos son: 1d, 1w, 1m, 3m, 6m, 1y");

                var symbols = new List<string> { "^GSPC", "^IXIC", "^DJI" };
                var series = new List<MarketHistorySeriesDto>();

                foreach (var symbol in symbols)
                {
                    var history = await _externalProvider.GetChartHistoryAsync(symbol, range);

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
                return Response.Fail("Ocurrió un error al obtener el histórico de comparación");
            }
        }


        // Metodos auxiliares
        private static MarketStatusDto GetMarketStatus()
        {
            var timeZone = GetEasternTimeZone();
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            var open = new TimeSpan(9, 30, 0);
            var close = new TimeSpan(16, 0, 0);
            var isBusinessDay = now.DayOfWeek != DayOfWeek.Saturday && now.DayOfWeek != DayOfWeek.Sunday;
            var isOpen = isBusinessDay && now.TimeOfDay >= open && now.TimeOfDay <= close;

            return new MarketStatusDto
            {
                IsOpen = isOpen,
                StatusText = isOpen ? "Mercado abierto" : "Mercado cerrado",
                MarketTime = now.ToString("HH:mm:ss"),
                TimeZone = "America/New_York",
                OpenTime = "09:30",
                CloseTime = "16:00"
            };
        }

        private static TimeZoneInfo GetEasternTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
            }
            catch
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            }
        }

        private static string GetIndexName(string symbol)
        {
            return symbol switch
            {
                "^GSPC" => "S&P 500",
                "^IXIC" => "NASDAQ",
                "^DJI" => "Dow Jones",
                _ => symbol
            };
        }

        private static string NormalizeHistoryRange(string range)
        {
            return string.IsNullOrWhiteSpace(range) ? "1m" : range.Trim().ToLower();
        }

        private static bool IsValidHistoryRange(string range)
        {
            return range is "1d" or "1w" or "1m" or "3m" or "6m" or "1y";
        }

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