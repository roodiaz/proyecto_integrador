namespace InvestLab.Models.DTOs.Portfolio
{
    public class PortfolioOpenPositionDto
    {
        public string Symbol { get; set; } = default!;

        public string? Sector { get; set; }

        public decimal Quantity { get; set; }

        public decimal AveragePrice { get; set; }

        public decimal CurrentPrice { get; set; }

        public decimal VariationPercent { get; set; }

        public decimal ProfitLoss { get; set; }

        public decimal ProfitLossPercent { get; set; }

        public bool IsOpen { get; set; }
    }
}
