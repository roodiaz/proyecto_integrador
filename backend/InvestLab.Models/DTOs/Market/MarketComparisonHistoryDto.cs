namespace InvestLab.Models.DTOs.Market
{
    public class MarketComparisonHistoryDto
    {
        public string Range { get; set; } = string.Empty;
        public List<MarketHistorySeriesDto> Series { get; set; } = new();
    }
}