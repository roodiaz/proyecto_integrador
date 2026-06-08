namespace InvestLab.Business.Interfaces.Workers
{
    public interface IDailySnapshotWorker
    {
        Task GenerateDailyPortfolioSnapshotsAsync(DateTime marketCloseUtc);

        Task SaveDailyMarketHistoryAsync();
    }
}
