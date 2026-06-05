using InvestLab.Business.Interfaces;
using InvestLab.Business.Interfaces.Api;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Market;
using Microsoft.Extensions.Logging;

namespace InvestLab.Business.Services
{
    public class MarketService : IMarketService
    {
        private readonly IExternalProvider _externalProvider;
        private readonly IAssetService _assetService;
        private readonly ILogger<MarketService> _logger;

        public MarketService(IExternalProvider externalProvider, ILogger<MarketService> logger, IAssetService assetService)
        {
            _externalProvider = externalProvider;
            _logger = logger;
            _assetService = assetService;
        }

        public async Task<Response> GetMarketOverviewAsync()
        {
            try
            {
                var symbols = new List<string> { "^GSPC", "^IXIC", "^DJI" };
                var prices = await _externalProvider.GetPricesAsync(symbols);

                if (prices == null || prices.Count == 0)
                    return Response.Fail("No se pudieron obtener los índices del mercado");

                var indices = prices.Select(x => new MarketIndexDto
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

                var price = await _externalProvider.GetPriceAsync(symbol);
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
    }
}