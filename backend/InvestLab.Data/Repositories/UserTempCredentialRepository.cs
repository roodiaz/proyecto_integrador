using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data.Repositories
{
    public class UserTempCredentialRepository : IUserTempCredentialRepository
    {
        private readonly InvestLabDbContext _context;

        public UserTempCredentialRepository(InvestLabDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(UserTempCredential credential)
        {
            await _context.UserTempCredentials.AddAsync(credential);
        }

        public async Task<List<UserTempCredential>> GetActiveByUserIdAsync(int userId)
        {
            return await _context.UserTempCredentials
                .Where(x => x.UserId == userId && !x.IsUsed)
                .ToListAsync();
        }

        public async Task<UserTempCredential?> GetLatestAsync(int userId)
        {
            return await _context.UserTempCredentials
                .Where(x => x.UserId == userId && !x.IsUsed)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
