using InvestLab.Models;
using InvestLab.Models.DTOs.Portfolio;

namespace InvestLab.Business.Interfaces.Api
{
    public interface IPortfolioService
    {
        Task<Response> BuyAsync(int userId, BuyAssetDto dto);

        Task<Response> SellAsync(int userId, SellAssetDto dto);

        Task<Response> GetPositionForSellAsync(int userId, string symbol);

        Task<Response> GetPriceAsync(string symbol);

        Task<Response> GetBalanceCardsAsync(int userId);

        Task<Response> GetPieChartAsync(int userId);

        Task<Response> GetOpenPositionsAsync(int userId, PortfolioOpenPositionsFilterDto filter);

        Task<Response> GetLineChartAsync(int userId, PortfolioLineChartFilterDto filter);

        Task<Response> ResetSimulationAsync(int userId);
    }
}
