using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;

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
    }
}
