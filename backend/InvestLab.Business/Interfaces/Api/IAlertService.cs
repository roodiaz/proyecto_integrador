using InvestLab.Models;
using InvestLab.Models.DTOs.Alerts;

namespace InvestLab.Business.Interfaces.Api
{
    public interface IAlertService
    {
        Task<Response> CreateAlertAsync(int userId, CreateAlertDto dto);
        Task<Response> DeleteAlertAsync(int userId, int id);
        Task<Response> ToggleAlertAsync(int userId, int id);
        Task<Response> GetAlertsAsync(int userId, AlertFilterDto filter);
        Task<Response> UpdateAlertAsync(int userId, UpdateAlertDto dto);
        Task<Response> GetStatsAsync(int userId);
    }
}
