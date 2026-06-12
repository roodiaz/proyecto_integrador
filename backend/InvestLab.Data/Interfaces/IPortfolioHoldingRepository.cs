namespace InvestLab.Data.Interfaces
{
    public interface IPortfolioHoldingRepository
    {
        Task<PortfolioHolding?> GetByPortfolioAndAssetAsync(int portfolioId, int assetId);

        Task InsertAsync(PortfolioHolding portfolioHolding);

        Task UpdateAsync(PortfolioHolding portfolioHolding);

        Task DeleteAsync(PortfolioHolding portfolioHolding);

        Task<List<PortfolioHolding>> GetByPortfolioAsync(int portfolioId);

        Task<List<PortfolioHolding>> GetPagedByPortfolioAsync(int portfolioId);

        Task DeleteByPortfolioIdAsync(int portfolioId);
    }
}
