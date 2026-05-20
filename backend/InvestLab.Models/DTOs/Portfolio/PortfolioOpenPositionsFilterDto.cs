namespace InvestLab.Models.DTOs.Portfolio
{
    public class PortfolioOpenPositionsFilterDto
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public string? Symbol { get; set; }

        public string? SortBy { get; set; }
    }
}
