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
using static InvestLab.Models.Enums;

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
    private readonly Mock<IMarketProviderResolver> _providerResolver = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<DailySnapshotService>> _logger = new();

    private DailySnapshotService CreateService()
    {
        _providerResolver.Setup(x => x.GetProvider()).Returns(_externalProvider.Object);
        return new(_userRepository.Object, _portfolioRepository.Object, _portfolioHistoryRepository.Object, _providerResolver.Object, _logger.Object, _priceHistoryRepository.Object, _marketPriceService.Object, _assetRepository.Object, _marketMetadataRepository.Object, _unitOfWork.Object, _transactionRepository.Object);
    }

    private static readonly DateTime MarketCloseUtc = new DateTime(2025, 6, 8, 20, 5, 0, DateTimeKind.Utc);

    private static User UserEntity(int id = 1, decimal balance = 1000, DateTime? createdAt = null) =>
        new() { Id = id, Username = "user", Email = "user@test.com", PasswordHash = "hash", Phone = "123", Balance = balance, CreatedAt = createdAt ?? new DateTime(2025, 6, 8) };

    private static Asset AssetEntity(int id = 1, string symbol = "AAPL", bool historyLoaded = true) => new() { Id = id, Symbol = symbol, HistoryLoaded = historyLoaded };

    private static Portfolio PortfolioEntity(Asset asset, decimal quantity = 10) => new() { Id = 1, UserId = 1, AssetId = asset.Id, Asset = asset, Quantity = quantity, AvgPrice = 100 };

    private static HistoricalPriceDto Candle(DateTime date, decimal close = 150) => new() { Date = date, Open = close, High = close, Low = close, Close = close, Volume = 1000 };

    private static Transaction BuyTx(int userId, int assetId, decimal quantity, decimal total, DateTime createdAt) =>
        new() { UserId = userId, AssetId = assetId, Type = TransactionType.Buy, Quantity = quantity, Price = total / quantity, Total = total, CreatedAt = createdAt };

    private static Transaction SellTx(int userId, int assetId, decimal quantity, decimal total, DateTime createdAt) =>
        new() { UserId = userId, AssetId = assetId, Type = TransactionType.Sell, Quantity = quantity, Price = total / quantity, Total = total, CreatedAt = createdAt };

    private static Transaction BuyTxWithAsset(int userId, Asset asset, decimal quantity, decimal total, DateTime createdAt) =>
        new() { UserId = userId, AssetId = asset.Id, Asset = asset, Type = TransactionType.Buy, Quantity = quantity, Price = total / quantity, Total = total, CreatedAt = createdAt };

    private static Transaction SellTxWithAsset(int userId, Asset asset, decimal quantity, decimal total, DateTime createdAt) =>
        new() { UserId = userId, AssetId = asset.Id, Asset = asset, Type = TransactionType.Sell, Quantity = quantity, Price = total / quantity, Total = total, CreatedAt = createdAt };

    // ---------- GenerateDailyPortfolioSnapshotsAsync ----------

    /// <summary>Verifica que, si ya existe un snapshot para el usuario en el día indicado, no se genere uno nuevo.</summary>
    [Fact]
    public async Task GenerateDailyPortfolioSnapshotsAsync_WhenSnapshotAlreadyExistsForToday_ShouldSkipUser()
    {
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { UserEntity() });
        _portfolioHistoryRepository.Setup(r => r.ExistsByDateAsync(1, It.IsAny<DateTime>())).ReturnsAsync(true);

        await CreateService().GenerateDailyPortfolioSnapshotsAsync(MarketCloseUtc);

        _portfolioHistoryRepository.Verify(r => r.InsertAsync(It.IsAny<PortfolioHistory>()), Times.Never);
    }

    /// <summary>Verifica que, cuando el usuario no tiene posiciones abiertas, el snapshot se genere utilizando únicamente su saldo disponible.</summary>
    [Fact]
    public async Task GenerateDailyPortfolioSnapshotsAsync_WhenUserHasNoOpenPositions_ShouldUseBalanceAsTotalValue()
    {
        var user = UserEntity(balance: 1000);
        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
        _portfolioHistoryRepository.Setup(r => r.ExistsByDateAsync(1, It.IsAny<DateTime>())).ReturnsAsync(false);
        _transactionRepository.Setup(r => r.GetByUserAfterDateAsync(1, MarketCloseUtc)).ReturnsAsync(new List<Transaction>());
        _portfolioRepository.Setup(r => r.GetByUserAsync(1)).ReturnsAsync(new List<Portfolio>());
        _marketPriceService.Setup(s => s.GetHistoricalPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new Dictionary<string, decimal>());

        await CreateService().GenerateDailyPortfolioSnapshotsAsync(MarketCloseUtc);

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
        _transactionRepository.Setup(r => r.GetByUserAfterDateAsync(1, MarketCloseUtc)).ReturnsAsync(new List<Transaction>());
        _portfolioRepository.Setup(r => r.GetByUserAsync(1)).ReturnsAsync(portfolio);
        _marketPriceService.Setup(s => s.GetHistoricalPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new Dictionary<string, decimal> { ["AAPL"] = 120 });

        await CreateService().GenerateDailyPortfolioSnapshotsAsync(MarketCloseUtc);

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
        _transactionRepository.Setup(r => r.GetByUserAfterDateAsync(1, MarketCloseUtc)).ReturnsAsync(new List<Transaction>());
        _portfolioRepository.Setup(r => r.GetByUserAsync(1)).ReturnsAsync(portfolio);
        _marketPriceService.Setup(s => s.GetHistoricalPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new Dictionary<string, decimal> { ["AAPL"] = 120 });

        await CreateService().GenerateDailyPortfolioSnapshotsAsync(MarketCloseUtc);

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
        _transactionRepository.Setup(r => r.GetByUserAfterDateAsync(2, MarketCloseUtc)).ReturnsAsync(new List<Transaction>());
        _portfolioRepository.Setup(r => r.GetByUserAsync(2)).ReturnsAsync(new List<Portfolio>());
        _marketPriceService.Setup(s => s.GetHistoricalPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new Dictionary<string, decimal>());

        await CreateService().GenerateDailyPortfolioSnapshotsAsync(MarketCloseUtc);

        _portfolioHistoryRepository.Verify(r => r.InsertAsync(It.Is<PortfolioHistory>(h => h.UserId == 2)), Times.Once);
    }

    /// <summary>Verifica que el snapshot use la fecha del cierre recibido como parámetro, no la fecha actual del sistema.</summary>
    [Fact]
    public async Task GenerateDailyPortfolioSnapshotsAsync_WhenMarketCloseIsProvided_ShouldUseItsDateForSnapshotAndExistenceCheck()
    {
        var closeUtc = new DateTime(2025, 3, 15, 21, 5, 0, DateTimeKind.Utc); // 16:05 NY horario estándar
        var user = UserEntity(balance: 500);

        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
        _portfolioHistoryRepository.Setup(r => r.ExistsByDateAsync(1, closeUtc.Date)).ReturnsAsync(false);
        _transactionRepository.Setup(r => r.GetByUserAfterDateAsync(1, closeUtc)).ReturnsAsync(new List<Transaction>());
        _portfolioRepository.Setup(r => r.GetByUserAsync(1)).ReturnsAsync(new List<Portfolio>());
        _marketPriceService.Setup(s => s.GetHistoricalPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new Dictionary<string, decimal>());

        await CreateService().GenerateDailyPortfolioSnapshotsAsync(closeUtc);

        _portfolioHistoryRepository.Verify(r => r.ExistsByDateAsync(1, closeUtc.Date), Times.Once);
        _portfolioHistoryRepository.Verify(r => r.InsertAsync(It.Is<PortfolioHistory>(h => h.Date == closeUtc.Date && h.TotalValue == 500)), Times.Once);
    }

    /// <summary>Verifica que una compra realizada después del cierre sea revertida al calcular el balance del snapshot,
    /// de forma que no impacte en el historial del día de cierre.</summary>
    [Fact]
    public async Task GenerateDailyPortfolioSnapshotsAsync_WhenPostCloseBuyExists_ShouldRestoreBalanceToValueAtClose()
    {
        // Balance actual: 400 (luego de la compra post-cierre de 600)
        // Balance al cierre: 400 + 600 = 1000
        var user = UserEntity(balance: 400);
        var buyAfterClose = BuyTx(userId: 1, assetId: 1, quantity: 3, total: 600, createdAt: MarketCloseUtc.AddHours(2));

        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
        _portfolioHistoryRepository.Setup(r => r.ExistsByDateAsync(1, It.IsAny<DateTime>())).ReturnsAsync(false);
        _transactionRepository.Setup(r => r.GetByUserAfterDateAsync(1, MarketCloseUtc)).ReturnsAsync(new List<Transaction> { buyAfterClose });
        _portfolioRepository.Setup(r => r.GetByUserAsync(1)).ReturnsAsync(new List<Portfolio>());
        _marketPriceService.Setup(s => s.GetHistoricalPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new Dictionary<string, decimal>());

        await CreateService().GenerateDailyPortfolioSnapshotsAsync(MarketCloseUtc);

        _portfolioHistoryRepository.Verify(r => r.InsertAsync(It.Is<PortfolioHistory>(h => h.TotalValue == 1000)), Times.Once);
    }

    /// <summary>Verifica que una venta realizada después del cierre sea revertida al calcular el balance del snapshot.</summary>
    [Fact]
    public async Task GenerateDailyPortfolioSnapshotsAsync_WhenPostCloseSellExists_ShouldRestoreBalanceToValueAtClose()
    {
        // Balance actual: 1500 (luego de la venta post-cierre que generó 500)
        // Balance al cierre: 1500 - 500 = 1000
        var user = UserEntity(balance: 1500);
        var sellAfterClose = SellTx(userId: 1, assetId: 1, quantity: 5, total: 500, createdAt: MarketCloseUtc.AddHours(3));

        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
        _portfolioHistoryRepository.Setup(r => r.ExistsByDateAsync(1, It.IsAny<DateTime>())).ReturnsAsync(false);
        _transactionRepository.Setup(r => r.GetByUserAfterDateAsync(1, MarketCloseUtc)).ReturnsAsync(new List<Transaction> { sellAfterClose });
        _portfolioRepository.Setup(r => r.GetByUserAsync(1)).ReturnsAsync(new List<Portfolio>());
        _marketPriceService.Setup(s => s.GetHistoricalPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new Dictionary<string, decimal>());

        await CreateService().GenerateDailyPortfolioSnapshotsAsync(MarketCloseUtc);

        _portfolioHistoryRepository.Verify(r => r.InsertAsync(It.Is<PortfolioHistory>(h => h.TotalValue == 1000)), Times.Once);
    }

    /// <summary>Verifica que las acciones compradas post-cierre no se cuenten como parte de las tenencias del día.</summary>
    [Fact]
    public async Task GenerateDailyPortfolioSnapshotsAsync_WhenPostCloseBuyExists_ShouldExcludeAcquiredQuantityFromHoldings()
    {
        // Posición actual: 8 acciones de AAPL (5 al cierre + 3 compradas post-cierre)
        // Al cierre solo había 5 → holdings al cierre = 5 × 120 = 600
        // Balance al cierre = 400 + 600 (compra post-cierre) = 1000
        // Total al cierre = 1000 + 600 = 1600
        var user = UserEntity(balance: 400);
        var asset = AssetEntity(id: 1, symbol: "AAPL");
        var portfolioAtPresent = new List<Portfolio> { new() { Id = 1, UserId = 1, AssetId = 1, Asset = asset, Quantity = 8, AvgPrice = 100 } };
        var buyAfterClose = BuyTx(userId: 1, assetId: 1, quantity: 3, total: 600, createdAt: MarketCloseUtc.AddHours(2));

        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
        _portfolioHistoryRepository.Setup(r => r.ExistsByDateAsync(1, It.IsAny<DateTime>())).ReturnsAsync(false);
        _transactionRepository.Setup(r => r.GetByUserAfterDateAsync(1, MarketCloseUtc)).ReturnsAsync(new List<Transaction> { buyAfterClose });
        _portfolioRepository.Setup(r => r.GetByUserAsync(1)).ReturnsAsync(portfolioAtPresent);
        _marketPriceService.Setup(s => s.GetHistoricalPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new Dictionary<string, decimal> { ["AAPL"] = 120 });

        await CreateService().GenerateDailyPortfolioSnapshotsAsync(MarketCloseUtc);

        // balance al cierre = 400 + 600 = 1000; holdings al cierre = 5 × 120 = 600; total = 1600
        _portfolioHistoryRepository.Verify(r => r.InsertAsync(It.Is<PortfolioHistory>(h => h.TotalValue == 1600)), Times.Once);
    }

    // ---------- RunHistoricalCatchUpAsync ----------

    /// <summary>
    /// Si el usuario nunca tuvo snapshots y no tiene operaciones, se deben generar snapshots
    /// para todos los días bursátiles desde CreatedAt hasta lastCloseUtc, con valor = balance.
    /// Escenario: creado el lunes 02/06/2025, último cierre el viernes 06/06/2025 → 5 snapshots.
    /// </summary>
    [Fact]
    public async Task RunHistoricalCatchUpAsync_WhenUserHasNoSnapshotsAndNoTransactions_ShouldGenerateSnapshotsForAllMissingBusinessDays()
    {
        var createdAt = new DateTime(2025, 6, 2); // lunes
        var lastClose = new DateTime(2025, 6, 6, 20, 5, 0, DateTimeKind.Utc); // viernes 16:05 NY EDT
        var user = UserEntity(balance: 10_000, createdAt: createdAt);

        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
        _portfolioHistoryRepository.Setup(r => r.GetLatestAsync(1)).ReturnsAsync((PortfolioHistory?)null);
        _portfolioRepository.Setup(r => r.GetByUserAsync(1)).ReturnsAsync(new List<Portfolio>());
        _transactionRepository.Setup(r => r.GetAllByUserAsync(1)).ReturnsAsync(new List<Transaction>());

        await CreateService().RunHistoricalCatchUpAsync(lastClose);

        // 02/06, 03/06, 04/06, 05/06, 06/06 (lun–vie) → 5 snapshots todos con valor 10 000
        _portfolioHistoryRepository.Verify(
            r => r.InsertAsync(It.Is<PortfolioHistory>(h => h.UserId == 1 && h.TotalValue == 10_000)),
            Times.Exactly(5));
    }

    /// <summary>
    /// Si el usuario tiene un snapshot previo, la recuperación debe comenzar desde el día siguiente
    /// al último snapshot, sin duplicar el existente.
    /// Escenario: último snapshot = 04/06/2025, último cierre = 06/06/2025 → 2 snapshots (05 y 06).
    /// </summary>
    [Fact]
    public async Task RunHistoricalCatchUpAsync_WhenUserHasExistingSnapshot_ShouldStartFromDayAfterLastSnapshot()
    {
        var lastClose = new DateTime(2025, 6, 6, 20, 5, 0, DateTimeKind.Utc);
        var user = UserEntity(balance: 10_000, createdAt: new DateTime(2025, 6, 2));
        var existingSnapshot = new PortfolioHistory { UserId = 1, Date = new DateTime(2025, 6, 4), TotalValue = 10_000 };

        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
        _portfolioHistoryRepository.Setup(r => r.GetLatestAsync(1)).ReturnsAsync(existingSnapshot);
        _portfolioRepository.Setup(r => r.GetByUserAsync(1)).ReturnsAsync(new List<Portfolio>());
        _transactionRepository.Setup(r => r.GetAllByUserAsync(1)).ReturnsAsync(new List<Transaction>());

        await CreateService().RunHistoricalCatchUpAsync(lastClose);

        // Solo 05/06 y 06/06 (04/06 ya existía)
        _portfolioHistoryRepository.Verify(
            r => r.InsertAsync(It.Is<PortfolioHistory>(h => h.UserId == 1 && h.TotalValue == 10_000)),
            Times.Exactly(2));
    }

    /// <summary>
    /// Si ya existen todos los snapshots hasta el último cierre, no se debe insertar ninguno nuevo.
    /// </summary>
    [Fact]
    public async Task RunHistoricalCatchUpAsync_WhenAllSnapshotsExist_ShouldInsertNothing()
    {
        var lastClose = new DateTime(2025, 6, 6, 20, 5, 0, DateTimeKind.Utc);
        var user = UserEntity(balance: 10_000, createdAt: new DateTime(2025, 6, 2));
        var upToDate = new PortfolioHistory { UserId = 1, Date = new DateTime(2025, 6, 6), TotalValue = 10_000 };

        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
        _portfolioHistoryRepository.Setup(r => r.GetLatestAsync(1)).ReturnsAsync(upToDate);

        await CreateService().RunHistoricalCatchUpAsync(lastClose);

        _portfolioHistoryRepository.Verify(r => r.InsertAsync(It.IsAny<PortfolioHistory>()), Times.Never);
    }

    /// <summary>
    /// Cuando hay transacciones posteriores a la fecha del snapshot a reconstruir,
    /// el balance debe reconstruirse revirtiendo esas operaciones.
    /// Una compra de 600 después de la fecha D → balance en D = balance_actual + 600.
    /// </summary>
    [Fact]
    public async Task RunHistoricalCatchUpAsync_WhenBuyOccurredAfterSnapshotDate_ShouldAddBuyTotalBackToBalance()
    {
        // Escenario: usuario creado el 03/06, último cierre el 04/06.
        // Realizó una compra de 600 el 05/06 (después del día a reconstruir).
        // Balance actual: 9400. Balance al 04/06 = 9400 + 600 = 10 000.
        var createdAt = new DateTime(2025, 6, 3);
        var lastClose = new DateTime(2025, 6, 4, 20, 5, 0, DateTimeKind.Utc);
        var user = UserEntity(balance: 9_400, createdAt: createdAt);
        var asset = AssetEntity();
        var buyAfter = BuyTxWithAsset(1, asset, quantity: 3, total: 600, createdAt: new DateTime(2025, 6, 5, 10, 0, 0));

        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
        _portfolioHistoryRepository.Setup(r => r.GetLatestAsync(1)).ReturnsAsync((PortfolioHistory?)null);
        _portfolioRepository.Setup(r => r.GetByUserAsync(1)).ReturnsAsync(new List<Portfolio>());
        _transactionRepository.Setup(r => r.GetAllByUserAsync(1)).ReturnsAsync(new List<Transaction> { buyAfter });

        await CreateService().RunHistoricalCatchUpAsync(lastClose);

        // 03/06 y 04/06: ambos con balance 10 000, sin holdings (la compra fue el 05/06)
        _portfolioHistoryRepository.Verify(
            r => r.InsertAsync(It.Is<PortfolioHistory>(h => h.UserId == 1 && h.TotalValue == 10_000)),
            Times.Exactly(2));
    }

    /// <summary>
    /// Cuando hay tenencias activas y se dispone de precio histórico para el día reconstruido,
    /// el snapshot debe incluir su valorización.
    /// </summary>
    [Fact]
    public async Task RunHistoricalCatchUpAsync_WhenUserHasHoldingsWithHistoricalPrice_ShouldIncludeHoldingsInSnapshot()
    {
        // Escenario: usuario creado el 05/06, último cierre el 05/06.
        // Tiene 10 acciones de AAPL (compradas antes del 05/06), precio histórico del 05/06 = 150.
        // Balance = 1000. Total esperado = 1000 + 10 × 150 = 2500.
        var createdAt = new DateTime(2025, 6, 5);
        var lastClose = new DateTime(2025, 6, 5, 20, 5, 0, DateTimeKind.Utc);
        var user = UserEntity(balance: 1_000, createdAt: createdAt);
        var asset = AssetEntity(symbol: "AAPL");
        var portfolio = new List<Portfolio> { PortfolioEntity(asset, quantity: 10) };

        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
        _portfolioHistoryRepository.Setup(r => r.GetLatestAsync(1)).ReturnsAsync((PortfolioHistory?)null);
        _portfolioRepository.Setup(r => r.GetByUserAsync(1)).ReturnsAsync(portfolio);
        _transactionRepository.Setup(r => r.GetAllByUserAsync(1)).ReturnsAsync(new List<Transaction>());
        _priceHistoryRepository.Setup(r => r.GetClosingPriceOnOrBeforeAsync("AAPL", new DateTime(2025, 6, 5))).ReturnsAsync(150m);

        await CreateService().RunHistoricalCatchUpAsync(lastClose);

        _portfolioHistoryRepository.Verify(
            r => r.InsertAsync(It.Is<PortfolioHistory>(h => h.UserId == 1 && h.TotalValue == 2_500)),
            Times.Once);
    }

    /// <summary>
    /// Si un activo fue vendido después de la fecha a reconstruir,
    /// debe aparecer en las tenencias históricas con la cantidad correcta.
    /// </summary>
    [Fact]
    public async Task RunHistoricalCatchUpAsync_WhenAssetWasSoldAfterSnapshotDate_ShouldRestoreItToHistoricalHoldings()
    {
        // Escenario: usuario creado el 04/06, último cierre el 04/06.
        // Tenía 5 AAPL al 04/06 y las vendió todas el 05/06.
        // Portfolio actual: vacío. Balance actual: 1500 (= 1000 + 500 de la venta).
        // Al 04/06: balance = 1500 - 500 = 1000; holdings = 5 × 120 = 600; total = 1600.
        var createdAt = new DateTime(2025, 6, 4);
        var lastClose = new DateTime(2025, 6, 4, 20, 5, 0, DateTimeKind.Utc);
        var user = UserEntity(balance: 1_500, createdAt: createdAt);
        var asset = AssetEntity(symbol: "AAPL");
        var sellAfter = SellTxWithAsset(1, asset, quantity: 5, total: 500, createdAt: new DateTime(2025, 6, 5, 10, 0, 0));

        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { user });
        _portfolioHistoryRepository.Setup(r => r.GetLatestAsync(1)).ReturnsAsync((PortfolioHistory?)null);
        _portfolioRepository.Setup(r => r.GetByUserAsync(1)).ReturnsAsync(new List<Portfolio>()); // portfolio actual vacío
        _transactionRepository.Setup(r => r.GetAllByUserAsync(1)).ReturnsAsync(new List<Transaction> { sellAfter });
        _priceHistoryRepository.Setup(r => r.GetClosingPriceOnOrBeforeAsync("AAPL", new DateTime(2025, 6, 4))).ReturnsAsync(120m);

        await CreateService().RunHistoricalCatchUpAsync(lastClose);

        // balance al 04/06 = 1000; holdings = 5 × 120 = 600; total = 1600
        _portfolioHistoryRepository.Verify(
            r => r.InsertAsync(It.Is<PortfolioHistory>(h => h.UserId == 1 && h.TotalValue == 1_600)),
            Times.Once);
    }

    /// <summary>
    /// Si un error ocurre procesando un usuario, debe registrarse y continuar con los demás.
    /// </summary>
    [Fact]
    public async Task RunHistoricalCatchUpAsync_WhenOneUserFails_ShouldLogErrorAndContinueWithOthers()
    {
        var lastClose = new DateTime(2025, 6, 6, 20, 5, 0, DateTimeKind.Utc);
        var failingUser = UserEntity(id: 1, createdAt: new DateTime(2025, 6, 6));
        var workingUser = UserEntity(id: 2, balance: 5_000, createdAt: new DateTime(2025, 6, 6));

        _userRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { failingUser, workingUser });
        _portfolioHistoryRepository.Setup(r => r.GetLatestAsync(1)).ThrowsAsync(new Exception("db failure"));
        _portfolioHistoryRepository.Setup(r => r.GetLatestAsync(2)).ReturnsAsync((PortfolioHistory?)null);
        _portfolioRepository.Setup(r => r.GetByUserAsync(2)).ReturnsAsync(new List<Portfolio>());
        _transactionRepository.Setup(r => r.GetAllByUserAsync(2)).ReturnsAsync(new List<Transaction>());

        await CreateService().RunHistoricalCatchUpAsync(lastClose);

        // Usuario 1 falló, usuario 2 debe haber generado su snapshot
        _portfolioHistoryRepository.Verify(
            r => r.InsertAsync(It.Is<PortfolioHistory>(h => h.UserId == 2 && h.TotalValue == 5_000)),
            Times.Once);
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
