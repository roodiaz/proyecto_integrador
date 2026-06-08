using static InvestLab.Models.Enums;

namespace InvestLab.Models.DTOs.Transaction
{
    public class TransactionFilterDto
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public string? Symbol { get; set; }

        public TransactionType? Type { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        // date | symbol | sector | quantity | price | total
        public string? SortBy { get; set; }

        // asc | desc
        public string? SortDirection { get; set; }
    }
}
