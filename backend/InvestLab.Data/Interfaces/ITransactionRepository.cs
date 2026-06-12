using InvestLab.Models.DTOs.Transaction;

namespace InvestLab.Data.Interfaces
{
    public interface ITransactionRepository
    {
        Task InsertAsync(Transaction transaction);

        Task<List<Transaction>> GetLatestByPortfolioAsync(int portfolioId, int take);

        Task<(List<TransactionDto> Data, int Total)> SearchAsync(int portfolioId, TransactionFilterDto filter);

        Task DeleteByPortfolioIdAsync(int portfolioId);

        Task<List<Transaction>> GetByPortfolioAfterDateAsync(int portfolioId, DateTime fromUtc);

        Task<List<Transaction>> GetAllByPortfolioAsync(int portfolioId);

        Task<List<TransactionDto>> GetAllForExportAsync(int portfolioId, TransactionFilterDto filter);
    }
}
