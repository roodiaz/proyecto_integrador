using static InvestLab.Models.Enums;

namespace InvestLab.Models.DTOs.Transaction
{
    public class TransactionFilterDto
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public string? Symbol { get; set; }

        public TransactionType? Type { get; set; }

        public int? Days { get; set; }

        public string? OrderBy { get; set; } = "date";
    }
}
