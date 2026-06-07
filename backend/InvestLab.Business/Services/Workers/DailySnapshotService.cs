using InvestLab.Business.Interfaces;
using InvestLab.Business.Interfaces.Workers;
using InvestLab.Data.Interfaces;
using InvestLab.Data.Repositories;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models.Documents;
using Microsoft.Extensions.Logging;

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

    private readonly IMarketPriceService _marketPriceService;
    private readonly IExternalProvider _externalProvider;
    private readonly IUnitOfWork _unitOfWork;

    private readonly ILogger<DailySnapshotService> _logger;

    /// <summary>
    /// Inicializa una nueva instancia del servicio de snapshots diarios,
    /// inyectando los repositorios y servicios necesarios para su funcionamiento.
    /// </summary>
    /// <param name="userRepository">Repositorio de usuarios.</param>
    /// <param name="portfolioRepository">Repositorio de portfolios.</param>
    /// <param name="portfolioHistoryRepository">Repositorio de históricos de portfolio.</param>
    /// <param name="externalProvider">Proveedor externo de datos de mercado.</param>
    /// <param name="logger">Logger utilizado para registrar la actividad del servicio.</param>
    /// <param name="priceHistoryRepository">Repositorio de históricos de precios.</param>
    /// <param name="marketPriceService">Servicio encargado de obtener precios de mercado.</param>
    /// <param name="assetRepository">Repositorio de activos.</param>
    /// <param name="marketMetadataRepository">Repositorio de metadata de mercado.</param>
    /// <param name="unitOfWork">Unidad de trabajo utilizada para confirmar los cambios en la base de datos.</param>
    public DailySnapshotService(IUserRepository userRepository, IPortfolioRepository portfolioRepository, IPortfolioHistoryRepository portfolioHistoryRepository, IExternalProvider externalProvider, ILogger<DailySnapshotService> logger, IPriceHistoryRepository priceHistoryRepository, IMarketPriceService marketPriceService, IAssetRepository assetRepository, IMarketMetadataRepository marketMetadataRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _portfolioRepository = portfolioRepository;
        _portfolioHistoryRepository = portfolioHistoryRepository;
        _externalProvider = externalProvider;
        _logger = logger;
        _priceHistoryRepository = priceHistoryRepository;
        _marketPriceService = marketPriceService;
        _assetRepository = assetRepository;
        _marketMetadataRepository = marketMetadataRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Genera el snapshot diario del valor de portfolio para cada usuario activo.
    ///
    /// Por cada usuario, calcula el valor actual de sus posiciones abiertas
    /// utilizando los precios obtenidos del servicio de precios de mercado,
    /// suma el saldo disponible y guarda un registro histórico del valor total,
    /// evitando duplicar el snapshot si ya existe uno para el día actual.
    /// </summary>
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
    public async Task GenerateDailyPortfolioSnapshotsAsync()
    {
        // Obtiene todos los usuarios activos del sistema
        var users = await _userRepository.GetAllAsync();

        foreach (var user in users)
        {
            try
            {
                // Evita generar más de un snapshot por día para el mismo usuario
                var exists = await _portfolioHistoryRepository.ExistsByDateAsync(user.Id, DateTime.UtcNow.Date);

                if (exists)
                {
                    _logger.LogInformation("Snapshot ya existe para UserId={UserId}", user.Id);
                    continue;
                }

                // Obtiene las posiciones abiertas del usuario
                var portfolio = await _portfolioRepository.GetByUserAsync(user.Id);

                // Obtiene los símbolos necesarios para calcular el valor actual
                var symbols = portfolio
                    .Select(x => x.Asset.Symbol)
                    .Distinct()
                    .ToList();

                // Obtiene precios desde Mongo y realiza fallback al proveedor externo
                // cuando no existe información histórica
                var pricesBySymbol = await _marketPriceService.GetHistoricalPricesAsync(symbols);

                decimal holdingsValue = 0;

                foreach (var item in portfolio)
                {
                    // Si no existe precio disponible para el símbolo,
                    // se omite del cálculo
                    if (!pricesBySymbol.TryGetValue(item.Asset.Symbol, out var price))
                    {
                        _logger.LogWarning("Sin precio disponible para {Symbol}", item.Asset.Symbol);
                        continue;
                    }

                    // Calcula el valor actual de la posición
                    holdingsValue += item.Quantity * price;
                }

                // Valor total = efectivo disponible + valor de posiciones abiertas
                var totalValue = user.Balance + holdingsValue;

                // Guarda una foto diaria del portfolio para gráficos históricos
                var history = new PortfolioHistory
                {
                    UserId = user.Id,
                    Date = DateTime.UtcNow,
                    TotalValue = Math.Round(totalValue, 2)
                };

                await _portfolioHistoryRepository.InsertAsync(history);

                _logger.LogInformation("Snapshot portfolio generado: UserId={UserId} Valor={TotalValue}", user.Id, totalValue);
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
                var historical = await _externalProvider.GetHistoricalAsync(asset.Symbol, today.AddDays(-1), today);

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