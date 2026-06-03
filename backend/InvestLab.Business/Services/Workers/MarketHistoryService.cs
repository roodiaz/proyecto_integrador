using InvestLab.Business.Interfaces.Workers;
using InvestLab.Data.Interfaces;
using InvestLab.Integrations.Interfaces;
using Microsoft.Extensions.Logging;

namespace InvestLab.Business.Services.Workers;

/// <summary>
/// Servicio encargado de almacenar históricos
/// diarios de mercado en MongoDB.
/// </summary>
public class MarketHistoryService : IMarketHistoryService
{
    private readonly ILogger<MarketHistoryService> _logger;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly IExternalProvider _externalProvider;
    private readonly IAssetRepository _assetRepository;
    private readonly IMarketMetadataRepository _marketMetadataRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarketHistoryService(IPriceHistoryRepository repository, IExternalProvider externalProvider, IAssetRepository assetRepository, IUnitOfWork unitOfWork, ILogger<MarketHistoryService> logger, IMarketMetadataRepository marketMetadataRepository)
    {
        _priceHistoryRepository = repository;
        _externalProvider = externalProvider;
        _assetRepository = assetRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _marketMetadataRepository = marketMetadataRepository;
    }

    /// <summary>
    /// Worker de mantenimiento histórico.
    ///
    /// Responsabilidades:
    /// - Detectar activos nuevos y cargar 1 año de histórico.
    /// - Recuperar huecos históricos de activos existentes.
    /// - Inicializar la metadata de mercado la primera vez.
    ///
    /// NO guarda el cierre diario del mercado.
    /// </summary>
    public async Task SeedMissingHistoryAsync()
    {
        _logger.LogInformation("Iniciando mantenimiento de históricos");

        await LoadNewAssetsHistoryAsync();

        await RecoverMissingHistoryAsync();

        await UpdateMarketMetadataAsync();

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Finalizó mantenimiento de históricos");
    }

    /// <summary>
    /// Carga un año de histórico para los activos
    /// que todavía no fueron inicializados.
    /// </summary>
    private async Task LoadNewAssetsHistoryAsync()
    {
        var pendingAssets = await _assetRepository.GetPendingHistoryAsync();

        _logger.LogInformation("Activos pendientes encontrados: {Count}", pendingAssets.Count);

        foreach (var asset in pendingAssets)
        {
            try
            {
                _logger.LogInformation("Cargando histórico inicial para {Symbol}", asset.Symbol);

                var historical = await _externalProvider.GetHistoricalAsync(asset.Symbol, DateTime.UtcNow.AddYears(-1), DateTime.UtcNow.Date.AddDays(-1));
                if (!historical.Any())
                {
                    _logger.LogWarning("No se encontraron históricos para {Symbol}", asset.Symbol);
                    continue;
                }

                var history = historical
                    .Select(x => new PriceHistory
                    {
                        Symbol = asset.Symbol,
                        Date = x.Date.Date,
                        Open = x.Open,
                        High = x.High,
                        Low = x.Low,
                        Close = x.Close,
                        Volume = x.Volume
                    })
                    .ToList();

                await _priceHistoryRepository.InsertManyAsync(history);

                asset.HistoryLoaded = true;

                await _assetRepository.UpdateAsync(asset);

                _logger.LogInformation("Histórico inicial cargado para {Symbol}. Registros: {Count}", asset.Symbol, history.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando histórico inicial para {Symbol}", asset.Symbol);
            }
        }
    }

    /// <summary>
    /// Recupera históricos faltantes de activos ya existentes.
    ///
    /// Nunca procesa el día actual.
    /// El cierre del día actual es responsabilidad
    /// exclusiva del DailyMarketWorker.
    /// </summary>
    private async Task RecoverMissingHistoryAsync()
    {
        var assets = await _assetRepository.GetAllSymbolsAsync();

        foreach (var asset in assets)
        {
            try
            {
                // Obtiene la última fecha almacenada.
                var lastDate = await _priceHistoryRepository.GetLatestDateAsync(asset.Symbol);

                // Si no tiene histórico, será procesado
                // por LoadNewAssetsHistoryAsync().
                if (!lastDate.HasValue)
                    continue;

                var fromDate = lastDate.Value.Date.AddDays(1);

                // Nunca recuperamos el día actual.
                var toDate = DateTime.UtcNow.Date.AddDays(-1);

                if (fromDate > toDate)
                    continue;

                _logger.LogInformation("Recuperando históricos para {Symbol}. Desde {From} hasta {To}", asset.Symbol, fromDate, toDate);

                var historical = await _externalProvider.GetHistoricalAsync(asset.Symbol, fromDate, toDate);

                if (!historical.Any())
                    continue;

                var history = historical
                    .Select(x => new PriceHistory
                    {
                        Symbol = asset.Symbol,
                        Date = x.Date.Date,
                        Open = x.Open,
                        High = x.High,
                        Low = x.Low,
                        Close = x.Close,
                        Volume = x.Volume
                    })
                    .ToList();

                await _priceHistoryRepository.InsertManyAsync(history);

                _logger.LogInformation("Recuperados {Count} registros para {Symbol}", history.Count, asset.Symbol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recuperando históricos para {Symbol}", asset.Symbol);
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
    public async Task SaveDailyMarketHistoryAsync()
    {
        _logger.LogInformation("Iniciando snapshot diario de mercado");

        var assets = await _assetRepository.GetAllSymbolsAsync();
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
    private async Task UpdateMarketMetadataAsync()
    {
        var latestMarketDate = await _priceHistoryRepository.GetLatestDateAsync();

        if (!latestMarketDate.HasValue)
            return;

        await _marketMetadataRepository.UpdateLastCloseAsync(latestMarketDate.Value);

        _logger.LogInformation("Metadata de mercado actualizada. Última fecha: {Date}", latestMarketDate.Value);
    }
}