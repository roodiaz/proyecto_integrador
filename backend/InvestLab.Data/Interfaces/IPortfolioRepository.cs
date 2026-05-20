using InvestLab.Models.Documents;

namespace InvestLab.Data.Interfaces
{
    public interface IPortfolioRepository
    {
        Task<Portfolio?> GetByUserAndAssetAsync(int userId, int assetId);

        Task InsertAsync(Portfolio portfolio);

        Task UpdateAsync(Portfolio portfolio);

        Task DeleteAsync(Portfolio portfolio);

        Task<List<Portfolio>> GetByUserAsync(int userId);

        Task<List<Portfolio>> GetPagedByUserAsync(int userId);
    }
}
