namespace InvestLab.Data.Interfaces
{
    public interface IAssetRepository
    {
        Task<Asset?> GetBySymbolAsync(string symbol);
    }
}
