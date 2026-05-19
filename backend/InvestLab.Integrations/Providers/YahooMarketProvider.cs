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
    }
}
