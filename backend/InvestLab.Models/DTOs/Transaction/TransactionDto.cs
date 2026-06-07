using static InvestLab.Models.Enums;

namespace InvestLab.Models.DTOs.Transaction
{
    public class TransactionDto
    {
        public DateTime OperationDate { get; set; }

        public string Symbol { get; set; } = string.Empty;

        public string? Sector { get; set; }

        public TransactionType Type { get; set; }

        public decimal Quantity { get; set; }

        public decimal BuyPrice { get; set; }

        public decimal Total { get; set; }
    }
}
