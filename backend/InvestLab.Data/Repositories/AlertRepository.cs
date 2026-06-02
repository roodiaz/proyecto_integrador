using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using InvestLab.Models.DTOs.Alerts;
using Microsoft.EntityFrameworkCore;
using static InvestLab.Models.Enums;

namespace InvestLab.Data.Repositories
{
    public class AlertRepository : IAlertRepository
    {
        private readonly InvestLabDbContext _context;

        public AlertRepository(InvestLabDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Alert alert)
        {
            await _context.Alerts.AddAsync(alert);
        }

        public async Task<Alert?> GetByIdAsync(int id)
        {
            return await _context.Alerts
                .Include(x => x.Asset)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task DeleteAsync(Alert alert)
        {
            _context.Alerts.Remove(alert);
            return Task.CompletedTask;
        }

        public async Task<int> CountByUserAsync(int userId)
        {
            return await _context.Alerts.CountAsync(x => x.UserId == userId);
        }

        public async Task<bool> ExistsAsync(int userId, int assetId, ConditionType type, AlertOperator op, decimal value)
        {
            return await _context.Alerts.AnyAsync(x =>
                x.UserId == userId &&
                x.AssetId == assetId &&
                x.ConditionType == type &&
                x.Operator == op &&
                x.Value == value);
        }

        public async Task<(List<Alert>, int)> GetPagedAsync(int userId, AlertFilterDto filter)
        {
            var query = _context.Alerts
                .Include(a => a.Asset)
                .Where(a => a.UserId == userId);

            if (filter.IsActive.HasValue)
                query = query.Where(a => a.IsActive == filter.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim().ToLower();
                query = query.Where(a =>a.Asset.Symbol.ToLower().Contains(search));
            }

            if (filter.CreatedFrom.HasValue)
            {
                var createdFrom = DateTime.SpecifyKind(
                    filter.CreatedFrom.Value.Date,
                    DateTimeKind.Utc);

                query = query.Where(a => a.CreatedAt >= createdFrom);
            }

            if (filter.CreatedTo.HasValue)
            {
                var createdTo = DateTime.SpecifyKind(
                    filter.CreatedTo.Value.Date.AddDays(1),
                    DateTimeKind.Utc);

                query = query.Where(a => a.CreatedAt < createdTo);
            }

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (data, total);
        }

        public async Task<(int, int, int, int)> GetStatsAsync(int userId)
        {
            var query = _context.Alerts.Where(x => x.UserId == userId);

            var active = await query.CountAsync(x => x.IsActive == true);
            var paused = await query.CountAsync(x => x.IsActive == false);

            var today = DateTime.UtcNow.Date;

            var triggeredToday = await query.CountAsync(x =>
                x.LastTriggered != null &&
                x.LastTriggered.Value.Date == today);

            var total = await query.CountAsync();

            return (active, paused, triggeredToday, total);
        }

        public async Task<List<Alert>> GetActiveAlertsAsync()
        {
            return await _context.Alerts.Include(x => x.Asset)
                .Where(x => x.IsActive)
                .ToListAsync();
        }

        public Task UpdateAsync(Alert alert)
        {
            _context.Alerts.Update(alert);

            return Task.CompletedTask;
        }

        public async Task<List<Alert>> GetLatestActiveByUserAsync(int userId, int take)
        {
            return await _context.Alerts
                .Include(x => x.Asset)
                .Where(x => x.UserId == userId && x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .Take(take)
                .ToListAsync();
        }
    }
}

