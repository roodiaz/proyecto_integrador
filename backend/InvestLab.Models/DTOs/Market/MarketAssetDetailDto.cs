namespace InvestLab.Models.DTOs.Market
{
    public class MarketAssetDetailDto
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? Change { get; set; }
        public decimal? ChangePercent { get; set; }
        public decimal? Open { get; set; }
        public long? Volume { get; set; }
        public long? AvgVolume { get; set; }
        public decimal? DayHigh { get; set; }
        public decimal? DayLow { get; set; }
        public long? MarketCap { get; set; }
        public decimal? PeRatio { get; set; }
        public decimal? DividendYield { get; set; }
        public string? Sector { get; set; }
    }
}