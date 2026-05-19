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

    private readonly string[] _defaultSymbols =
    [
        "^GSPC",
        "^IXIC",
        "AAPL",
        "MSFT",
        "AMZN",
        "GOOGL",
        "META",
        "TSLA",
        "NVDA"
    ];

    public MarketHistoryService(      IPriceHistoryRepository repository, IExternalProvider externalProvider)
    {
        _repository = repository;
        _externalProvider = externalProvider;
    }

    public async Task SeedDefaultAssetsAsync()
    {
        foreach (var symbol in _defaultSymbols)
        {
            var exists = await _repository
                .ExistsAsync(symbol);

            if (exists)
                continue;

            var candles = await _externalProvider
                .GetHistoricalAsync(
                    symbol,
                    DateTime.UtcNow.AddYears(-1),
                    DateTime.UtcNow);

            var history = candles.Select(c => new PriceHistory
            {
                Symbol = symbol,

                Date = c.Date,

                Open = c.Open,
                High = c.High,
                Low = c.Low,
                Close = c.Close,

                Volume = c.Volume
            }).ToList();

            await _repository.InsertManyAsync(history);
        }
    }

    /// <summary>
    /// Guarda un snapshot diario de mercado
    /// luego del cierre bursátil.
    /// </summary>
    public async Task SaveDailyMarketHistoryAsync()
    {
        foreach (var symbol in _defaultSymbols)
        {
            var alreadyExists = await _repository
                .ExistsByDateAsync(
                    symbol,
                    DateTime.UtcNow);

            if (alreadyExists)
                continue;

            var market = await _externalProvider
                .GetPriceAsync(symbol);

            if (market == null)
                continue;

            var history = new PriceHistory
            {
                Symbol = symbol,

                Date = DateTime.UtcNow.Date,

                Open = market.PreviousClose,
                High = market.Price,
                Low = market.PreviousClose,
                Close = market.Price,

                Volume = 0
            };

            await _repository.InsertAsync(history);
        }
    }
}