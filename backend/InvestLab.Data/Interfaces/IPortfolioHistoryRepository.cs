using InvestLab.Models.Documents;

namespace InvestLab.Data.Interfaces
{
    public interface IPortfolioHistoryRepository
    {
        Task InsertAsync(PortfolioHistory history);

        Task<List<PortfolioHistory>> GetByUserAndDateAsync(int userId, DateTime fromDate);
    }
}
