using InvestLab.Models.Documents;

namespace InvestLab.Data.Interfaces
{
    public interface IPortfolioHistoryRepository
    {
        Task InsertAsync(PortfolioHistory history);

        Task<List<PortfolioHistory>> GetByUserAndDateAsync(int userId, DateTime fromDate);

        Task<PortfolioHistory?> GetLatestAsync(int userId);

        Task<PortfolioHistory?> GetPreviousAsync(int userId);

        Task<bool> ExistsByDateAsync(int userId, DateTime date);
    }
}
