using InvestLab.Business.Interfaces.Workers;
using InvestLab.Data.Interfaces;

namespace InvestLab.Business.Services.Workers;

/// <summary>
/// Servicio encargado de eliminar
/// históricos viejos de mercado.
/// </summary>
public class MarketHistoryCleanupService: IMarketHistoryCleanupService
{
    private readonly IPriceHistoryRepository  _repository;

    public MarketHistoryCleanupService(  IPriceHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task CleanupOldHistoryAsync()
    {
        var limitDate = DateTime.UtcNow.AddYears(-1);

        await _repository .DeleteOlderThanAsync(limitDate);
    }
}