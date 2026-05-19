using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly InvestLabDbContext _context;

        public UserRepository(InvestLabDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdWithSettingsAsync(int userId)
        {
            return await _context.Users
                .Include(x => x.UserSetting)
                .FirstOrDefaultAsync(x => x.Id == userId);
        }

        public async Task<User?> GetByIdAsync(int userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.Id == userId);
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
