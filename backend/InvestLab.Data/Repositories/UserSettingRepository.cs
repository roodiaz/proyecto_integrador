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
            return await _context.UserSettings
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }
    }
}
