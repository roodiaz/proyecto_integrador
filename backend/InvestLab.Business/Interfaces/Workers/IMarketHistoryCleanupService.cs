namespace InvestLab.Business.Interfaces.Workers;

public interface IMarketHistoryCleanupService
{
    Task CleanupOldHistoryAsync();
}