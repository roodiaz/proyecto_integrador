using InvestLab.Business.Interfaces.Api;
using InvestLab.Business.Services.Api;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Models.Documents;
using InvestLab.Models.DTOs.Dashboard;
using InvestLab.Models.DTOs.Market;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static InvestLab.Models.Enums;

namespace InvestLab.Tests.Business.Services.Api;

/// <summary>
/// Pruebas unitarias de <see cref="DashboardService"/>, cubriendo los métodos invocados desde <c>DashboardController</c>
/// para construir las tarjetas, la distribución de cartera, las notificaciones recientes, el gráfico de desempeño,
/// las últimas operaciones y la composición del portfolio activo.
/// </summary>
public class DashboardServiceTests
{
    private readonly Mock<IUserPortfolioRepository> _userPortfolioRepository = new();
    private readonly Mock<IPortfolioHoldingRepository> _portfolioHoldingRepository = new();
    private readonly Mock<IPortfolioHistoryRepository> _portfolioHistoryRepository = new();
    private readonly Mock<IMarketPriceCacheService> _marketPriceCacheService = new();
    private readonly Mock<ILogger<DashboardService>> _logger = new();
    private readonly Mock<IAlertRepository> _alertRepository = new();
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<INotificationRepository> _notificationRepository = new();

    private DashboardService CreateService() => new(_userPortfolioRepository.Object, _portfolioHoldingRepository.Object, _portfolioHistoryRepository.Object, _marketPriceCacheService.Object, _logger.Object, _alertRepository.Object, _priceHistoryRepository.Object, _transactionRepository.Object, _notificationRepository.Object);

    private static UserPortfolio PortfolioEntity(int id = 1, int userId = 1, decimal currentBalance = 1000, decimal initialBalance = 10000) => new() { Id = id, UserId = userId, Name = "Mi Portfolio", InitialBalance = initialBalance, CurrentBalance = currentBalance, IsActive = true };

    private static Asset AssetEntity(int id = 1, string symbol = "AAPL", string? sector = "Tecnología") => new() { Id = id, Symbol = symbol, Sector = sector };

    private static PortfolioHolding HoldingEntity(Asset asset, int portfolioId = 1, decimal quantity = 10, decimal avgPrice = 100) => new() { Id = 1, UserId = 1, PortfolioId = portfolioId, AssetId = asset.Id, Asset = asset, Quantity = quantity, AvgPrice = avgPrice };

    private static MarketPricesResponseDto Prices(params (string Symbol, decimal Price)[] prices) =>
        new() { Prices = prices.Select(p => new MarketPriceDto { Symbol = p.Symbol, Price = p.Price }).ToList() };

    private static PortfolioHistory History(decimal totalValue, DateTime date) => new() { UserId = 1, PortfolioId = 1, Date = date, TotalValue = totalValue };

    private static PriceHistory PriceHistoryEntity(string symbol, decimal close, DateTime date) => new() { Id = Guid.NewGuid().ToString(), Symbol = symbol, Date = date, Open = close, High = close, Low = close, Close = close, Volume = 100 };

    // ---------- GetTopCardsAsync ----------

    /// <summary>Verifica que, si el portfolio no existe o no pertenece al usuario, se devuelva una respuesta de error sin consultar las posiciones.</summary>
    [Fact]
    public async Task GetTopCardsAsync_WhenPortfolioDoesNotExist_ShouldReturnErrorResponse()
    {
        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((UserPortfolio?)null);

        var result = await CreateService().GetTopCardsAsync(1, 1);

        Assert.False(result.Success);
        Assert.Equal("Portfolio no encontrado", result.Message);
        _portfolioHoldingRepository.Verify(r => r.GetByPortfolioAsync(It.IsAny<int>()), Times.Never);
    }

    /// <summary>Verifica que, con datos válidos, se calcule correctamente el valor total, la ganancia del día, los activos activos y las alertas activas.</summary>
    [Fact]
    public async Task GetTopCardsAsync_WhenDataIsValid_ShouldReturnComputedTopCards()
    {
        var asset = AssetEntity();
        var holdings = new List<PortfolioHolding> { HoldingEntity(asset, quantity: 10) };

        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(1, 1)).ReturnsAsync(PortfolioEntity(currentBalance: 1000));
        _portfolioHoldingRepository.Setup(r => r.GetByPortfolioAsync(1)).ReturnsAsync(holdings);
        _marketPriceCacheService.Setup(s => s.GetPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(Prices(("AAPL", 150)));
        _portfolioHistoryRepository.Setup(r => r.GetPreviousAsync(1)).ReturnsAsync(History(2400, DateTime.UtcNow.AddDays(-1)));
        _alertRepository.Setup(r => r.CountByUserAsync(1)).ReturnsAsync(3);

        var result = await CreateService().GetTopCardsAsync(1, 1);

        Assert.True(result.Success);
        var data = Assert.IsType<DashboardTopCardsDto>(result.Data);
        Assert.Equal(2500, data.TotalValue);
        Assert.Equal(100, data.TodayProfit);
        Assert.Equal(1, data.ActiveAssets);
        Assert.Equal(3, data.ActiveAlerts);
    }

    /// <summary>Verifica el caso borde donde no existe historial previo de cartera: la ganancia del día debe quedar en cero.</summary>
    [Fact]
    public async Task GetTopCardsAsync_WhenNoPreviousHistoryExists_ShouldReturnZeroTodayProfit()
    {
        var asset = AssetEntity();
        var holdings = new List<PortfolioHolding> { HoldingEntity(asset, quantity: 5) };

        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(1, 1)).ReturnsAsync(PortfolioEntity(currentBalance: 500));
        _portfolioHoldingRepository.Setup(r => r.GetByPortfolioAsync(1)).ReturnsAsync(holdings);
        _marketPriceCacheService.Setup(s => s.GetPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(Prices(("AAPL", 100)));
        _portfolioHistoryRepository.Setup(r => r.GetPreviousAsync(1)).ReturnsAsync((PortfolioHistory?)null);
        _alertRepository.Setup(r => r.CountByUserAsync(1)).ReturnsAsync(0);

        var result = await CreateService().GetTopCardsAsync(1, 1);

        Assert.True(result.Success);
        var data = Assert.IsType<DashboardTopCardsDto>(result.Data);
        Assert.Equal(0, data.TodayProfit);
        Assert.Equal(0, data.TodayProfitPercent);
    }

    /// <summary>Verifica que, si el repositorio lanza una excepción, se registre el error y se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task GetTopCardsAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(It.IsAny<int>(), It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().GetTopCardsAsync(1, 1);

        Assert.False(result.Success);
        Assert.Equal("Error interno", result.Message);
    }

    // ---------- GetPortfolioDistributionAsync ----------

    /// <summary>Verifica que, si el portfolio no existe o no pertenece al usuario, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task GetPortfolioDistributionAsync_WhenPortfolioDoesNotExist_ShouldReturnErrorResponse()
    {
        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((UserPortfolio?)null);

        var result = await CreateService().GetPortfolioDistributionAsync(1, 1);

        Assert.False(result.Success);
        Assert.Equal("Portfolio no encontrado", result.Message);
    }

    /// <summary>Verifica que, con datos válidos, se agrupe el valor de la cartera por sector calculando correctamente los porcentajes.</summary>
    [Fact]
    public async Task GetPortfolioDistributionAsync_WhenDataIsValid_ShouldReturnDistributionGroupedBySector()
    {
        var techAsset = AssetEntity(1, "AAPL", "Tecnología");
        var energyAsset = AssetEntity(2, "XOM", "Energía");
        var holdings = new List<PortfolioHolding>
        {
            HoldingEntity(techAsset, quantity: 10),
            new() { Id = 2, UserId = 1, PortfolioId = 1, AssetId = energyAsset.Id, Asset = energyAsset, Quantity = 5, AvgPrice = 50 }
        };

        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(1, 1)).ReturnsAsync(PortfolioEntity());
        _portfolioHoldingRepository.Setup(r => r.GetByPortfolioAsync(1)).ReturnsAsync(holdings);
        _marketPriceCacheService.Setup(s => s.GetPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(Prices(("AAPL", 100), ("XOM", 50)));

        var result = await CreateService().GetPortfolioDistributionAsync(1, 1);

        Assert.True(result.Success);
        var data = Assert.IsType<List<DashboardPortfolioDistributionDto>>(result.Data);
        Assert.Equal(2, data.Count);
        Assert.Equal("Tecnología", data[0].Sector);
        Assert.Equal(1000, data[0].Value);
        Assert.Equal(80, data[0].Percentage);
        Assert.Equal("Energía", data[1].Sector);
        Assert.Equal(20, data[1].Percentage);
    }

    /// <summary>Verifica que, cuando la cartera está vacía y el valor total invertido es cero, se devuelva una lista vacía.</summary>
    [Fact]
    public async Task GetPortfolioDistributionAsync_WhenPortfolioIsEmpty_ShouldReturnEmptyList()
    {
        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(1, 1)).ReturnsAsync(PortfolioEntity());
        _portfolioHoldingRepository.Setup(r => r.GetByPortfolioAsync(1)).ReturnsAsync(new List<PortfolioHolding>());

        var result = await CreateService().GetPortfolioDistributionAsync(1, 1);

        Assert.True(result.Success);
        var data = Assert.IsType<List<DashboardPortfolioDistributionDto>>(result.Data);
        Assert.Empty(data);
    }

    /// <summary>Verifica que, ante un fallo del repositorio, se registre el error y se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task GetPortfolioDistributionAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(It.IsAny<int>(), It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().GetPortfolioDistributionAsync(1, 1);

        Assert.False(result.Success);
        Assert.Equal("Error interno", result.Message);
    }

    // ---------- GetRecentNotificationsAsync ----------

    /// <summary>Verifica que, cuando existen notificaciones, se devuelva la lista mapeada con sus datos correspondientes.</summary>
    [Fact]
    public async Task GetRecentNotificationsAsync_WhenNotificationsExist_ShouldReturnMappedList()
    {
        var notifications = new List<Notification>
        {
            new() { Id = 1, AlertId = 1, UserId = 1, Message = "AAPL superó los $150", Price = 150.456m, CreatedAt = DateTime.UtcNow, IsRead = false }
        };

        _notificationRepository.Setup(r => r.GetLatestByUserAsync(1, 5)).ReturnsAsync(notifications);

        var result = await CreateService().GetRecentNotificationsAsync(1);

        Assert.True(result.Success);
        var data = Assert.IsType<List<DashboardRecentNotificationDto>>(result.Data);
        Assert.Single(data);
        Assert.Equal("AAPL superó los $150", data[0].Message);
        Assert.Equal(150.46m, data[0].Price);
        Assert.False(data[0].IsRead);
    }

    /// <summary>Verifica que, cuando el usuario no tiene notificaciones, se devuelva una lista vacía.</summary>
    [Fact]
    public async Task GetRecentNotificationsAsync_WhenUserHasNoNotifications_ShouldReturnEmptyList()
    {
        _notificationRepository.Setup(r => r.GetLatestByUserAsync(1, 5)).ReturnsAsync(new List<Notification>());

        var result = await CreateService().GetRecentNotificationsAsync(1);

        Assert.True(result.Success);
        var data = Assert.IsType<List<DashboardRecentNotificationDto>>(result.Data);
        Assert.Empty(data);
    }

    /// <summary>Verifica que, ante un fallo del repositorio de notificaciones, se registre el error y se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task GetRecentNotificationsAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _notificationRepository.Setup(r => r.GetLatestByUserAsync(It.IsAny<int>(), It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().GetRecentNotificationsAsync(1);

        Assert.False(result.Success);
        Assert.Equal("Error interno", result.Message);
    }

    // ---------- GetPerformanceChartAsync ----------

    /// <summary>Verifica que, si el portfolio no existe o no pertenece al usuario, se devuelva una respuesta de error sin consultar el historial.</summary>
    [Fact]
    public async Task GetPerformanceChartAsync_WhenPortfolioDoesNotExist_ShouldReturnErrorResponse()
    {
        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((UserPortfolio?)null);

        var result = await CreateService().GetPerformanceChartAsync(1, 1, new DashboardPerformanceChartFilterDto { Period = "1M" });

        Assert.False(result.Success);
        Assert.Equal("Portfolio no encontrado", result.Message);
        _portfolioHistoryRepository.Verify(r => r.GetByPortfolioAndDateAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
    }

    /// <summary>Verifica que, cuando no hay datos suficientes de historial de cartera o de los índices, se devuelva un gráfico vacío como respuesta exitosa.</summary>
    [Fact]
    public async Task GetPerformanceChartAsync_WhenHistoryDataIsInsufficient_ShouldReturnEmptyChart()
    {
        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(1, 1)).ReturnsAsync(PortfolioEntity());
        _portfolioHistoryRepository.Setup(r => r.GetByPortfolioAndDateAsync(It.IsAny<int>(), It.IsAny<DateTime>())).ReturnsAsync(new List<PortfolioHistory>());
        _priceHistoryRepository.Setup(r => r.GetBySymbolAndDateAsync(It.IsAny<string>(), It.IsAny<DateTime>())).ReturnsAsync(new List<PriceHistory>());

        var result = await CreateService().GetPerformanceChartAsync(1, 1, new DashboardPerformanceChartFilterDto { Period = "1M" });

        Assert.True(result.Success);
        var data = Assert.IsType<DashboardPerformanceChartDto>(result.Data);
        Assert.Empty(data.Data);
        Assert.Equal(0, data.CurrentValue);
    }

    /// <summary>Verifica que, con datos suficientes y un período diario (por ejemplo "1M"), se construya el gráfico comparando la cartera contra el S&amp;P 500 y el NASDAQ.</summary>
    [Fact]
    public async Task GetPerformanceChartAsync_WhenDataIsValidWithDailyPeriod_ShouldReturnChartWithComparisonData()
    {
        var date1 = new DateTime(2026, 1, 1);
        var date2 = new DateTime(2026, 1, 2);
        var asset = AssetEntity();
        var holdings = new List<PortfolioHolding> { HoldingEntity(asset, quantity: 10) };

        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(1, 1)).ReturnsAsync(PortfolioEntity(currentBalance: 500));
        _portfolioHistoryRepository.Setup(r => r.GetByPortfolioAndDateAsync(It.IsAny<int>(), It.IsAny<DateTime>())).ReturnsAsync(new List<PortfolioHistory> { History(1000, date1), History(1100, date2) });
        _priceHistoryRepository.Setup(r => r.GetBySymbolAndDateAsync("^GSPC", It.IsAny<DateTime>())).ReturnsAsync(new List<PriceHistory> { PriceHistoryEntity("^GSPC", 4000, date1), PriceHistoryEntity("^GSPC", 4100, date2) });
        _priceHistoryRepository.Setup(r => r.GetBySymbolAndDateAsync("^IXIC", It.IsAny<DateTime>())).ReturnsAsync(new List<PriceHistory> { PriceHistoryEntity("^IXIC", 12000, date1), PriceHistoryEntity("^IXIC", 12300, date2) });
        _portfolioHoldingRepository.Setup(r => r.GetByPortfolioAsync(1)).ReturnsAsync(holdings);
        _marketPriceCacheService.Setup(s => s.GetPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(Prices(("AAPL", 100)));

        var result = await CreateService().GetPerformanceChartAsync(1, 1, new DashboardPerformanceChartFilterDto { Period = "1M" });

        Assert.True(result.Success);
        var data = Assert.IsType<DashboardPerformanceChartDto>(result.Data);
        Assert.Equal(2, data.Data.Count);
        Assert.Equal(100, data.Data[0].Portfolio);
        Assert.Equal(110, data.Data[1].Portfolio);
        Assert.Equal(1500, data.CurrentValue);
        Assert.Equal("vs. mes anterior", data.VariationText);
    }

    /// <summary>Verifica que, ante un fallo de algún repositorio, se registre el error y se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task GetPerformanceChartAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(1, 1)).ReturnsAsync(PortfolioEntity());
        _portfolioHistoryRepository.Setup(r => r.GetByPortfolioAndDateAsync(It.IsAny<int>(), It.IsAny<DateTime>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().GetPerformanceChartAsync(1, 1, new DashboardPerformanceChartFilterDto { Period = "1M" });

        Assert.False(result.Success);
        Assert.Equal("Error interno", result.Message);
    }

    // ---------- GetLatestTransactionsAsync ----------

    /// <summary>Verifica que, si el portfolio no existe o no pertenece al usuario, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task GetLatestTransactionsAsync_WhenPortfolioDoesNotExist_ShouldReturnErrorResponse()
    {
        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((UserPortfolio?)null);

        var result = await CreateService().GetLatestTransactionsAsync(1, 1);

        Assert.False(result.Success);
        Assert.Equal("Portfolio no encontrado", result.Message);
    }

    /// <summary>Verifica que, cuando existen operaciones, se devuelva la lista mapeada con el símbolo, el tipo en español y el total de cada operación.</summary>
    [Fact]
    public async Task GetLatestTransactionsAsync_WhenTransactionsExist_ShouldReturnMappedList()
    {
        var asset = AssetEntity();
        var transactions = new List<Transaction>
        {
            new() { Id = 1, UserId = 1, PortfolioId = 1, AssetId = asset.Id, Asset = asset, Type = TransactionType.Buy, Quantity = 5, Price = 100, Total = 500.456m, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, UserId = 1, PortfolioId = 1, AssetId = asset.Id, Asset = asset, Type = TransactionType.Sell, Quantity = 2, Price = 110, Total = 220, CreatedAt = DateTime.UtcNow }
        };

        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(1, 1)).ReturnsAsync(PortfolioEntity());
        _transactionRepository.Setup(r => r.GetLatestByPortfolioAsync(1, 5)).ReturnsAsync(transactions);

        var result = await CreateService().GetLatestTransactionsAsync(1, 1);

        Assert.True(result.Success);
        var data = Assert.IsType<List<DashboardLatestTransactionDto>>(result.Data);
        Assert.Equal(2, data.Count);
        Assert.Equal("AAPL", data[0].AssetSymbol);
        Assert.Equal("Compra", data[0].Type);
        Assert.Equal(500.46m, data[0].Total);
        Assert.Equal("Venta", data[1].Type);
    }

    /// <summary>Verifica que, cuando el portfolio no tiene operaciones registradas, se devuelva una lista vacía.</summary>
    [Fact]
    public async Task GetLatestTransactionsAsync_WhenPortfolioHasNoTransactions_ShouldReturnEmptyList()
    {
        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(1, 1)).ReturnsAsync(PortfolioEntity());
        _transactionRepository.Setup(r => r.GetLatestByPortfolioAsync(1, 5)).ReturnsAsync(new List<Transaction>());

        var result = await CreateService().GetLatestTransactionsAsync(1, 1);

        Assert.True(result.Success);
        var data = Assert.IsType<List<DashboardLatestTransactionDto>>(result.Data);
        Assert.Empty(data);
    }

    /// <summary>Verifica que, ante un fallo del repositorio de operaciones, se registre el error y se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task GetLatestTransactionsAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(1, 1)).ReturnsAsync(PortfolioEntity());
        _transactionRepository.Setup(r => r.GetLatestByPortfolioAsync(It.IsAny<int>(), It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().GetLatestTransactionsAsync(1, 1);

        Assert.False(result.Success);
        Assert.Equal("Error interno", result.Message);
    }

    // ---------- GetPortfolioCompositionAsync ----------

    /// <summary>Verifica que, si el portfolio no existe o no pertenece al usuario, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task GetPortfolioCompositionAsync_WhenPortfolioDoesNotExist_ShouldReturnErrorResponse()
    {
        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((UserPortfolio?)null);

        var result = await CreateService().GetPortfolioCompositionAsync(1, 1, DateTime.UtcNow);

        Assert.False(result.Success);
        Assert.Equal("Portfolio no encontrado", result.Message);
    }

    /// <summary>Verifica que, cuando no existe ningún snapshot, se devuelva una composición con valores en cero.</summary>
    [Fact]
    public async Task GetPortfolioCompositionAsync_WhenNoSnapshotExists_ShouldReturnEmptyComposition()
    {
        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(1, 1)).ReturnsAsync(PortfolioEntity());
        _portfolioHistoryRepository.Setup(r => r.GetOnOrBeforeAsync(1, It.IsAny<DateTime>())).ReturnsAsync((PortfolioHistory?)null);

        var result = await CreateService().GetPortfolioCompositionAsync(1, 1, DateTime.UtcNow);

        Assert.True(result.Success);
        var data = Assert.IsType<DashboardPortfolioCompositionDto>(result.Data);
        Assert.Equal(0, data.TotalValue);
    }

    /// <summary>Verifica que, cuando existe un snapshot, se devuelva la composición con los valores y la fecha efectiva del snapshot.</summary>
    [Fact]
    public async Task GetPortfolioCompositionAsync_WhenSnapshotExists_ShouldReturnComposition()
    {
        var date = new DateTime(2026, 1, 1);
        var snapshot = new PortfolioHistory { UserId = 1, PortfolioId = 1, Date = date, TotalValue = 1500, AvailableBalance = 500, InvestedValue = 1000 };

        _userPortfolioRepository.Setup(r => r.GetByIdAndUserAsync(1, 1)).ReturnsAsync(PortfolioEntity());
        _portfolioHistoryRepository.Setup(r => r.GetOnOrBeforeAsync(1, It.IsAny<DateTime>())).ReturnsAsync(snapshot);

        var result = await CreateService().GetPortfolioCompositionAsync(1, 1, DateTime.UtcNow);

        Assert.True(result.Success);
        var data = Assert.IsType<DashboardPortfolioCompositionDto>(result.Data);
        Assert.Equal(500, data.AvailableBalance);
        Assert.Equal(1000, data.InvestedValue);
        Assert.Equal(1500, data.TotalValue);
        Assert.Equal(date, data.EffectiveDate);
    }
}
