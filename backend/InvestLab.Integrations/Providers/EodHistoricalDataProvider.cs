using InvestLab.Integrations.Configuration;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models.DTOs;
using InvestLab.Models.DTOs.Market;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using static InvestLab.Models.Enums;

namespace InvestLab.Integrations.Providers;

public class EodHistoricalDataProvider : IExternalProvider
{
    private readonly HttpClient _httpClient;
    private readonly MarketProviderOptions _options;
    private readonly ILogger<EodHistoricalDataProvider> _logger;

    public MarketProviderType ProviderType => MarketProviderType.EodHistoricalData;

    public EodHistoricalDataProvider(HttpClient httpClient, IOptions<MarketDataOptions> options, ILogger<EodHistoricalDataProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value.Providers.EodHistoricalData;
        _logger = logger;
    }

    public async Task<MarketPriceDto?> GetPriceAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol) || !HasApiKey())
            return null;

        symbol = symbol.Trim().ToUpper();

        var url = $"{_options.BaseUrl}/real-time/{Uri.EscapeDataString(symbol)}?api_token={_options.ApiKey}&fmt=json";

        var root = await GetJsonAsync(url);

        if (root == null || root.Value.ValueKind != JsonValueKind.Object)
            return null;

        var price = GetJsonDecimal(root.Value, "close") ?? 0;
        var previousClose = GetJsonDecimal(root.Value, "previousClose") ?? 0;
        var changePercent = GetJsonDecimal(root.Value, "change_p");

        if (changePercent == null && previousClose > 0)
            changePercent = ((price - previousClose) / previousClose) * 100;

        return new MarketPriceDto
        {
            Symbol = GetJsonString(root.Value, "code") ?? symbol,
            Price = price,
            PreviousClose = previousClose,
            VariationPercent = Math.Round(changePercent ?? 0, 2),
            Open = GetJsonDecimal(root.Value, "open"),
            Volume = GetJsonLong(root.Value, "volume"),
            DayHigh = GetJsonDecimal(root.Value, "high"),
            DayLow = GetJsonDecimal(root.Value, "low")
        };
    }

    public async Task<List<MarketPriceDto>> GetPricesAsync(List<string> symbols)
    {
        if (symbols == null || !symbols.Any())
            return [];

        var cleanSymbols = symbols.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToUpper()).Distinct().ToArray();

        if (cleanSymbols.Length == 0)
            return [];

        var tasks = cleanSymbols.Select(GetPriceAsync);
        var results = await Task.WhenAll(tasks);

        return results.Where(x => x != null).Cast<MarketPriceDto>().ToList();
    }

    public async Task<List<HistoricalPriceDto>> GetHistoricalAsync(string symbol, DateTime from, DateTime to)
    {
        if (string.IsNullOrWhiteSpace(symbol) || !HasApiKey())
            return [];

        symbol = symbol.Trim().ToUpper();

        var url = $"{_options.BaseUrl}/eod/{Uri.EscapeDataString(symbol)}" +
                  $"?api_token={_options.ApiKey}&fmt=json" +
                  $"&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";

        var root = await GetJsonAsync(url);

        if (root == null || root.Value.ValueKind != JsonValueKind.Array)
            return [];

        return ParseEodCandles(root.Value);
    }

    public async Task<AssetProfileDto?> GetProfileAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol) || !HasApiKey())
            return null;

        symbol = symbol.Trim().ToUpper();

        var url = $"{_options.BaseUrl}/fundamentals/{Uri.EscapeDataString(symbol)}?api_token={_options.ApiKey}&fmt=json";

        var root = await GetJsonAsync(url);

        if (root == null || root.Value.ValueKind != JsonValueKind.Object)
            return null;

        if (!root.Value.TryGetProperty("General", out var general) || general.ValueKind != JsonValueKind.Object)
            return null;

        return new AssetProfileDto
        {
            Symbol = GetJsonString(general, "Code") ?? symbol,
            Name = GetJsonString(general, "Name") ?? symbol,
            Sector = GetJsonString(general, "Sector") ?? "Unknown"
        };
    }

    public async Task<List<MarketMoverDto>> GetMarketMoversAsync(string screenerId, int count)
    {
        if (!HasApiKey())
            return [];

        count = count <= 0 ? 6 : count;

        var url = $"{_options.BaseUrl}/screener?api_token={_options.ApiKey}&fmt=json" +
                  $"&sort=change_p.desc&limit={count}";

        var root = await GetJsonAsync(url);

        if (root == null)
            return [];

        if (!root.Value.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            return [];

        var movers = new List<MarketMoverDto>();

        foreach (var item in data.EnumerateArray())
        {
            var symbol = GetJsonString(item, "code");

            if (string.IsNullOrWhiteSpace(symbol))
                continue;

            movers.Add(new MarketMoverDto
            {
                Symbol = symbol,
                Name = GetJsonString(item, "name") ?? symbol,
                Price = Math.Round(GetJsonDecimal(item, "close") ?? 0, 2),
                Change = Math.Round(GetJsonDecimal(item, "change") ?? 0, 2),
                ChangePercent = Math.Round(GetJsonDecimal(item, "change_p") ?? 0, 2),
                Volume = GetJsonLong(item, "volume"),
                Exchange = GetJsonString(item, "exchange"),
                Sector = GetJsonString(item, "sector")
            });
        }

        return movers;
    }

    public async Task<List<MarketNewsDto>> GetMarketNewsAsync(int count)
    {
        if (!HasApiKey())
            return [];

        count = count <= 0 ? 6 : count;

        var url = $"{_options.BaseUrl}/news?api_token={_options.ApiKey}&fmt=json&limit={count}";

        var root = await GetJsonAsync(url);

        if (root == null || root.Value.ValueKind != JsonValueKind.Array)
            return [];

        var result = new List<MarketNewsDto>();

        foreach (var item in root.Value.EnumerateArray())
        {
            var title = GetJsonString(item, "title");

            if (string.IsNullOrWhiteSpace(title))
                continue;

            var publishedAt = GetJsonDateTime(item, "date");
            var relatedTickers = GetRelatedSymbols(item);

            result.Add(new MarketNewsDto
            {
                Id = GetJsonString(item, "link") ?? Guid.NewGuid().ToString(),
                Title = title,
                Source = "EOD Historical Data",
                Url = GetJsonString(item, "link") ?? string.Empty,
                PublishedAt = publishedAt,
                Time = publishedAt == null ? string.Empty : GetRelativeTime(publishedAt.Value),
                Summary = GetJsonString(item, "content") ?? string.Empty,
                RelatedTickers = relatedTickers
            });
        }

        return result.Take(count).ToList();
    }

    public async Task<List<HistoricalPriceDto>> GetChartHistoryAsync(string symbol, string range)
    {
        if (string.IsNullOrWhiteSpace(symbol) || !HasApiKey())
            return [];

        symbol = symbol.Trim().ToUpper();

        var (from, to) = GetChartDateRange(range);

        var url = $"{_options.BaseUrl}/eod/{Uri.EscapeDataString(symbol)}" +
                  $"?api_token={_options.ApiKey}&fmt=json" +
                  $"&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";

        var root = await GetJsonAsync(url);

        if (root == null || root.Value.ValueKind != JsonValueKind.Array)
            return [];

        return ParseEodCandles(root.Value);
    }

    // HELPERS
    private bool HasApiKey()
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            return true;

        _logger.LogWarning("EOD Historical Data: falta configurar la ApiKey, no se puede consumir el proveedor");
        return false;
    }

    private async Task<JsonElement?> GetJsonAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("EOD Historical Data: se alcanzó el límite de requests (rate limit)");
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("EOD Historical Data: respuesta HTTP no exitosa ({StatusCode})", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json))
                return null;

            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "EOD Historical Data: error al deserializar la respuesta");
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "EOD Historical Data: error al consumir la API");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "EOD Historical Data: timeout al consumir la API");
            return null;
        }
    }

    private static List<HistoricalPriceDto> ParseEodCandles(JsonElement array)
    {
        var list = new List<HistoricalPriceDto>();

        foreach (var item in array.EnumerateArray())
        {
            var date = GetJsonDateTime(item, "date");
            var close = GetJsonDecimal(item, "close");

            if (date == null || close == null)
                continue;

            list.Add(new HistoricalPriceDto
            {
                Date = date.Value,
                Open = GetJsonDecimal(item, "open") ?? close.Value,
                High = GetJsonDecimal(item, "high") ?? close.Value,
                Low = GetJsonDecimal(item, "low") ?? close.Value,
                Close = close.Value,
                Volume = GetJsonLong(item, "volume") ?? 0
            });
        }

        return list.OrderBy(x => x.Date).ToList();
    }

    private static (DateTime From, DateTime To) GetChartDateRange(string range)
    {
        var to = DateTime.UtcNow.Date;

        var from = (string.IsNullOrWhiteSpace(range) ? "1m" : range.Trim().ToLower()) switch
        {
            "1d" => to.AddDays(-1),
            "1w" => to.AddDays(-7),
            "1m" => to.AddMonths(-1),
            "3m" => to.AddMonths(-3),
            "6m" => to.AddMonths(-6),
            "1y" => to.AddYears(-1),
            _ => to.AddMonths(-1)
        };

        return (from, to);
    }

    private static string? GetJsonString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static decimal? GetJsonDecimal(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetDecimal(),
            JsonValueKind.String => decimal.TryParse(value.GetString(), out var parsed) ? parsed : null,
            _ => null
        };
    }

    private static long? GetJsonLong(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetInt64(),
            JsonValueKind.String => long.TryParse(value.GetString(), out var parsed) ? parsed : null,
            _ => null
        };
    }

    private static DateTime? GetJsonDateTime(JsonElement element, string property)
    {
        var value = GetJsonString(element, property);

        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTime.TryParse(value, out var date) ? DateTime.SpecifyKind(date, DateTimeKind.Utc) : null;
    }

    private static List<string> GetRelatedSymbols(JsonElement item)
    {
        if (!item.TryGetProperty("symbols", out var symbols) || symbols.ValueKind != JsonValueKind.Array)
            return [];

        return symbols.EnumerateArray()
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
