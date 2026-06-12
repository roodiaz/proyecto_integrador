using InvestLab.Models;
using InvestLab.Models.DTOs.Dashboard;

namespace InvestLab.Business.Interfaces.Api
{
    public interface IDashboardService
    {
        Task<Response> GetTopCardsAsync(int userId, int portfolioId);

        Task<Response> GetPortfolioDistributionAsync(int userId, int portfolioId);

        Task<Response> GetRecentNotificationsAsync(int userId);

        Task<Response> GetPerformanceChartAsync(int userId, int portfolioId, DashboardPerformanceChartFilterDto filter);

        Task<Response> GetLatestTransactionsAsync(int userId, int portfolioId);

        Task<Response> GetPortfolioCompositionAsync(int userId, int portfolioId, DateTime date);
    }
}
