namespace InvestLab.Models.DTOs.Market
{
    public class MarketNewsDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime? PublishedAt { get; set; }
        public string Time { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<string> RelatedTickers { get; set; } = [];
    }
}