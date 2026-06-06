using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using InvestLab.Models.DTOs.Favorite;
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

        public async Task<(List<Favorite>, int)> GetPagedAsync(int userId, FavoriteFilterDto filter)
        {
            var query = _context.Favorites.Include(x => x.Asset).Where(x => x.UserId == userId);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim().ToUpper();

                query = query.Where(x =>
                    x.Asset.Symbol.Contains(search) ||
                    x.Asset.Name.Contains(search));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.Asset.Symbol)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task DeleteByUserIdAsync(int userId)
        {
            await _context.Favorites.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        }
    }
}
