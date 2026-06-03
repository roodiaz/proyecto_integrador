namespace InvestLab.Models.DTOs.Transaction
{
    public class TransactionSearchResponseDto
    {
        public List<TransactionDto> Data { get; set; } = [];

        public int Total { get; set; }
    }
}
