namespace InvestLab.Models.DTOs.Portfolio
{
    public class PortfolioOpenPositionsFilterDto
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public string? Symbol { get; set; }

        // symbol | sector | quantity | averagePrice | currentPrice | variationPercent | profitLoss
        public string? SortBy { get; set; }

        // asc | desc
        public string? SortDirection { get; set; } = "asc";
    }
}
