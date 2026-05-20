using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data.Repositories
{
    internal class PortfolioRepository : IPortfolioRepository
    {
        private readonly InvestLabDbContext _context;
        public PortfolioRepository(InvestLabDbContext context)
        {
            _context = context;
        }

        public async Task<Portfolio?> GetByUserAndAssetAsync(int userId, int assetId)
        {
            return await _context.Portfolios.FirstOrDefaultAsync(x => x.UserId == userId && x.AssetId == assetId);
        }

        public async Task InsertAsync(Portfolio portfolio)
        {
            await _context.Portfolios.AddAsync(portfolio);
        }

        public Task UpdateAsync(Portfolio portfolio)
        {
            _context.Portfolios.Update(portfolio);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Portfolio portfolio)
        {
            _context.Portfolios.Remove(portfolio);

            return Task.CompletedTask;
        }
    }
}
