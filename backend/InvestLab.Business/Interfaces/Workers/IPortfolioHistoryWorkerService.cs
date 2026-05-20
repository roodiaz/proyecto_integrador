namespace InvestLab.Business.Interfaces.Workers
{
    public interface IPortfolioHistoryWorkerService
    {
        Task GenerateDailySnapshotsAsync();
    }
}
