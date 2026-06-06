namespace InvestLab.Business.Interfaces.Workers
{
    public interface IDailySnapshotWorker
    {
        Task GenerateDailyPortfolioSnapshotsAsync();

        Task SaveDailyMarketHistoryAsync();
    }
}
