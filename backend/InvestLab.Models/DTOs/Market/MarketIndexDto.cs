namespace InvestLab.Models.DTOs.Market
{
    public class MarketIndexDto
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal PreviousClose { get; set; }
        public decimal Change { get; set; }
        public decimal ChangePercent { get; set; }
        public string Trend { get; set; } = string.Empty;
    }
}