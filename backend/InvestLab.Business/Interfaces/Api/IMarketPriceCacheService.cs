using InvestLab.Models.DTOs.Market;

namespace InvestLab.Business.Interfaces.Api
{
    public interface IMarketPriceCacheService
    {
        Task<MarketPriceDto?> GetPriceAsync(string symbol);

        Task<MarketPricesResponseDto> GetPricesAsync(List<string> symbols);

        Task RefreshPricesAsync(List<string> symbols);

        Task<DateTime?> GetLastUpdatedAtAsync();
    }
}
