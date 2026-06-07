using InvestLab.Business.Interfaces;
using InvestLab.Business.Services.Workers;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models.DTOs.Market;
using InvestLab.Models.Documents;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InvestLab.Tests.Business.Services.Workers;

/// <summary>
/// Pruebas unitarias de <see cref="DailySnapshotService"/>, cubriendo la generación del snapshot diario
/// del valor de portfolio de los usuarios y el guardado del cierre diario oficial del mercado.
/// </summary>
public class DailySnapshotServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPortfolioRepository> _portfolioRepository = new();
    private readonly Mock<IPortfolioHistoryRepository> _portfolioHistoryRepository = new();
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepository = new();
    private readonly Mock<IAssetRepository> _assetRepository = new();
    private readonly Mock<IMarketMetadataRepository> _marketMetadataRepository = new();
    private readonly Mock<IMarketPriceService> _marketPriceService = new();
    private readonly Mock<IExternalProvider> _externalProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<DailySnapshotService>> _logger = new();

    private DailySnapshotService CreateService() => new(_userRepository.Object, _portfolioRepository.Object, _portfolioHistoryRepository.Object, _externalProvider.Object, _logger.Object, _priceHistoryRepository.Object, _marketPriceService.Object, _assetRepository.Object, _marketMetadataRepository.Object, _unitOfWork.Object);

    private static User UserEntity(int id = 1, decimal balance = 1000) => new() { Id = id, Username = "user", Email = "user@test.com", PasswordHash = "hash", Phone = "123", Balance = balance };

    private static Asset AssetEntity(int id = 1, string symbol = "AAPL", bool historyLoaded = true) => new() { Id = id, Symbol = symbol, HistoryLoaded = historyLoaded };

    private static Portfolio PortfolioEntity(Asset asset, decimal quantity = 10) => new() { Id = 1, UserId = 1, AssetId = asset.Id, Asset = asset, Quantity = quantity, AvgPrice = 100 };

    private static HistoricalPriceDto Candle(DateTime date, decimal close = 150) => new() { Date = date, Open = close, High = close, Low = close, Close = close, Volume = 1000 };

    // ---------- GenerateDailyPortfolioSnapshotsAsync ----------

    /// <summary>Verifica que, si ya existe un snapshot para el usuario en el día actual, no se genere uno nuevo.</summary>
    [Fact]
    public async Task GenerateDailyPortfolioSnapshotsAsync_WhenSnapshotAlreadyExistsForToday_ShouldSkipUser()
    {
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { UserEntity() });
        _portfolioHistoryRepository.Setup(r => r.ExistsByDateAsync(1, It.IsAny<DateTime>())).ReturnsAsync(true);

        await CreateService().GenerateDailyPortfolioSnapshotsAsync();

        _portfolioHistoryRepository.Verify(r => r.InsertAsync(It.IsAny<PortfolioHistory>()), Times.Never);
    }

    /// <summary>Verifica que, cuando el usuario no tiene posiciones abiertas, el snapshot se genere utilizando únicamente su saldo disponible.</summary>
    [Fact]
    public async Task GenerateDailyPortfolioSnapshotsAsync_WhenUserHasNoOpenPositions_ShouldUseBalanceAsTotalValue()
    {
        var user = UserEntity(balance: 1000);
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
        _portfolioHistoryRepository.Setup(r => r.ExistsByDateAsync(1, It.IsAny<DateTime>())).ReturnsAsync(false);
        _portfolioRepository.Setup(r => r.GetByUserAsync(1)).ReturnsAsync(new List<Portfolio>());
        _marketPriceService.Setup(s => s.GetHistoricalPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new Dictionary<string, decimal>());

        await CreateService().GenerateDailyPortfolioSnapshotsAsync();

        _portfolioHistoryRepository.Verify(r => r.InsertAsync(It.Is<PortfolioHistory>(h => h.UserId == 1 && h.TotalValue == 1000)), Times.Once);
    }

    /// <summary>Verifica que, cuando se dispone del precio de todos los activos en cartera, el valor total se calcule sumando el saldo y el valor de las posiciones abiertas.</summary>
    [Fact]
    public async Task GenerateDailyPortfolioSnapshotsAsync_WhenPricesAreAvailableForAllPositions_ShouldComputeTotalValueIncludingHoldings()
    {
        var user = UserEntity(balance: 1000);
        var asset = AssetEntity();
        var portfolio = new List<Portfolio> { PortfolioEntity(asset, quantity: 10) };

        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
        _portfolioHistoryRepository.Setup(r => r.ExistsByDateAsync(1, It.IsAny<DateTime>())).ReturnsAsync(false);
        _portfolioRepository.Setup(r => r.GetByUserAsync(1)).ReturnsAsync(portfolio);
        _marketPriceService.Setup(s => s.GetHistoricalPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new Dictionary<string, decimal> { ["AAPL"] = 120 });

        await CreateService().GenerateDailyPortfolioSnapshotsAsync();

        _portfolioHistoryRepository.Verify(r => r.InsertAsync(It.Is<PortfolioHistory>(h => h.UserId == 1 && h.TotalValue == 2200)), Times.Once);
    }

    /// <summary>Verifica que, cuando no se dispone del precio de un activo en cartera, su posición se omita del cálculo del valor total.</summary>
    [Fact]
    public async Task GenerateDailyPortfolioSnapshotsAsync_WhenPriceIsMissingForAnAsset_ShouldExcludeItFromTotalValue()
    {
        var user = UserEntity(balance: 1000);
        var aapl = AssetEntity(1, "AAPL");
        var msft = AssetEntity(2, "MSFT");
        var portfolio = new List<Portfolio> { PortfolioEntity(aapl, quantity: 10), new() { Id = 2, UserId = 1, AssetId = msft.Id, Asset = msft, Quantity = 5, AvgPrice = 50 } };

        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
        _portfolioHistoryRepository.Setup(r => r.ExistsByDateAsync(1, It.IsAny<DateTime>())).ReturnsAsync(false);
        _portfolioRepository.Setup(r => r.GetByUserAsync(1)).ReturnsAsync(portfolio);
        _marketPriceService.Setup(s => s.GetHistoricalPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new Dictionary<string, decimal> { ["AAPL"] = 120 });

        await CreateService().GenerateDailyPortfolioSnapshotsAsync();

        _portfolioHistoryRepository.Verify(r => r.InsertAsync(It.Is<PortfolioHistory>(h => h.TotalValue == 2200)), Times.Once);
    }

    /// <summary>Verifica que, si ocurre un error al generar el snapshot de un usuario, se registre el error y se continúe procesando a los demás usuarios.</summary>
    [Fact]
    public async Task GenerateDailyPortfolioSnapshotsAsync_WhenGeneratingSnapshotForOneUserThrows_ShouldLogErrorAndContinueWithOthers()
    {
        var failingUser = UserEntity(id: 1);
        var workingUser = UserEntity(id: 2);

        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { failingUser, workingUser });
        _portfolioHistoryRepository.Setup(r => r.ExistsByDateAsync(1, It.IsAny<DateTime>())).ThrowsAsync(new Exception("db error"));
        _portfolioHistoryRepository.Setup(r => r.ExistsByDateAsync(2, It.IsAny<DateTime>())).ReturnsAsync(false);
        _portfolioRepository.Setup(r => r.GetByUserAsync(2)).ReturnsAsync(new List<Portfolio>());
        _marketPriceService.Setup(s => s.GetHistoricalPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new Dictionary<string, decimal>());

        await CreateService().GenerateDailyPortfolioSnapshotsAsync();

        _portfolioHistoryRepository.Verify(r => r.InsertAsync(It.Is<PortfolioHistory>(h => h.UserId == 2)), Times.Once);
    }

    // ---------- SaveDailyMarketHistoryAsync ----------

    /// <summary>Verifica que, cuando no se obtienen datos históricos para un activo, no se inserte ninguna vela diaria para ese activo.</summary>
    [Fact]
    public async Task SaveDailyMarketHistoryAsync_WhenNoHistoricalDataIsAvailable_ShouldNotInsertCandle()
    {
        _assetRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Asset> { AssetEntity() });
        _externalProvider.Setup(p => p.GetHistoricalAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new List<HistoricalPriceDto>());
        _priceHistoryRepository.Setup(r => r.GetLatestDateAsync()).ReturnsAsync((DateTime?)null);

        await CreateService().SaveDailyMarketHistoryAsync();

        _priceHistoryRepository.Verify(r => r.InsertAsync(It.IsAny<PriceHistory>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /// <summary>Verifica que, si el cierre diario más reciente ya se encuentra almacenado, no se inserte un registro duplicado.</summary>
    [Fact]
    public async Task SaveDailyMarketHistoryAsync_WhenLatestCandleIsAlreadyStored_ShouldNotInsertDuplicate()
    {
        var today = DateTime.UtcNow.Date;
        _assetRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Asset> { AssetEntity() });
        _externalProvider.Setup(p => p.GetHistoricalAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new List<HistoricalPriceDto> { Candle(today) });
        _priceHistoryRepository.Setup(r => r.GetLatestDateAsync("AAPL")).ReturnsAsync(today);
        _priceHistoryRepository.Setup(r => r.GetLatestDateAsync()).ReturnsAsync((DateTime?)null);

        await CreateService().SaveDailyMarketHistoryAsync();

        _priceHistoryRepository.Verify(r => r.InsertAsync(It.IsAny<PriceHistory>()), Times.Never);
    }

    /// <summary>Verifica que, con datos válidos, se inserte la nueva vela diaria, se actualice la metadata de mercado y se confirmen los cambios.</summary>
    [Fact]
    public async Task SaveDailyMarketHistoryAsync_WhenDataIsValid_ShouldInsertCandleAndUpdateMarketMetadata()
    {
        var today = DateTime.UtcNow.Date;
        _assetRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Asset> { AssetEntity() });
        _externalProvider.Setup(p => p.GetHistoricalAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new List<HistoricalPriceDto> { Candle(today, close: 150) });
        _priceHistoryRepository.Setup(r => r.GetLatestDateAsync("AAPL")).ReturnsAsync(today.AddDays(-1));
        _priceHistoryRepository.Setup(r => r.GetLatestDateAsync()).ReturnsAsync(today);

        await CreateService().SaveDailyMarketHistoryAsync();

        _priceHistoryRepository.Verify(r => r.InsertAsync(It.Is<PriceHistory>(h => h.Symbol == "AAPL" && h.Close == 150 && h.Date == today)), Times.Once);
        _marketMetadataRepository.Verify(r => r.UpdateLastCloseAsync(today), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /// <summary>Verifica que, si no existen históricos almacenados, no se actualice la metadata global de mercado.</summary>
    [Fact]
    public async Task SaveDailyMarketHistoryAsync_WhenNoStoredHistoryExists_ShouldNotUpdateMarketMetadata()
    {
        _assetRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Asset>());
        _priceHistoryRepository.Setup(r => r.GetLatestDateAsync()).ReturnsAsync((DateTime?)null);

        await CreateService().SaveDailyMarketHistoryAsync();

        _marketMetadataRepository.Verify(r => r.UpdateLastCloseAsync(It.IsAny<DateTime>()), Times.Never);
    }
}
