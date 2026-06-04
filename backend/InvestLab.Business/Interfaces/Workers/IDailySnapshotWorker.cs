namespace InvestLab.Business.Interfaces.Workers
{
    public interface IDailySnapshotWorker
    {
        Task GenerateDailySnapshotsAsync();

        Task SaveDailyMarketHistoryAsync();
    }
}
