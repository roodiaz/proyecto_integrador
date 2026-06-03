using InvestLab.Models.Documents;

namespace InvestLab.Data.Interfaces
{
    public interface IMarketMetadataRepository
    {
        Task<MarketMetadata?> GetAsync();

        Task UpdateLastCloseAsync(DateTime marketCloseDate);
    }
}
