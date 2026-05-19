namespace InvestLab.Business.Interfaces.Workers;

public interface IMarketHistoryService
{
    Task SeedDefaultAssetsAsync();
    Task SaveDailyMarketHistoryAsync();
}