namespace InvestLab.Business.Interfaces.Workers;

public interface IMarketHistoryService
{
    Task SeedMissingHistoryAsync();
    Task SaveDailyMarketHistoryAsync();
}