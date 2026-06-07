using InvestLab.Business.Services.Workers;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models.DTOs.Market;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InvestLab.Tests.Business.Services.Workers;

/// <summary>
/// Pruebas unitarias de <see cref="MarketHistoryService"/>, cubriendo el mantenimiento de históricos de mercado:
/// la carga inicial de históricos para activos nuevos, la recuperación de huecos en activos existentes
/// y la actualización de la metadata global de mercado.
/// </summary>
public class MarketHistoryServiceTests
{
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepository = new();
    private readonly Mock<IExternalProvider> _externalProvider = new();
    private readonly Mock<IAssetRepository> _assetRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<MarketHistoryService>> _logger = new();
    private readonly Mock<IMarketMetadataRepository> _marketMetadataRepository = new();

    private MarketHistoryService CreateService() => new(_priceHistoryRepository.Object, _externalProvider.Object, _assetRepository.Object, _unitOfWork.Object, _logger.Object, _marketMetadataRepository.Object);

    private static Asset AssetEntity(int id = 1, string symbol = "AAPL", bool historyLoaded = false) => new() { Id = id, Symbol = symbol, HistoryLoaded = historyLoaded };

    private static HistoricalPriceDto Candle(DateTime date, decimal close = 150) => new() { Date = date, Open = close, High = close, Low = close, Close = close, Volume = 1000 };

    private void SetupEmptyMaintenance()
    {
        _assetRepository.Setup(r => r.GetPendingHistoryAsync()).ReturnsAsync(new List<Asset>());
        _assetRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Asset>());
        _priceHistoryRepository.Setup(r => r.GetLatestDateAsync()).ReturnsAsync((DateTime?)null);
    }

    // ---------- SeedMissingHistoryAsync: carga de activos nuevos ----------

    /// <summary>Verifica que, para un activo pendiente de carga, se obtenga un año de histórico, se inserte y se marque el activo como cargado.</summary>
    [Fact]
    public async Task SeedMissingHistoryAsync_WhenAssetIsPendingHistory_ShouldLoadOneYearOfHistoryAndMarkAsLoaded()
    {
        var asset = AssetEntity(historyLoaded: false);
        var today = DateTime.UtcNow.Date;

        _assetRepository.Setup(r => r.GetPendingHistoryAsync()).ReturnsAsync(new List<Asset> { asset });
        _assetRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Asset>());
        _externalProvider.Setup(p => p.GetHistoricalAsync("AAPL", It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new List<HistoricalPriceDto> { Candle(today.AddDays(-2)), Candle(today.AddDays(-1)) });
        _priceHistoryRepository.Setup(r => r.GetLatestDateAsync()).ReturnsAsync((DateTime?)null);

        await CreateService().SeedMissingHistoryAsync();

        _priceHistoryRepository.Verify(r => r.InsertManyAsync(It.Is<List<PriceHistory>>(l => l.Count == 2 && l.All(x => x.Symbol == "AAPL"))), Times.Once);
        Assert.True(asset.HistoryLoaded);
        _assetRepository.Verify(r => r.UpdateAsync(asset), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /// <summary>Verifica que, si no se obtienen datos históricos para un activo pendiente, no se inserten registros ni se lo marque como cargado.</summary>
    [Fact]
    public async Task SeedMissingHistoryAsync_WhenNoHistoricalDataForPendingAsset_ShouldNotInsertNorMarkAsLoaded()
    {
        var asset = AssetEntity(historyLoaded: false);

        _assetRepository.Setup(r => r.GetPendingHistoryAsync()).ReturnsAsync(new List<Asset> { asset });
        _assetRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Asset>());
        _externalProvider.Setup(p => p.GetHistoricalAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new List<HistoricalPriceDto>());
        _priceHistoryRepository.Setup(r => r.GetLatestDateAsync()).ReturnsAsync((DateTime?)null);

        await CreateService().SeedMissingHistoryAsync();

        _priceHistoryRepository.Verify(r => r.InsertManyAsync(It.IsAny<List<PriceHistory>>()), Times.Never);
        Assert.False(asset.HistoryLoaded);
        _assetRepository.Verify(r => r.UpdateAsync(It.IsAny<Asset>()), Times.Never);
    }

    /// <summary>Verifica que, si ocurre un error al cargar el histórico de un activo pendiente, se registre el error y se continúe con los demás activos.</summary>
    [Fact]
    public async Task SeedMissingHistoryAsync_WhenLoadingHistoryForOneAssetThrows_ShouldLogErrorAndContinueWithOthers()
    {
        var failingAsset = AssetEntity(1, "AAPL");
        var workingAsset = AssetEntity(2, "MSFT");
        var today = DateTime.UtcNow.Date;

        _assetRepository.Setup(r => r.GetPendingHistoryAsync()).ReturnsAsync(new List<Asset> { failingAsset, workingAsset });
        _assetRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Asset>());
        _externalProvider.Setup(p => p.GetHistoricalAsync("AAPL", It.IsAny<DateTime>(), It.IsAny<DateTime>())).ThrowsAsync(new Exception("provider error"));
        _externalProvider.Setup(p => p.GetHistoricalAsync("MSFT", It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new List<HistoricalPriceDto> { Candle(today.AddDays(-1)) });
        _priceHistoryRepository.Setup(r => r.GetLatestDateAsync()).ReturnsAsync((DateTime?)null);

        await CreateService().SeedMissingHistoryAsync();

        Assert.True(workingAsset.HistoryLoaded);
        _assetRepository.Verify(r => r.UpdateAsync(workingAsset), Times.Once);
        _assetRepository.Verify(r => r.UpdateAsync(failingAsset), Times.Never);
    }

    // ---------- SeedMissingHistoryAsync: recuperación de huecos ----------

    /// <summary>Verifica que, para un activo existente con históricos faltantes, se recupere el rango pendiente hasta el día anterior al actual.</summary>
    [Fact]
    public async Task SeedMissingHistoryAsync_WhenExistingAssetHasMissingHistory_ShouldRecoverGapUntilYesterday()
    {
        var asset = AssetEntity(historyLoaded: true);
        var today = DateTime.UtcNow.Date;
        var lastStoredDate = today.AddDays(-5);

        _assetRepository.Setup(r => r.GetPendingHistoryAsync()).ReturnsAsync(new List<Asset>());
        _assetRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Asset> { asset });
        _priceHistoryRepository.Setup(r => r.GetLatestDateAsync("AAPL")).ReturnsAsync(lastStoredDate);
        _priceHistoryRepository.Setup(r => r.GetLatestDateAsync()).ReturnsAsync((DateTime?)null);
        _externalProvider.Setup(p => p.GetHistoricalAsync("AAPL", lastStoredDate.AddDays(1), today)).ReturnsAsync(new List<HistoricalPriceDto> { Candle(today.AddDays(-3)), Candle(today.AddDays(-2)) });

        await CreateService().SeedMissingHistoryAsync();

        _priceHistoryRepository.Verify(r => r.InsertManyAsync(It.Is<List<PriceHistory>>(l => l.Count == 2 && l.All(x => x.Symbol == "AAPL"))), Times.Once);
    }

    /// <summary>Verifica que, si el activo existente no tiene huecos pendientes (ya tiene el histórico hasta el día anterior), no se consulte al proveedor externo.</summary>
    [Fact]
    public async Task SeedMissingHistoryAsync_WhenExistingAssetHasNoGap_ShouldNotQueryExternalProvider()
    {
        var asset = AssetEntity(historyLoaded: true);
        var today = DateTime.UtcNow.Date;

        _assetRepository.Setup(r => r.GetPendingHistoryAsync()).ReturnsAsync(new List<Asset>());
        _assetRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Asset> { asset });
        _priceHistoryRepository.Setup(r => r.GetLatestDateAsync("AAPL")).ReturnsAsync(today.AddDays(-1));
        _priceHistoryRepository.Setup(r => r.GetLatestDateAsync()).ReturnsAsync((DateTime?)null);

        await CreateService().SeedMissingHistoryAsync();

        _externalProvider.Verify(p => p.GetHistoricalAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        _priceHistoryRepository.Verify(r => r.InsertManyAsync(It.IsAny<List<PriceHistory>>()), Times.Never);
    }

    /// <summary>Verifica que, si el activo existente todavía no tiene ningún histórico almacenado, se omita de la recuperación de huecos (queda a cargo de la carga inicial).</summary>
    [Fact]
    public async Task SeedMissingHistoryAsync_WhenExistingAssetHasNoStoredHistory_ShouldSkipGapRecovery()
    {
        var asset = AssetEntity(historyLoaded: true);

        _assetRepository.Setup(r => r.GetPendingHistoryAsync()).ReturnsAsync(new List<Asset>());
        _assetRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Asset> { asset });
        _priceHistoryRepository.Setup(r => r.GetLatestDateAsync("AAPL")).ReturnsAsync((DateTime?)null);
        _priceHistoryRepository.Setup(r => r.GetLatestDateAsync()).ReturnsAsync((DateTime?)null);

        await CreateService().SeedMissingHistoryAsync();

        _externalProvider.Verify(p => p.GetHistoricalAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
    }

    // ---------- SeedMissingHistoryAsync: metadata de mercado ----------

    /// <summary>Verifica que, cuando existe una fecha histórica almacenada, se actualice la metadata global de mercado con dicha fecha.</summary>
    [Fact]
    public async Task SeedMissingHistoryAsync_WhenLatestStoredDateExists_ShouldUpdateMarketMetadata()
    {
        var latestDate = DateTime.UtcNow.Date.AddDays(-1);
        SetupEmptyMaintenance();
        _priceHistoryRepository.Setup(r => r.GetLatestDateAsync()).ReturnsAsync(latestDate);

        await CreateService().SeedMissingHistoryAsync();

        _marketMetadataRepository.Verify(r => r.UpdateLastCloseAsync(latestDate), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /// <summary>Verifica que, cuando no existe ninguna fecha histórica almacenada, no se actualice la metadata global de mercado.</summary>
    [Fact]
    public async Task SeedMissingHistoryAsync_WhenNoStoredDateExists_ShouldNotUpdateMarketMetadata()
    {
        SetupEmptyMaintenance();

        await CreateService().SeedMissingHistoryAsync();

        _marketMetadataRepository.Verify(r => r.UpdateLastCloseAsync(It.IsAny<DateTime>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
