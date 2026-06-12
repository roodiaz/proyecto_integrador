namespace InvestLab.Data.Interfaces
{
    public interface IUserPortfolioRepository
    {
        Task<List<UserPortfolio>> GetByUserAsync(int userId);

        Task<UserPortfolio?> GetByIdAsync(int portfolioId);

        Task<UserPortfolio?> GetByIdAndUserAsync(int portfolioId, int userId);

        Task<UserPortfolio?> GetActiveByUserAsync(int userId);

        Task<int> CountByUserAsync(int userId);

        Task InsertAsync(UserPortfolio portfolio);

        Task UpdateAsync(UserPortfolio portfolio);

        Task DeleteAsync(UserPortfolio portfolio);

        Task<List<UserPortfolio>> GetAllAsync();

        Task SetActiveAsync(int userId, int portfolioId);
    }
}
