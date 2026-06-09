using InvestLab.Models.DTOs.Transaction;

namespace InvestLab.Data.Interfaces
{
    public interface ITransactionRepository
    {
        Task InsertAsync(Transaction transaction);

        Task<List<Transaction>> GetLatestByUserAsync(int userId, int take);

        Task<(List<TransactionDto> Data, int Total)> SearchAsync( int userId,TransactionFilterDto filter);

        Task DeleteByUserIdAsync(int userId);

        Task<List<Transaction>> GetByUserAfterDateAsync(int userId, DateTime fromUtc);

        Task<List<Transaction>> GetAllByUserAsync(int userId);

        Task<List<TransactionDto>> GetAllForExportAsync(int userId, TransactionFilterDto filter);
    }
}
