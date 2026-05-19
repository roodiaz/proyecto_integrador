using InvestLab.Models.DTOs.Alerts;
using static InvestLab.Models.Enums;

namespace InvestLab.Data.Interfaces
{
    public interface IAlertRepository
    {
        Task AddAsync(Alert alert);

        Task<Alert?> GetByIdAsync(int id);

        Task DeleteAsync(Alert alert);

        Task<int> CountByUserAsync(int userId);

        Task<bool> ExistsAsync(int userId, int assetId, ConditionType type, AlertOperator op, decimal value);

        Task<(List<Alert>, int total)> GetPagedAsync(int userId, AlertFilterDto filter);

        Task<(int active, int paused, int triggeredToday, int total)> GetStatsAsync(int userId);

        Task<List<Alert>> GetActiveAlertsAsync();

        Task UpdateAsync(Alert alert);
    }
}
