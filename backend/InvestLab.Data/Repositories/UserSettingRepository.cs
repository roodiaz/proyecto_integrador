using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data.Repositories
{
    public class UserSettingRepository : IUserSettingRepository
    {
        private readonly InvestLabDbContext _context;

        public UserSettingRepository(InvestLabDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(UserSetting setting)
        {
            await _context.UserSettings.AddAsync(setting);
        }

        public async Task<UserSetting?> GetByUserIdAsync(int userId)
        {
            return await _context.UserSettings.FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task ResetDailyLimitsAsync()
        {
            await _context.UserSettings
                .ExecuteUpdateAsync(setters =>
                    setters
                        .SetProperty(x => x.SearchesUsedToday, 0)
                        .SetProperty(x => x.OperationsUsedToday, 0));
        }

        public async Task ResetOperationsUsedTodayAsync(int userId)
        {
            var settings = await _context.UserSettings.FirstOrDefaultAsync(x => x.UserId == userId);

            if (settings == null)
                return;

            settings.OperationsUsedToday = 0;
        }
    }
}
