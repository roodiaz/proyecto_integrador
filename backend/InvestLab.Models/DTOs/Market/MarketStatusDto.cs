namespace InvestLab.Models.DTOs.Market
{
    public class MarketStatusDto
    {
        public bool IsOpen { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string MarketTime { get; set; } = string.Empty;
        public string TimeZone { get; set; } = "America/New_York";
        public string OpenTime { get; set; } = "09:30";
        public string CloseTime { get; set; } = "16:00";
    }
}
