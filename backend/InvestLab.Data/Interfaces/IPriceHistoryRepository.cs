namespace InvestLab.Data.Interfaces;

public interface IPriceHistoryRepository
{
    Task<bool> ExistsAsync(string symbol);

    Task<bool> ExistsByDateAsync(string symbol, DateTime date);

    Task<DateTime?> GetLatestDateAsync(string symbol);

    Task InsertAsync(PriceHistory history);

    Task InsertManyAsync(List<PriceHistory> history);
}