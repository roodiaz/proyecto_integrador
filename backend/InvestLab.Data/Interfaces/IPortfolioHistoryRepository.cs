using InvestLab.Models.Documents;

namespace InvestLab.Data.Interfaces
{
    public interface IPortfolioHistoryRepository
    {
        Task<List<PortfolioHistory>> GetByUserAndDateAsync(int userId, DateTime fromDate);
    }
}
