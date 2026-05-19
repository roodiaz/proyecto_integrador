using InvestLab.Data.Interfaces;
using InvestLab.Models;
using MongoDB.Driver;

namespace InvestLab.Data.Repositories;

public class PriceHistoryRepository : IPriceHistoryRepository
{
    private readonly IMongoCollection<PriceHistory> _collection;

    public PriceHistoryRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<PriceHistory>("price_history");
    }

    public async Task<bool> ExistsAsync(string symbol)
    {
        return await _collection
            .Find(x => x.Symbol == symbol)
            .AnyAsync();
    }

    public async Task InsertManyAsync(List<PriceHistory> history)
    {
        if (!history.Any())
            return;

        await _collection.InsertManyAsync(history);
    }
}