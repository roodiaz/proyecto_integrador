using InvestLab.Models;
using InvestLab.Models.DTOs.Dashboard;

namespace InvestLab.Business.Interfaces.Api
{
    public interface IDashboardService
    {
        Task<Response> GetTopCardsAsync(int userId);

        Task<Response> GetPortfolioDistributionAsync(int userId);

        Task<Response> GetRecentNotificationsAsync(int userId);

        Task<Response> GetPerformanceChartAsync(int userId, DashboardPerformanceChartFilterDto filter);

        Task<Response> GetLatestTransactionsAsync(int userId);
    }
}
