using InvestLab.Integrations.Configuration;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models.DTOs;
using InvestLab.Models.DTOs.Market;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace InvestLab.Integrations.Providers;

public class FinnhubMarketProvider : IExternalProvider
{
    private readonly HttpClient _httpClient;
    private readonly FinnhubOptions _options;

    public FinnhubMarketProvider(HttpClient httpClient, IOptions<FinnhubOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<MarketPriceDto?> GetPriceAsync(string symbol)
    {
        var url =
            $"{_options.BaseUrl}/quote" +
            $"?symbol={symbol}" +
            $"&token={_options.ApiKey}";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();

        using var data = JsonDocument.Parse(json);

        var root = data.RootElement;

        var price = root.GetProperty("c").GetDecimal();
        var previousClose = root.GetProperty("pc").GetDecimal();

        var variation = previousClose == 0 ? 0 : ((price - previousClose) / previousClose) * 100;

        return new MarketPriceDto
        {
            Symbol = symbol,
            Price = price,
            PreviousClose = previousClose,
            VariationPercent = Math.Round(variation, 2)
        };
    }

    public async Task<List<MarketPriceDto>> GetPricesAsync(List<string> symbols)
    {
        if (symbols == null || !symbols.Any())
            return [];

        var tasks = symbols.Select(GetPriceAsync);

        var results = await Task.WhenAll(tasks);

        return results
            .Where(x => x != null)
            .Cast<MarketPriceDto>()
            .ToList();
    }

    public async Task<List<HistoricalPriceDto>> GetHistoricalAsync(string symbol, DateTime from, DateTime to)
    {
        var fromUnix = ((DateTimeOffset)from).ToUnixTimeSeconds();

        var toUnix = ((DateTimeOffset)to).ToUnixTimeSeconds();

        var url =
            $"{_options.BaseUrl}/stock/candle" +
            $"?symbol={symbol}" +
            $"&resolution=D" +
            $"&from={fromUnix}" +
            $"&to={toUnix}" +
            $"&token={_options.ApiKey}";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return [];

        var json = await response.Content.ReadAsStringAsync();
        using var data = JsonDocument.Parse(json);
        var root = data.RootElement;

        if (!root.TryGetProperty("s", out var status))
            return [];

        if (status.GetString() != "ok")
            return [];

        var timestamps = root
            .GetProperty("t")
            .EnumerateArray()
            .Select(x => x.GetInt64())
            .ToList();

        var opens = root
            .GetProperty("o")
            .EnumerateArray()
            .Select(x => x.GetDecimal())
            .ToList();

        var highs = root
            .GetProperty("h")
            .EnumerateArray()
            .Select(x => x.GetDecimal())
            .ToList();

        var lows = root
            .GetProperty("l")
            .EnumerateArray()
            .Select(x => x.GetDecimal())
            .ToList();

        var closes = root
            .GetProperty("c")
            .EnumerateArray()
            .Select(x => x.GetDecimal())
            .ToList();

        var volumes = root
            .GetProperty("v")
            .EnumerateArray()
            .Select(x => x.GetInt64())
            .ToList();

        var history =
            new List<HistoricalPriceDto>();

        for (int i = 0; i < timestamps.Count; i++)
        {
            history.Add(
                new HistoricalPriceDto
                {
                    Date = DateTimeOffset
                        .FromUnixTimeSeconds(
                            timestamps[i])
                        .UtcDateTime,

                    Open = opens[i],
                    High = highs[i],
                    Low = lows[i],
                    Close = closes[i],
                    Volume = volumes[i]
                });
        }

        return history;
    }

    public async Task<AssetProfileDto?> GetProfileAsync(string symbol)
    {
        var url =
            $"{_options.BaseUrl}/stock/profile2" +
            $"?symbol={symbol}" +
            $"&token={_options.ApiKey}";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        using var data = JsonDocument.Parse(json);
        var root = data.RootElement;

        // Finnhub devuelve {} cuando no existe
        if (!root.EnumerateObject().Any())
            return null;

        return new AssetProfileDto
        {
            Symbol = root.TryGetProperty("ticker", out var ticker)
                    ? ticker.GetString() ?? symbol
                   : symbol,
            Name = root.TryGetProperty("name", out var name)
                    ? name.GetString() ?? symbol
                    : symbol,
            Sector = root.TryGetProperty("finnhubIndustry", out var industry)
                    ? industry.GetString() ?? "Unknown"
                    : "Unknown"
        };
    }
}