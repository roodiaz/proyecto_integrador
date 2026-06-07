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

    /// <summary>
    /// Inicializa una nueva instancia del servicio de históricos de mercado,
    /// inyectando los repositorios y servicios necesarios para su funcionamiento.
    /// </summary>
    /// <param name="repository">Repositorio de históricos de precios.</param>
    /// <param name="externalProvider">Proveedor externo de datos de mercado.</param>
    /// <param name="assetRepository">Repositorio de activos.</param>
    /// <param name="unitOfWork">Unidad de trabajo utilizada para persistir los cambios.</param>
    /// <param name="logger">Logger utilizado para registrar la actividad del servicio.</param>
    /// <param name="marketMetadataRepository">Repositorio de metadata de mercado.</param>
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
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
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
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
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
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
    private async Task RecoverMissingHistoryAsync()
    {
        var assets = await _assetRepository.GetAllAsync();

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
                var toDate = DateTime.UtcNow.Date;

                if (fromDate >= toDate)
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