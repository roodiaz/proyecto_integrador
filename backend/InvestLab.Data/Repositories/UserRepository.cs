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

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task<User?> GetByIdWithSettingsAsync(int userId)
        {
            return await _context.Users
                .Include(x => x.UserSetting)
                .FirstOrDefaultAsync(x => x.Id == userId);
        }

        public async Task<User?> GetByIdAsync(int userId)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users.Where(x => x.IsActive).ToListAsync();
        }

        public async Task UpdateBalanceAsync(int userId, decimal balance)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
                return;

            user.Balance = balance;
            user.UpdateAt = DateTime.UtcNow;
        }
    }
}
