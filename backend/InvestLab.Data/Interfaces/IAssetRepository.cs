namespace InvestLab.Data.Interfaces
{
    public interface IAssetRepository
    {
        Task<List<Asset>> GetAllAsync();

        Task<Asset?> GetAsync(string symbol);

        Task AddAsync(Asset asset);

        Task<List<Asset>> GetPendingHistoryAsync();

        Task UpdateAsync(Asset asset);
    }
}
