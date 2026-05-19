using InvestLab.Integrations.Configuration;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models.DTOs.Market;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace InvestLab.Integrations.Providers
{
    public class YahooMarketProvider : IExternalProvider
    {
        private readonly YahooOptions _options;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public YahooMarketProvider(HttpClient httpClient, IConfiguration config, YahooOptions options)
        {
            _httpClient = httpClient;
            _config = config;
            _options = options;
        }

        public async Task<MarketPriceDto?> GetPriceAsync(string symbol)
        {
            var url = $"{_options.BaseUrl}/v7/finance/quote?symbols={symbol}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            var data = JsonDocument.Parse(json);

            var result = data.RootElement
                .GetProperty("quoteResponse")
                .GetProperty("result")[0];

            var price = result.GetProperty("regularMarketPrice").GetDecimal();
            var previousClose = result.GetProperty("regularMarketPreviousClose").GetDecimal();

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
            var resultList = new List<MarketPriceDto>();

            if (symbols == null || !symbols.Any())
                return resultList;

            var symbolsQuery = string.Join(",", symbols);

            var url = $"{_options.BaseUrl}/v7/finance/quote?symbols={symbolsQuery}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return resultList;

            var json = await response.Content.ReadAsStringAsync();

            using var data = JsonDocument.Parse(json);

            var results = data.RootElement
                .GetProperty("quoteResponse")
                .GetProperty("result");

            foreach (var item in results.EnumerateArray())
            {
                var symbol = item.GetProperty("symbol").GetString();

                var price = item.GetProperty("regularMarketPrice").GetDecimal();
                var previousClose = item.GetProperty("regularMarketPreviousClose").GetDecimal();

                var variation = ((price - previousClose) / previousClose) * 100;

                resultList.Add(new MarketPriceDto
                {
                    Symbol = symbol,
                    Price = price,
                    PreviousClose = previousClose,
                    VariationPercent = Math.Round(variation, 2)
                });
            }

            return resultList;
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
    }
}
