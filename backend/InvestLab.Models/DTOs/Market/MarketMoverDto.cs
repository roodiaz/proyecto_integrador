namespace InvestLab.Models.DTOs.Market
{
    public class MarketMoverDto
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Change { get; set; }
        public decimal ChangePercent { get; set; }
        public long? Volume { get; set; }
        public string? Exchange { get; set; }
        public string? Sector { get; set; }
    }
}