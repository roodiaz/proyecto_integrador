using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data.Repositories
{
    internal class UserPortfolioRepository : IUserPortfolioRepository
    {
        private readonly InvestLabDbContext _context;

        public UserPortfolioRepository(InvestLabDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserPortfolio>> GetByUserAsync(int userId)
        {
            return await _context.UserPortfolios
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<UserPortfolio?> GetByIdAsync(int portfolioId)
        {
            return await _context.UserPortfolios.FirstOrDefaultAsync(x => x.Id == portfolioId);
        }

        public async Task<UserPortfolio?> GetByIdAndUserAsync(int portfolioId, int userId)
        {
            return await _context.UserPortfolios
                .FirstOrDefaultAsync(x => x.Id == portfolioId && x.UserId == userId);
        }

        public async Task<UserPortfolio?> GetActiveByUserAsync(int userId)
        {
            return await _context.UserPortfolios
                .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        }

        public async Task<int> CountByUserAsync(int userId)
        {
            return await _context.UserPortfolios.CountAsync(x => x.UserId == userId);
        }

        public async Task InsertAsync(UserPortfolio portfolio)
        {
            await _context.UserPortfolios.AddAsync(portfolio);
        }

        public Task UpdateAsync(UserPortfolio portfolio)
        {
            _context.UserPortfolios.Update(portfolio);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(UserPortfolio portfolio)
        {
            _context.UserPortfolios.Remove(portfolio);

            return Task.CompletedTask;
        }

        public async Task<List<UserPortfolio>> GetAllAsync()
        {
            return await _context.UserPortfolios.ToListAsync();
        }

        public async Task SetActiveAsync(int userId, int portfolioId)
        {
            var portfolios = await _context.UserPortfolios
                .Where(x => x.UserId == userId)
                .ToListAsync();

            foreach (var portfolio in portfolios)
            {
                portfolio.IsActive = portfolio.Id == portfolioId;
            }

            await _context.SaveChangesAsync();
        }
    }
}
