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

        public async Task<UserTempCredential?> GetByUserIdAsync(int userId)
        {
            return await _context.UserTempCredentials
                .Where(x => x.UserId == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<UserTempCredential?> GetLatestAsync(int userId)
        {
            return await _context.UserTempCredentials
                .Where(x => x.UserId == userId && !x.IsUsed)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task DeleteByUserIdAsync(int userId)
        {
            await _context.UserTempCredentials.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        }
    }
}
