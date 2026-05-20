namespace InvestLab.Models.DTOs.Portfolio
{
    public class PortfolioOpenPositionDto
    {
        public string Symbol { get; set; } = default!;

        public decimal Quantity { get; set; }

        public decimal BuyPrice { get; set; }

        public decimal CurrentPrice { get; set; }

        public decimal VariationPercent { get; set; }

        public decimal ProfitLoss { get; set; }

        public decimal ProfitLossPercent { get; set; }

        public bool IsOpen { get; set; }
    }
}
