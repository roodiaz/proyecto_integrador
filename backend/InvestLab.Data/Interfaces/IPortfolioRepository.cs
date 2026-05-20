namespace InvestLab.Data.Interfaces
{
    public interface IPortfolioRepository
    {
        Task<Portfolio?> GetByUserAndAssetAsync(int userId, int assetId);

        Task InsertAsync(Portfolio portfolio);

        Task UpdateAsync(Portfolio portfolio);

        Task DeleteAsync(Portfolio portfolio);
    }
}
