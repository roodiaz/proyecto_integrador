using InvestLab.Models.DTOs.Market.InvestLab.Models.DTOs.Market;

namespace InvestLab.Models.DTOs.Market
{
    public class MarketHistorySeriesDto
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<MarketHistoryPointDto> Points { get; set; } = new();
    }
}