using InvestLab.Integrations.Configuration;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models.DTOs;
using InvestLab.Models.DTOs.Market;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NodaTime;
using System.Text.Json;
using YahooQuotesApi;

namespace InvestLab.Integrations.Providers
{
    public class YahooMarketProvider : IExternalProvider
    {
        private readonly YahooOptions _options;
        private readonly IConfiguration _config;
        private readonly YahooQuotes _yahooQuotes;
        private readonly HttpClient _httpClient;

        public YahooMarketProvider(HttpClient httpClient, IConfiguration config, IOptions<YahooOptions> options)
        {
            _config = config;
            _options = options.Value;
            _yahooQuotes = new YahooQuotesBuilder().Build();
            _httpClient = httpClient;
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

        public async Task<List<MarketMoverDto>> GetMarketMoversAsync(string screenerId, int count)
        {
            if (string.IsNullOrWhiteSpace(screenerId))
                return [];

            count = count <= 0 ? 6 : count;

            var url = $"https://query1.finance.yahoo.com/v1/finance/screener/predefined/saved?scrIds={Uri.EscapeDataString(screenerId)}&count={count}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return [];

            var json = await response.Content.ReadAsStringAsync();

            using var data = JsonDocument.Parse(json);

            if (!data.RootElement.TryGetProperty("finance", out var finance))
                return [];

            if (!finance.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Array || result.GetArrayLength() == 0)
                return [];

            var firstResult = result[0];

            if (!firstResult.TryGetProperty("quotes", out var quotes) || quotes.ValueKind != JsonValueKind.Array)
                return [];

            var movers = new List<MarketMoverDto>();

            foreach (var item in quotes.EnumerateArray())
            {
                var symbol = GetJsonString(item, "symbol");

                if (string.IsNullOrWhiteSpace(symbol))
                    continue;

                var price = GetJsonDecimal(item, "regularMarketPrice") ?? 0;
                var change = GetJsonDecimal(item, "regularMarketChange") ?? 0;
                var changePercent = GetJsonDecimal(item, "regularMarketChangePercent") ?? 0;

                movers.Add(new MarketMoverDto
                {
                    Symbol = symbol,
                    Name = GetJsonString(item, "shortName") ?? GetJsonString(item, "longName") ?? GetJsonString(item, "displayName") ?? symbol,
                    Price = Math.Round(price, 2),
                    Change = Math.Round(change, 2),
                    ChangePercent = Math.Round(changePercent, 2),
                    Volume = GetJsonLong(item, "regularMarketVolume"),
                    Exchange = GetJsonString(item, "fullExchangeName") ?? GetJsonString(item, "exchange"),
                    Sector = GetJsonString(item, "sector")
                });
            }

            return movers;
        }

        public async Task<List<MarketNewsDto>> GetMarketNewsAsync(int count)
        {
            count = count <= 0 ? 6 : count;

            var news = await GetMarketNewsFromSearchAsync("stock market", count, "https://query1.finance.yahoo.com", "en-US", "US");
            if (news.Count > 0) return news;

            news = await GetMarketNewsFromSearchAsync("stock market", count, "https://query2.finance.yahoo.com", "en-US", "US");
            if (news.Count > 0) return news;

            news = await GetMarketNewsFromSearchAsync("wall street", count, "https://query1.finance.yahoo.com", "en-US", "US");
            if (news.Count > 0) return news;

            news = await GetMarketNewsFromSearchAsync("S&P 500", count, "https://query1.finance.yahoo.com", "en-US", "US");
            if (news.Count > 0) return news;

            return await GetMarketNewsFromSearchAsync("investing", count, "https://query1.finance.yahoo.com", "en-US", "US");
        }

        private async Task<List<MarketNewsDto>> GetMarketNewsFromSearchAsync(string query, int count, string baseUrl, string lang, string region)
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"{baseUrl}/v1/finance/search?q={encodedQuery}&newsCount={count}&lang={lang}&region={region}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Accept-Language", "es-US,es;q=0.9,en;q=0.7");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return [];

            var json = await response.Content.ReadAsStringAsync();

            using var data = JsonDocument.Parse(json);

            if (!data.RootElement.TryGetProperty("news", out var newsArray) || newsArray.ValueKind != JsonValueKind.Array)
                return [];

            var result = new List<MarketNewsDto>();

            foreach (var item in newsArray.EnumerateArray())
            {
                var title = GetJsonString(item, "title");

                if (string.IsNullOrWhiteSpace(title))
                    continue;

                var publishedAt = GetPublishedDate(item);
                var relatedTickers = GetRelatedTickers(item);

                result.Add(new MarketNewsDto
                {
                    Id = GetJsonString(item, "uuid") ?? Guid.NewGuid().ToString(),
                    Title = title,
                    Source = GetJsonString(item, "publisher") ?? "Yahoo Finance",
                    Url = GetJsonString(item, "link") ?? string.Empty,
                    PublishedAt = publishedAt,
                    Time = publishedAt == null ? string.Empty : GetRelativeTime(publishedAt.Value),
                    Summary = relatedTickers.Count > 0
                        ? $"Noticia relacionada con {string.Join(", ", relatedTickers.Take(3))}. Continuá leyendo para ver el reporte completo."
                        : "Continuá leyendo para ver el reporte completo.",
                    RelatedTickers = relatedTickers
                });
            }

            return result.Take(count).ToList();
        }


        // HERLPERS
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

        private static string? GetJsonString(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        }

        private static decimal? GetJsonDecimal(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDecimal() : null;
        }

        private static long? GetJsonLong(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt64() : null;
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

        private static DateTime? GetPublishedDate(JsonElement item)
        {
            if (!item.TryGetProperty("providerPublishTime", out var value) || value.ValueKind != JsonValueKind.Number)
                return null;

            return DateTimeOffset.FromUnixTimeSeconds(value.GetInt64()).UtcDateTime;
        }

        private static List<string> GetRelatedTickers(JsonElement item)
        {
            if (!item.TryGetProperty("relatedTickers", out var tickers) || tickers.ValueKind != JsonValueKind.Array)
                return [];

            return tickers.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .ToList();
        }

        private static string GetRelativeTime(DateTime publishedAt)
        {
            var diff = DateTime.UtcNow - publishedAt;

            if (diff.TotalMinutes < 1)
                return "Ahora";

            if (diff.TotalMinutes < 60)
                return $"Hace {(int)diff.TotalMinutes} min";

            if (diff.TotalHours < 24)
                return $"Hace {(int)diff.TotalHours} h";

            return $"Hace {(int)diff.TotalDays} días";
        }
    }
}