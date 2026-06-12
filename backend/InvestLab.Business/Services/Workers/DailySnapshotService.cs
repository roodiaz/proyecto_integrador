using InvestLab.Business.Interfaces;
using InvestLab.Business.Interfaces.Workers;
using InvestLab.Data;
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
    private readonly IUserPortfolioRepository _userPortfolioRepository;
    private readonly IPortfolioHoldingRepository _portfolioHoldingRepository;
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
        IUserPortfolioRepository userPortfolioRepository,
        IPortfolioHoldingRepository portfolioHoldingRepository,
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
        _userPortfolioRepository = userPortfolioRepository;
        _portfolioHoldingRepository = portfolioHoldingRepository;
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
        var portfolios = await _userPortfolioRepository.GetAllAsync();
        var snapshotDate = marketCloseUtc.Date;

        foreach (var portfolio in portfolios)
        {
            try
            {
                // Evita generar más de un snapshot por día para el mismo portfolio
                var exists = await _portfolioHistoryRepository.ExistsByDateAsync(portfolio.Id, snapshotDate);
                if (exists)
                {
                    _logger.LogInformation("Snapshot ya existe para PortfolioId={PortfolioId} Fecha={Date}", portfolio.Id, snapshotDate);
                    continue;
                }

                // Obtiene transacciones posteriores al cierre para reconstruir el estado al momento del mercado.
                // En una ejecución normal (16:05 NY) esta lista estará vacía.
                // En un catch-up (ej: worker reiniciado a las 20:00) puede contener operaciones post-cierre
                // que deben excluirse del snapshot del día.
                var postCloseTransactions = await _transactionRepository.GetByPortfolioAfterDateAsync(portfolio.Id, marketCloseUtc);

                // Reconstruye el balance al cierre revirtiendo las operaciones post-cierre
                var balanceAtClose = portfolio.CurrentBalance;
                foreach (var tx in postCloseTransactions)
                {
                    if (tx.Type == TransactionType.Buy)  balanceAtClose += tx.Total;
                    if (tx.Type == TransactionType.Sell) balanceAtClose -= tx.Total;
                }

                // Obtiene las posiciones actuales y calcula el ajuste de cantidad por activo
                var holdings = await _portfolioHoldingRepository.GetByPortfolioAsync(portfolio.Id);
                var symbols = holdings.Select(x => x.Asset.Symbol).Distinct().ToList();
                var pricesBySymbol = await _marketPriceService.GetHistoricalPricesAsync(symbols);

                // Calcula delta de cantidad por activo derivado de operaciones post-cierre
                var quantityDelta = postCloseTransactions
                    .GroupBy(tx => tx.AssetId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(tx => tx.Type == TransactionType.Buy ? -tx.Quantity : tx.Quantity)
                    );

                decimal holdingsValue = 0;
                foreach (var item in holdings)
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
                    UserId            = portfolio.UserId,
                    PortfolioId       = portfolio.Id,
                    Date              = snapshotDate,
                    AvailableBalance  = Math.Round(balanceAtClose, 2),
                    InvestedValue     = Math.Round(holdingsValue, 2),
                    TotalValue        = Math.Round(totalValue, 2)
                };

                await _portfolioHistoryRepository.InsertAsync(history);

                _logger.LogInformation("Snapshot portfolio generado: PortfolioId={PortfolioId} Fecha={Date} Valor={TotalValue}", portfolio.Id, snapshotDate, totalValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando snapshot portfolio: PortfolioId={PortfolioId}", portfolio.Id);
            }
        }
    }


    /// <summary>
    /// Garantiza que existan snapshots para todos los días bursátiles faltantes desde la
    /// creación de cada usuario (o desde su último snapshot) hasta <paramref name="lastCloseUtc"/>.
    ///
    /// Para reconstruir el estado histórico del portfolio en una fecha D pasada, el método
    /// invierte las operaciones realizadas después del fin del día D (>= D+1 UTC) sobre el
    /// estado actual del usuario. El precio de cada activo se obtiene del historial almacenado
    /// en la base de datos. Esto garantiza coherencia incluso cuando el worker estuvo apagado
    /// durante días o semanas.
    ///
    /// El método es idempotente: omite cualquier fecha para la que ya exista un snapshot.
    /// </summary>
    /// <param name="lastCloseUtc">
    /// Fecha/hora UTC del último cierre bursátil (límite superior de la recuperación).
    /// </param>
    public async Task RunHistoricalCatchUpAsync(DateTime lastCloseUtc)
    {
        var lastCloseDate = lastCloseUtc.Date;
        var portfolios = await _userPortfolioRepository.GetAllAsync();

        foreach (var portfolio in portfolios)
        {
            try
            {
                var latestSnapshot = await _portfolioHistoryRepository.GetLatestAsync(portfolio.Id);

                DateTime startDate;
                if (latestSnapshot == null)
                    startDate = portfolio.CreatedAt.Date;
                else
                    startDate = latestSnapshot.Date.Date.AddDays(1);

                var missingDays = GetBusinessDaysBetween(startDate, lastCloseDate);
                if (missingDays.Count == 0)
                {
                    _logger.LogInformation("Sin días bursátiles faltantes para PortfolioId={PortfolioId}", portfolio.Id);
                    continue;
                }

                _logger.LogInformation("Recuperando {Count} días para PortfolioId={PortfolioId} ({From} → {To})",
                    missingDays.Count, portfolio.Id, missingDays[0], missingDays[^1]);

                var currentHoldings = await _portfolioHoldingRepository.GetByPortfolioAsync(portfolio.Id);
                var allTransactions  = await _transactionRepository.GetAllByPortfolioAsync(portfolio.Id);

                foreach (var date in missingDays)
                {
                    var cutoffUtc = date.Date.AddDays(1); // transacciones de D+1 en adelante son "después de D"

                    var txAfterD = allTransactions.Where(tx => tx.CreatedAt >= cutoffUtc).ToList();

                    // Reconstruir balance al fin del día D
                    var balanceAtD = portfolio.CurrentBalance;
                    foreach (var tx in txAfterD)
                    {
                        if (tx.Type == TransactionType.Buy)  balanceAtD += tx.Total;
                        if (tx.Type == TransactionType.Sell) balanceAtD -= tx.Total;
                    }

                    // Reconstruir tenencias al fin del día D
                    // Clave: AssetId, Valor: (Symbol, Quantity)
                    var holdingsAtD = currentHoldings
                        .ToDictionary(p => p.AssetId, p => (p.Asset.Symbol, p.Quantity));

                    foreach (var tx in txAfterD)
                    {
                        if (!holdingsAtD.ContainsKey(tx.AssetId))
                            holdingsAtD[tx.AssetId] = (tx.Asset.Symbol, 0m);

                        var (sym, qty) = holdingsAtD[tx.AssetId];
                        holdingsAtD[tx.AssetId] = tx.Type == TransactionType.Buy
                            ? (sym, qty - tx.Quantity)   // deshacer compra
                            : (sym, qty + tx.Quantity);  // deshacer venta
                    }

                    // Valorizar tenencias con precios históricos
                    decimal holdingsValue = 0;
                    foreach (var (_, (symbol, quantity)) in holdingsAtD)
                    {
                        if (quantity <= 0) continue;

                        var price = await _priceHistoryRepository.GetClosingPriceOnOrBeforeAsync(symbol, date);
                        if (price == null)
                        {
                            _logger.LogWarning("Sin precio histórico para {Symbol} en {Date}", symbol, date);
                            continue;
                        }

                        holdingsValue += quantity * price.Value;
                    }

                    var totalValue = balanceAtD + holdingsValue;

                    await _portfolioHistoryRepository.InsertAsync(new PortfolioHistory
                    {
                        UserId           = portfolio.UserId,
                        PortfolioId      = portfolio.Id,
                        Date             = date,
                        AvailableBalance = Math.Round(balanceAtD, 2),
                        InvestedValue    = Math.Round(holdingsValue, 2),
                        TotalValue       = Math.Round(totalValue, 2)
                    });

                    _logger.LogInformation("Snapshot histórico generado: PortfolioId={PortfolioId} Fecha={Date} Valor={Total}",
                        portfolio.Id, date, totalValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en catch-up histórico para PortfolioId={PortfolioId}", portfolio.Id);
            }
        }
    }

    /// <summary>
    /// Completa el campo "portfolioId" en documentos históricos preexistentes que aún no lo
    /// poseen, asignando el portfolio activo (o el primero) de cada usuario.
    /// Operación idempotente: solo afecta documentos sin "portfolioId".
    /// </summary>
    public async Task BackfillPortfolioHistoryPortfolioIdsAsync()
    {
        var portfolios = await _userPortfolioRepository.GetAllAsync();

        var portfolioIdByUserId = portfolios
            .GroupBy(p => p.UserId)
            .ToDictionary(
                g => g.Key,
                g => (g.FirstOrDefault(p => p.IsActive) ?? g.First()).Id);

        if (portfolioIdByUserId.Count == 0)
            return;

        await _portfolioHistoryRepository.BackfillPortfolioIdsAsync(portfolioIdByUserId);
    }

    /// <summary>
    /// Devuelve la lista de días bursátiles (lunes–viernes) entre <paramref name="start"/>
    /// y <paramref name="end"/>, ambos inclusive.
    /// </summary>
    private static List<DateTime> GetBusinessDaysBetween(DateTime start, DateTime end)
    {
        var days = new List<DateTime>();
        var current = start.Date;
        while (current <= end.Date)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                days.Add(current);
            current = current.AddDays(1);
        }
        return days;
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

                // Pide from=ayer, to=mañana para que el endDate (tratado como exclusivo
                // por algunos proveedores) incluya siempre el cierre de hoy.
                var historical = await _providerResolver.GetProvider().GetHistoricalAsync(asset.Symbol, today.AddDays(-1), today.AddDays(1));

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
