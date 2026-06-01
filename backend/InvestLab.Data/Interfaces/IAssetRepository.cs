namespace InvestLab.Data.Interfaces
{
    public interface IAssetRepository
    {
        Task<List<Asset>> GetAllSymbolsAsync();

        Task<Asset?> GetBySymbolAsync(string symbol);

        Task AddAsync(Asset asset);

        Task<List<Asset>> GetPendingHistoryAsync();

        Task UpdateAsync(Asset asset);
    }
}
