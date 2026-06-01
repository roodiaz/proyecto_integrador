using InvestLab.Business.Interfaces.Workers;
using InvestLab.Data.Interfaces;
using InvestLab.Integrations.Interfaces;

namespace InvestLab.Business.Services.Workers;

/// <summary>
/// Servicio encargado de almacenar históricos
/// diarios de mercado en MongoDB.
/// </summary>
public class MarketHistoryService : IMarketHistoryService
{
    private readonly IPriceHistoryRepository _repository;
    private readonly IExternalProvider _externalProvider;
    private readonly IAssetRepository _assetRepository;

    public MarketHistoryService(IPriceHistoryRepository repository, IExternalProvider externalProvider, IAssetRepository assetRepository)
    {
        _repository = repository;
        _externalProvider = externalProvider;
        _assetRepository = assetRepository;
    }

    public async Task SeedMissingHistoryAsync()
    {
        var assets = await _assetRepository.GetPendingHistoryAsync();

        foreach (var asset in assets)
        {
            var historical =
                await _externalProvider.GetHistoricalAsync(
                    asset.Symbol,
                    DateTime.UtcNow.AddYears(-1),
                    DateTime.UtcNow);

            var history =
                historical.Select(c => new PriceHistory
                {
                    Symbol = asset.Symbol,
                    Date = c.Date,
                    Open = c.Open,
                    High = c.High,
                    Low = c.Low,
                    Close = c.Close,
                    Volume = c.Volume
                })
                .ToList();

            if (!history.Any())
                continue;

            await _repository.InsertManyAsync(history);

            asset.HistoryLoaded = true;
            asset.LastMarketUpdateAt = DateTime.UtcNow;

            await _assetRepository.UpdateAsync(asset);
        }
    }
    /// <summary>
    /// Guarda un snapshot diario de mercado
    /// luego del cierre bursátil.
    /// </summary>
    public async Task SaveDailyMarketHistoryAsync()
    {
        var assets = await _assetRepository.GetAllSymbolsAsync();

        foreach (var asset in assets)
        {
            var alreadyExists = await _repository.ExistsByDateAsync(asset.Symbol, DateTime.UtcNow.Date);

            if (alreadyExists)
                continue;

            var market = await _externalProvider.GetPriceAsync(asset.Symbol);
            if (market == null)
                continue;

            await _repository.InsertAsync(
                new PriceHistory
                {
                    Symbol = asset.Symbol,
                    Date = DateTime.UtcNow.Date,
                    Open = market.PreviousClose,
                    High = market.Price,
                    Low = market.PreviousClose,
                    Close = market.Price,
                    Volume = 0
                });

            asset.LastMarketUpdateAt =DateTime.UtcNow;

            await _assetRepository.UpdateAsync(asset);
        }
    }
}