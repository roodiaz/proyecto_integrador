using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using InvestLab.Models.Documents;
using MongoDB.Driver;

namespace InvestLab.Data.Repositories
{
    internal class PortfolioHistoryRepository : IPortfolioHistoryRepository
    {
        private readonly IMongoCollection<PortfolioHistory> _collection;

        public PortfolioHistoryRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<PortfolioHistory>("portfolio_history");
        }

        public async Task<List<PortfolioHistory>> GetByUserAndDateAsync(int userId, DateTime fromDate)
        {
            return await _collection
                .Find(x => x.UserId == userId && x.Date >= fromDate)
                .SortBy(x => x.Date)
                .ToListAsync();
        }

        public async Task InsertAsync(PortfolioHistory history)
        {
            await _collection.InsertOneAsync(history);
        }

        public async Task<PortfolioHistory?> GetLatestAsync(int userId)
        {
            return await _collection
                .Find(x => x.UserId == userId)
                .SortByDescending(x => x.Date)
                .FirstOrDefaultAsync();
        }

        public async Task<PortfolioHistory?> GetPreviousAsync(int userId)
        {
            return await _collection
                .Find(x => x.UserId == userId)
                .SortByDescending(x => x.Date)
                .Skip(1)
                .FirstOrDefaultAsync();
        }
    }
}
