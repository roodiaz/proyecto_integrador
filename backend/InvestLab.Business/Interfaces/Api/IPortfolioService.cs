using InvestLab.Models;
using InvestLab.Models.DTOs.Portfolio;

namespace InvestLab.Business.Interfaces.Api
{
    public interface IPortfolioService
    {
        Task<Response> GetUserPortfoliosAsync(int userId);

        Task<Response> CreatePortfolioAsync(int userId, SetupPortfolioDto dto);

        Task<Response> SetActivePortfolioAsync(int userId, int portfolioId);

        Task<Response> DeletePortfolioAsync(int userId, int portfolioId);

        Task<Response> ResetPortfolioAsync(int userId, int portfolioId, SetupPortfolioDto dto);

        Task<Response> BuyAsync(int userId, int portfolioId, BuyAssetDto dto);

        Task<Response> SellAsync(int userId, int portfolioId, SellAssetDto dto);

        Task<Response> GetPositionForSellAsync(int userId, int portfolioId, string symbol);

        Task<Response> GetPriceAsync(string symbol);

        Task<Response> GetBalanceCardsAsync(int userId, int portfolioId);

        Task<Response> GetPieChartAsync(int userId, int portfolioId);

        Task<Response> GetOpenPositionsAsync(int userId, int portfolioId, PortfolioOpenPositionsFilterDto filter);

        Task<Response> GetLineChartAsync(int userId, int portfolioId, PortfolioLineChartFilterDto filter);

        Task<byte[]> ExportHoldingsToExcelAsync(int userId, int portfolioId, PortfolioOpenPositionsFilterDto filter);
    }
}
