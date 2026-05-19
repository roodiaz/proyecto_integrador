using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data.Repositories
{
    public class FavoriteRepository : IFavoriteRepository
    {
        private readonly InvestLabDbContext _context;

        public FavoriteRepository(InvestLabDbContext context)
        {
            _context = context;
        }

        public async Task<List<Favorite>> GetByUserAsync(int userId)
        {
            return await _context.Favorites
                .Include(x => x.Asset)
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        public async Task<int> CountAsync(int userId)
        {
            return await _context.Favorites
                .CountAsync(x => x.UserId == userId);
        }

        public async Task<bool> ExistsAsync(int userId, int assetId)
        {
            return await _context.Favorites
                .AnyAsync(x => x.UserId == userId && x.AssetId == assetId);
        }

        public async Task AddAsync(Favorite favorite)
        {
            await _context.Favorites.AddAsync(favorite);
        }

        public async Task<Favorite?> GetByUserAndAssetAsync(int userId, int assetId)
        {
            return await _context.Favorites
                .FirstOrDefaultAsync(x => x.UserId == userId && x.AssetId == assetId);
        }

        public void Remove(Favorite favorite)
        {
            _context.Favorites.Remove(favorite);
        }

        public async Task<(List<Favorite> data, int total)> GetPagedAsync(int userId, int page, int pageSize)
        {
            var query = _context.Favorites
                .Include(x => x.Asset)
                .Where(x => x.UserId == userId);

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, total);
        }
    }
}
