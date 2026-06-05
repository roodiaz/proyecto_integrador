using InvestLab.Integrations.Configuration;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models.DTOs;
using InvestLab.Models.DTOs.Market;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NodaTime;
using YahooQuotesApi;

namespace InvestLab.Integrations.Providers
{
    public class YahooMarketProvider : IExternalProvider
    {
        private readonly YahooOptions _options;
        private readonly IConfiguration _config;
        private readonly YahooQuotes _yahooQuotes;

        public YahooMarketProvider(HttpClient httpClient, IConfiguration config, IOptions<YahooOptions> options)
        {
            _config = config;
            _options = options.Value;
            _yahooQuotes = new YahooQuotesBuilder().Build();
        }

        public async Task<MarketPriceDto?> GetPriceAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return null;

            symbol = symbol.Trim().ToUpper();

            var snapshot = await _yahooQuotes.GetSnapshotAsync(symbol);

            if (snapshot == null)
                return null;

            var price = GetDecimalValue(snapshot, "RegularMarketPrice", "PostMarketPrice", "PreMarketPrice") ?? 0;
            var previousClose = GetDecimalValue(snapshot, "RegularMarketPreviousClose", "PreviousClose", "RegularMarketPreviousCloseRaw") ?? 0;
            var changePercent = GetDecimalValue(snapshot, "RegularMarketChangePercent", "ChangePercent", "RegularMarketChangePercentRaw");

            if (changePercent == null && previousClose > 0)
                changePercent = ((price - previousClose) / previousClose) * 100;

            return new MarketPriceDto
            {
                Symbol = GetStringValue(snapshot, "Symbol") ?? symbol,
                Price = price,
                PreviousClose = previousClose,
                VariationPercent = Math.Round(changePercent ?? 0, 2),
                Open = GetDecimalValue(snapshot, "RegularMarketOpen", "Open"),
                Volume = GetLongValue(snapshot, "RegularMarketVolume", "Volume"),
                AvgVolume = GetLongValue(snapshot, "AverageDailyVolume3Month", "AverageDailyVolume10Day", "AverageVolume", "AverageVolume3Months"),
                DayHigh = GetDecimalValue(snapshot, "RegularMarketDayHigh", "DayHigh"),
                DayLow = GetDecimalValue(snapshot, "RegularMarketDayLow", "DayLow"),
                MarketCap = GetLongValue(snapshot, "MarketCap", "MarketCapitalization"),
                PeRatio = GetDecimalValue(snapshot, "TrailingPE", "PeRatio", "PERatio", "TrailingPe"),
                DividendYield = GetDecimalValue(snapshot, "DividendYield", "TrailingAnnualDividendYield")
            };
        }

        public async Task<List<MarketPriceDto>> GetPricesAsync(List<string> symbols)
        {
            if (symbols == null || !symbols.Any())
                return [];

            var cleanSymbols = symbols.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToUpper()).Distinct().ToArray();

            if (cleanSymbols.Length == 0)
                return [];

            var snapshots = await _yahooQuotes.GetSnapshotAsync(cleanSymbols);
            var prices = new List<MarketPriceDto>();

            foreach (var symbol in cleanSymbols)
            {
                if (!snapshots.TryGetValue(symbol, out var snapshot) || snapshot == null)
                    continue;

                var price = GetDecimalValue(snapshot, "RegularMarketPrice", "PostMarketPrice", "PreMarketPrice") ?? 0;
                var previousClose = GetDecimalValue(snapshot, "RegularMarketPreviousClose", "PreviousClose", "RegularMarketPreviousCloseRaw") ?? 0;
                var changePercent = GetDecimalValue(snapshot, "RegularMarketChangePercent", "ChangePercent", "RegularMarketChangePercentRaw");

                if (changePercent == null && previousClose > 0)
                    changePercent = ((price - previousClose) / previousClose) * 100;

                prices.Add(new MarketPriceDto
                {
                    Symbol = GetStringValue(snapshot, "Symbol") ?? symbol,
                    Price = price,
                    PreviousClose = previousClose,
                    VariationPercent = Math.Round(changePercent ?? 0, 2),
                    Open = GetDecimalValue(snapshot, "RegularMarketOpen", "Open"),
                    Volume = GetLongValue(snapshot, "RegularMarketVolume", "Volume"),
                    AvgVolume = GetLongValue(snapshot, "AverageDailyVolume3Month", "AverageDailyVolume10Day", "AverageVolume", "AverageVolume3Months"),
                    DayHigh = GetDecimalValue(snapshot, "RegularMarketDayHigh", "DayHigh"),
                    DayLow = GetDecimalValue(snapshot, "RegularMarketDayLow", "DayLow"),
                    MarketCap = GetLongValue(snapshot, "MarketCap", "MarketCapitalization"),
                    PeRatio = GetDecimalValue(snapshot, "TrailingPE", "PeRatio", "PERatio", "TrailingPe"),
                    DividendYield = GetDecimalValue(snapshot, "DividendYield", "TrailingAnnualDividendYield")
                });
            }

            return prices;
        }

        public async Task<List<HistoricalPriceDto>> GetHistoricalAsync(string symbol, DateTime from, DateTime to)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return [];

            symbol = symbol.Trim().ToUpper();

            var start = Instant.FromDateTimeUtc(DateTime.SpecifyKind(from.Date, DateTimeKind.Utc));
            var yahooQuotes = new YahooQuotesBuilder().WithHistoryStartDate(start).Build();

            var result = await yahooQuotes.GetHistoryAsync(symbol);

            if (result == null || result.Value == null)
                return [];

            var history = result.Value;
            var ticks = GetObjectValue(history, "Ticks");

            if (ticks == null)
                return [];

            var list = new List<HistoricalPriceDto>();

            foreach (var tick in (System.Collections.IEnumerable)ticks)
            {
                var date = GetDateTimeFromTick(tick);
                if (date == null)
                    continue;

                if (date.Value.Date < from.Date || date.Value.Date > to.Date)
                    continue;

                var close = GetDecimalValue(tick, "Close");
                if (close == null)
                    continue;

                list.Add(new HistoricalPriceDto
                {
                    Date = date.Value,
                    Open = GetDecimalValue(tick, "Open") ?? 0,
                    High = GetDecimalValue(tick, "High") ?? 0,
                    Low = GetDecimalValue(tick, "Low") ?? 0,
                    Close = close.Value,
                    Volume = GetLongValue(tick, "Volume") ?? 0
                });
            }

            return list.OrderBy(x => x.Date).ToList();
        }

        public async Task<AssetProfileDto?> GetProfileAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return null;

            symbol = symbol.Trim().ToUpper();

            var snapshot = await _yahooQuotes.GetSnapshotAsync(symbol);

            if (snapshot == null)
                return null;

            return new AssetProfileDto
            {
                Symbol = GetStringValue(snapshot, "Symbol") ?? symbol,
                Name = GetStringValue(snapshot, "LongName", "ShortName", "DisplayName") ?? symbol,
                Sector = GetStringValue(snapshot, "Sector", "SectorDisp", "Industry") ?? "Unknown"
            };
        }

        private static object? GetObjectValue(object source, params string[] propertyNames)
        {
            var type = source.GetType();

            foreach (var propertyName in propertyNames)
            {
                var prop = type.GetProperty(propertyName);
                if (prop == null)
                    continue;

                var value = prop.GetValue(source);
                if (value != null)
                    return value;
            }

            return null;
        }

        private static string? GetStringValue(object source, params string[] propertyNames)
        {
            var value = GetObjectValue(source, propertyNames);
            return value?.ToString();
        }

        private static decimal? GetDecimalValue(object source, params string[] propertyNames)
        {
            var value = GetObjectValue(source, propertyNames);

            if (value == null)
                return null;

            try
            {
                return value switch
                {
                    decimal d => d,
                    double d => Convert.ToDecimal(d),
                    float f => Convert.ToDecimal(f),
                    int i => i,
                    long l => l,
                    short s => s,
                    _ => decimal.TryParse(value.ToString(), out var parsed) ? parsed : null
                };
            }
            catch
            {
                return null;
            }
        }

        private static long? GetLongValue(object source, params string[] propertyNames)
        {
            var value = GetObjectValue(source, propertyNames);

            if (value == null)
                return null;

            try
            {
                return value switch
                {
                    long l => l,
                    int i => i,
                    short s => s,
                    decimal d => Convert.ToInt64(d),
                    double d => Convert.ToInt64(d),
                    float f => Convert.ToInt64(f),
                    _ => long.TryParse(value.ToString(), out var parsed) ? parsed : null
                };
            }
            catch
            {
                return null;
            }
        }

        private static DateTime? GetDateTimeFromTick(object tick)
        {
            var value = GetObjectValue(tick, "Date");

            if (value == null)
                return null;

            if (value is Instant instant)
                return instant.ToDateTimeUtc();

            if (value is DateTime dateTime)
                return dateTime;

            return null;
        }
    }
}