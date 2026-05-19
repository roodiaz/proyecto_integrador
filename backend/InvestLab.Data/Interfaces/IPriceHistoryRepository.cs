using InvestLab.Models;

namespace InvestLab.Data.Interfaces;

public interface IPriceHistoryRepository
{
    Task<bool> ExistsAsync(string symbol);
    Task InsertManyAsync(List<PriceHistory> history);
}