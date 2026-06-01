using InvestLab.Integrations.Configuration;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models.DTOs.Market;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Microsoft.Extensions.Options;
using InvestLab.Models.DTOs;

namespace InvestLab.Integrations.Providers
{
    public class YahooMarketProvider : IExternalProvider
    {
        private readonly YahooOptions _options;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public YahooMarketProvider(HttpClient httpClient, IConfiguration config, IOptions<YahooOptions> options)
        {
            _httpClient = httpClient;
            _config = config;
            _options = options.Value;
        }

        public async Task<MarketPriceDto?> GetPriceAsync(string symbol)
        {
            var url = $"{_options.BaseUrl}/v8/finance/chart/{symbol}?range=1d&interval=1m";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            using var data = JsonDocument.Parse(json);

            var result = data.RootElement
                .GetProperty("chart")
                .GetProperty("result")[0];

            var meta = result.GetProperty("meta");
            var price = meta.GetProperty("regularMarketPrice").GetDecimal();
            var previousClose = meta.GetProperty("previousClose").GetDecimal();

            var variation = ((price - previousClose) / previousClose) * 100;

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
            var period1 = ((DateTimeOffset)from).ToUnixTimeSeconds();
            var period2 = ((DateTimeOffset)to).ToUnixTimeSeconds();

            var url =
                $"{_options.BaseUrl}/v8/finance/chart/{symbol}" +
                $"?period1={period1}" +
                $"&period2={period2}" +
                $"&interval=1d";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return [];

            var json = await response.Content.ReadAsStringAsync();

            using var data = JsonDocument.Parse(json);

            var result = data.RootElement
                .GetProperty("chart")
                .GetProperty("result")[0];

            var timestamps = result
                .GetProperty("timestamp")
                .EnumerateArray()
                .Select(x => x.GetInt64())
                .ToList();

            var quote = result
                .GetProperty("indicators")
                .GetProperty("quote")[0];

            var opens = quote.GetProperty("open")
                .EnumerateArray()
                .ToList();

            var highs = quote.GetProperty("high")
                .EnumerateArray()
                .ToList();

            var lows = quote.GetProperty("low")
                .EnumerateArray()
                .ToList();

            var closes = quote.GetProperty("close")
                .EnumerateArray()
                .ToList();

            var volumes = quote.GetProperty("volume")
                .EnumerateArray()
                .ToList();

            var history = new List<HistoricalPriceDto>();

            for (int i = 0; i < timestamps.Count; i++)
            {
                if (closes[i].ValueKind == JsonValueKind.Null)
                    continue;

                history.Add(new HistoricalPriceDto
                {
                    Date = DateTimeOffset
                        .FromUnixTimeSeconds(timestamps[i])
                        .UtcDateTime,

                    Open = opens[i].ValueKind == JsonValueKind.Null
                        ? 0
                        : opens[i].GetDecimal(),

                    High = highs[i].ValueKind == JsonValueKind.Null
                        ? 0
                        : highs[i].GetDecimal(),

                    Low = lows[i].ValueKind == JsonValueKind.Null
                        ? 0
                        : lows[i].GetDecimal(),

                    Close = closes[i].GetDecimal(),

                    Volume = volumes[i].ValueKind == JsonValueKind.Null
                        ? 0
                        : volumes[i].GetInt64()
                });
            }

            return history;
        }

        public Task<AssetProfileDto?> GetProfileAsync(string symbol)
        {
            throw new NotImplementedException();
        }
    }
}
