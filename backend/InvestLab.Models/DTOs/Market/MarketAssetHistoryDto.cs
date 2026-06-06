namespace InvestLab.Models.DTOs.Market
{
    public class MarketAssetHistoryDto
    {
        public string Symbol { get; set; } = string.Empty;
        public string Range { get; set; } = string.Empty;
        public MarketHistorySeriesDto Series { get; set; } = new();
    }
}