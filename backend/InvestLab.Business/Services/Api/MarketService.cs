using InvestLab.Business.Interfaces;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Market;
using Microsoft.Extensions.Logging;

namespace InvestLab.Business.Services
{
    public class MarketService : IMarketService
    {
        private readonly IExternalProvider _externalProvider;
        private readonly ILogger<MarketService> _logger;

        public MarketService(IExternalProvider externalProvider, ILogger<MarketService> logger)
        {
            _externalProvider = externalProvider;
            _logger = logger;
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