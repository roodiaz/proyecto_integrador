namespace InvestLab.Models.DTOs.Market
{
    public class MarketOverviewDto
    {
        public MarketStatusDto MarketStatus { get; set; } = new();
        public List<MarketIndexDto> Indices { get; set; } = [];
    }
}