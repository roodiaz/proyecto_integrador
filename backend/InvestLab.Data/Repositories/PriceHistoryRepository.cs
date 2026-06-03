using InvestLab.Data.Interfaces;
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

    public async Task<bool> ExistsByDateAsync(string symbol, DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);

        return await _collection
            .Find(x => x.Symbol == symbol && x.Date >= start && x.Date < end)
            .AnyAsync();
    }

    public async Task<DateTime?> GetLatestDateAsync(string symbol)
    {
        var latest = await _collection
            .Find(x => x.Symbol == symbol)
            .SortByDescending(x => x.Date)
            .FirstOrDefaultAsync();

        return latest?.Date;
    }

    public async Task<DateTime?> GetLatestDateAsync()
    {
        var latest = await _collection
            .Find(_ => true)
            .SortByDescending(x => x.Date)
            .FirstOrDefaultAsync();

        return latest?.Date;
    }

    public async Task InsertAsync(PriceHistory history)
    {
        await _collection.InsertOneAsync(history);
    }

    public async Task InsertManyAsync(List<PriceHistory> history)
    {
        if (!history.Any())
            return;

        await _collection.InsertManyAsync(history);
    }

    public async Task DeleteOlderThanAsync(DateTime date)
    {
        await _collection.DeleteManyAsync(x => x.Date < date);
    }

    public async Task<List<PriceHistory>> GetBySymbolAndDateAsync(string symbol, DateTime fromDate)
    {
        // TODO: revisar fecha hay qe sacar
        return await _collection
            .Find(x => x.Symbol == symbol && x.Date >= fromDate)
            .SortBy(x => x.Date)
            .ToListAsync();
    }

    public async Task<List<PriceHistory>> GetLatestPricesAsync()
    {
        var pipeline = _collection.Aggregate()
            .SortByDescending(x => x.Date)
            .Group(
                x => x.Symbol,
                g => g.First());

        return await pipeline.ToListAsync();
    }
}