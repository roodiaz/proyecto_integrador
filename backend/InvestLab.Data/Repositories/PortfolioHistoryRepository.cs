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
                .Find(x =>
                    x.UserId == userId &&
                    x.Date >= fromDate)
                .SortBy(x => x.Date)
                .ToListAsync();
        }
    }
}
