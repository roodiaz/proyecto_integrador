using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data.Repositories
{
    public class RefreshTokenRepository: IRefreshTokenRepository
    {
        private readonly InvestLabDbContext _context;

        public RefreshTokenRepository(InvestLabDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == token);
        }

        public async Task AddAsync(RefreshToken token)
        {
            await _context.RefreshTokens.AddAsync(token);
        }

        public async Task DeleteByUserIdAsync(int userId)
        {
            await _context.RefreshTokens.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        }
    }
}
