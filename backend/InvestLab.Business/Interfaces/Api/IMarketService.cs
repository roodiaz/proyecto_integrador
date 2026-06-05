using InvestLab.Models;

namespace InvestLab.Business.Interfaces
{
    public interface IMarketService
    {
        Task<Response> GetMarketOverviewAsync();

        Task<Response> GetAssetDetailAsync(string symbol);

        Task<Response> GetTrendingAsync();

        Task<Response> GetGainersAsync();

        Task<Response> GetLosersAsync();
    }
}