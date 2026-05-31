namespace InvestLab.Data.Interfaces;

public interface IPriceHistoryRepository
{
    Task<bool> ExistsAsync(string symbol);

    Task<bool> ExistsByDateAsync(string symbol, DateTime date);

    Task<DateTime?> GetLatestDateAsync(string symbol);

    Task InsertAsync(PriceHistory history);

    Task InsertManyAsync(List<PriceHistory> history);

    Task DeleteOlderThanAsync(DateTime date);

    Task<List<PriceHistory>> GetBySymbolAndDateAsync(string symbol, DateTime fromDate);
}