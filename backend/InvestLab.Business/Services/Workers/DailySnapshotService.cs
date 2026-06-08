using InvestLab.Business.Interfaces;
using InvestLab.Business.Interfaces.Workers;
using InvestLab.Data.Interfaces;
using InvestLab.Data.Repositories;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models.Documents;
using Microsoft.Extensions.Logging;
using static InvestLab.Models.Enums;

namespace InvestLab.Business.Services.Workers;

public class DailySnapshotService : IDailySnapshotWorker
{
    // Repositorios
    private readonly IUserRepository _userRepository;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IPortfolioHistoryRepository _portfolioHistoryRepository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IMarketMetadataRepository _marketMetadataRepository;
    private readonly ITransactionRepository _transactionRepository;

    private readonly IMarketPriceService _marketPriceService;
    private readonly IMarketProviderResolver _providerResolver;
    private readonly IUnitOfWork _unitOfWork;

    private readonly ILogger<DailySnapshotService> _logger;

    /// <summary>
    /// Inicializa una nueva instancia del servicio de snapshots diarios,
    /// inyectando los repositorios y servicios necesarios para su funcionamiento.
    /// </summary>
    public DailySnapshotService(
        IUserRepository userRepository,
        IPortfolioRepository portfolioRepository,
        IPortfolioHistoryRepository portfolioHistoryRepository,
        IMarketProviderResolver providerResolver,
        ILogger<DailySnapshotService> logger,
        IPriceHistoryRepository priceHistoryRepository,
        IMarketPriceService marketPriceService,
        IAssetRepository assetRepository,
        IMarketMetadataRepository marketMetadataRepository,
        IUnitOfWork unitOfWork,
        ITransactionRepository transactionRepository)
    {
        _userRepository = userRepository;
        _portfolioRepository = portfolioRepository;
        _portfolioHistoryRepository = portfolioHistoryRepository;
        _providerResolver = providerResolver;
        _logger = logger;
        _priceHistoryRepository = priceHistoryRepository;
        _marketPriceService = marketPriceService;
        _assetRepository = assetRepository;
        _marketMetadataRepository = marketMetadataRepository;
        _unitOfWork = unitOfWork;
        _transactionRepository = transactionRepository;
    }

    /// <summary>
    /// Genera el snapshot diario del valor de portfolio para cada usuario activo.
    ///
    /// Recibe el momento exacto del cierre bursátil (16:05 NY en UTC) para:
    /// - Identificar la fecha del snapshot de forma precisa e independiente del reloj actual.
    /// - Reconstruir el estado del portfolio al cierre cuando el worker se ejecuta de forma
    ///   retroactiva (catch-up): las operaciones realizadas después del cierre se revierten
    ///   para reflejar el estado real al momento del cierre del mercado.
    ///
    /// El snapshot es idempotente: si ya existe uno para la fecha indicada, se omite.
    /// </summary>
    /// <param name="marketCloseUtc">Fecha/hora UTC del cierre del mercado (16:05 NY convertido a UTC).</param>
    public async Task GenerateDailyPortfolioSnapshotsAsync(DateTime marketCloseUtc)
    {
        var users = await _userRepository.GetAllAsync();
        var snapshotDate = marketCloseUtc.Date;

        foreach (var user in users)
        {
            try
            {
                // Evita generar más de un snapshot por día para el mismo usuario
                var exists = await _portfolioHistoryRepository.ExistsByDateAsync(user.Id, snapshotDate);
                if (exists)
                {
                    _logger.LogInformation("Snapshot ya existe para UserId={UserId} Fecha={Date}", user.Id, snapshotDate);
                    continue;
                }

                // Obtiene transacciones posteriores al cierre para reconstruir el estado al momento del mercado.
                // En una ejecución normal (16:05 NY) esta lista estará vacía.
                // En un catch-up (ej: worker reiniciado a las 20:00) puede contener operaciones post-cierre
                // que deben excluirse del snapshot del día.
                var postCloseTransactions = await _transactionRepository.GetByUserAfterDateAsync(user.Id, marketCloseUtc);

                // Reconstruye el balance al cierre revirtiendo las operaciones post-cierre
                var balanceAtClose = user.Balance;
                foreach (var tx in postCloseTransactions)
                {
                    if (tx.Type == TransactionType.Buy)  balanceAtClose += tx.Total;
                    if (tx.Type == TransactionType.Sell) balanceAtClose -= tx.Total;
                }

                // Obtiene las posiciones actuales y calcula el ajuste de cantidad por activo
                var portfolio = await _portfolioRepository.GetByUserAsync(user.Id);
                var symbols = portfolio.Select(x => x.Asset.Symbol).Distinct().ToList();
                var pricesBySymbol = await _marketPriceService.GetHistoricalPricesAsync(symbols);

                // Calcula delta de cantidad por activo derivado de operaciones post-cierre
                var quantityDelta = postCloseTransactions
                    .GroupBy(tx => tx.AssetId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(tx => tx.Type == TransactionType.Buy ? -tx.Quantity : tx.Quantity)
                    );

                decimal holdingsValue = 0;
                foreach (var item in portfolio)
                {
                    if (!pricesBySymbol.TryGetValue(item.Asset.Symbol, out var price))
                    {
                        _logger.LogWarning("Sin precio disponible para {Symbol}", item.Asset.Symbol);
                        continue;
                    }

                    var delta = quantityDelta.TryGetValue(item.AssetId, out var d) ? d : 0;
                    var quantityAtClose = item.Quantity + delta;
                    if (quantityAtClose <= 0) continue;

                    holdingsValue += quantityAtClose * price;
                }

                var totalValue = balanceAtClose + holdingsValue;

                var history = new PortfolioHistory
                {
                    UserId = user.Id,
                    Date = snapshotDate,
                    TotalValue = Math.Round(totalValue, 2)
                };

                await _portfolioHistoryRepository.InsertAsync(history);

                _logger.LogInformation("Snapshot portfolio generado: UserId={UserId} Fecha={Date} Valor={TotalValue}", user.Id, snapshotDate, totalValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando snapshot portfolio: UserId={UserId}", user.Id);
            }
        }
    }


    /// <summary>
    /// Guarda el cierre diario oficial del mercado.
    ///
    /// Debe ejecutarse una vez por día luego del cierre bursátil.
    ///
    /// Responsabilidades:
    /// - Obtener el cierre del día.
    /// - Guardar la vela diaria.
    /// - Actualizar la fecha global de mercado.
    ///
    /// NO recupera históricos faltantes.
    /// NO carga activos nuevos.
    /// </summary>
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
    public async Task SaveDailyMarketHistoryAsync()
    {
        _logger.LogInformation("Iniciando snapshot diario de mercado");

        var assets = await _assetRepository.GetAllAsync();
        var insertedCount = 0;

        foreach (var asset in assets)
        {
            try
            {
                _logger.LogInformation("Procesando cierre diario de {Symbol}", asset.Symbol);
                var today = DateTime.UtcNow.Date;

                // Obtiene únicamente la información
                // correspondiente al último día bursátil.
                var historical = await _providerResolver.GetProvider().GetHistoricalAsync(asset.Symbol, today.AddDays(-1), today);

                if (!historical.Any())
                {
                    _logger.LogWarning("Sin datos diarios para {Symbol}", asset.Symbol);
                    continue;
                }

                var lastCandle = historical.OrderByDescending(x => x.Date).First();

                // Evita insertar duplicados.
                var latestStoredDate = await _priceHistoryRepository.GetLatestDateAsync(asset.Symbol);
                if (latestStoredDate?.Date == lastCandle.Date.Date)
                {
                    _logger.LogInformation("El cierre diario ya existe para {Symbol}", asset.Symbol);
                    continue;
                }

                await _priceHistoryRepository.InsertAsync(new PriceHistory
                {
                    Symbol = asset.Symbol,
                    Date = lastCandle.Date.Date,
                    Open = lastCandle.Open,
                    High = lastCandle.High,
                    Low = lastCandle.Low,
                    Close = lastCandle.Close,
                    Volume = lastCandle.Volume
                });

                insertedCount++;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando cierre diario para {Symbol}", asset.Symbol);
            }
        }

        // Guarda la última fecha bursátil encontrada.
        await UpdateMarketMetadataAsync();

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Snapshot diario finalizado. Insertados={Count}", insertedCount);
    }

    /// <summary>
    /// Actualiza la metadata global de mercado
    /// utilizando la última fecha disponible en Mongo.
    ///
    /// Si no existen históricos cargados, no realiza ninguna acción.
    /// </summary>
    ///
    /// /// IMPORTANTE:
    /// La metadata se calcula a partir de Mongo,
    /// que es la fuente de verdad del sistema.
    /// No se consulta el proveedor externo.
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
    private async Task UpdateMarketMetadataAsync()
    {
        var latestMarketDate = await _priceHistoryRepository.GetLatestDateAsync();

        if (!latestMarketDate.HasValue)
            return;

        await _marketMetadataRepository.UpdateLastCloseAsync(latestMarketDate.Value);

        _logger.LogInformation("Metadata de mercado actualizada. Última fecha: {Date}", latestMarketDate.Value);
    }
}
