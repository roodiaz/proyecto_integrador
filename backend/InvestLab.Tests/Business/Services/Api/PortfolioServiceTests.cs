using InvestLab.Business.Interfaces;
using InvestLab.Business.Interfaces.Api;
using InvestLab.Business.Services.Api;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models.Documents;
using InvestLab.Models.DTOs.Market;
using InvestLab.Models.DTOs.Portfolio;
using InvestLab.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using static InvestLab.Models.Enums;

namespace InvestLab.Tests.Business.Services.Api;

/// <summary>
/// Pruebas unitarias de <see cref="PortfolioService"/>, cubriendo los métodos invocados desde <c>PortfolioController</c>
/// para la compra y venta de activos, la consulta de posiciones y precios, los resúmenes, gráficos del portfolio y el reinicio de la simulación.
/// </summary>
public class PortfolioServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUserSettingRepository> _userSettingRepository = new();
    private readonly Mock<IAssetRepository> _assetRepository = new();
    private readonly Mock<IPortfolioRepository> _portfolioRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IExternalProvider> _externalProvider = new();
    private readonly Mock<IMarketProviderResolver> _providerResolver = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPortfolioHistoryRepository> _portfolioHistoryRepository = new();
    private readonly Mock<ILogger<PortfolioService>> _logger = new();
    private readonly Mock<IMarketPriceService> _marketPriceService = new();
    private readonly Mock<IMarketMetadataRepository> _marketMetadataRepository = new();
    private readonly Mock<IAssetService> _assetService = new();
    private readonly Mock<IMarketPriceCacheService> _marketPriceCacheService = new();
    private readonly LimitsOptions _limits = new() { MaxOperationsPerDay = 10, InitialBalance = 10000 };

    private PortfolioService CreateService()
    {
        _providerResolver.Setup(x => x.GetProvider()).Returns(_externalProvider.Object);
        return new(_userRepository.Object, _userSettingRepository.Object, _assetRepository.Object, _portfolioRepository.Object, _transactionRepository.Object, _providerResolver.Object, _unitOfWork.Object, _portfolioHistoryRepository.Object, Options.Create(_limits), _logger.Object, _marketPriceService.Object, _marketMetadataRepository.Object, _assetService.Object, _marketPriceCacheService.Object);
    }

    private static User UserEntity(int id = 1, decimal balance = 1000, decimal initialBalance = 10000) => new() { Id = id, Username = "user", Email = "user@test.com", PasswordHash = "hash", Phone = "123", Balance = balance, InitialBalance = initialBalance, PortfolioName = "Mi Portfolio", PortfolioConfigured = true };

    private static UserSetting Settings(int operationsUsedToday = 0) => new() { Id = 1, UserId = 1, OperationsUsedToday = operationsUsedToday };

    private static Asset AssetEntity(int id = 1, string symbol = "AAPL") => new() { Id = id, Symbol = symbol };

    private static Portfolio PortfolioEntity(Asset asset, decimal quantity = 10, decimal avgPrice = 100) => new() { Id = 1, UserId = 1, AssetId = asset.Id, Asset = asset, Quantity = quantity, AvgPrice = avgPrice };

    private static MarketPriceDto Price(string symbol, decimal price = 150) => new() { Symbol = symbol, Price = price };

    private static BuyAssetDto BuyDto(string symbol = "AAPL", decimal quantity = 5) => new() { Symbol = symbol, Quantity = quantity };

    private static SellAssetDto SellDto(string symbol = "AAPL", decimal quantity = 5) => new() { Symbol = symbol, Quantity = quantity };

    private static SetupPortfolioDto SetupDto(string portfolioName = "Mi Portfolio", decimal initialBalance = 15000) => new() { PortfolioName = portfolioName, InitialBalance = initialBalance };

    // ---------- BuyAsync ----------

    /// <summary>Verifica que, si el usuario no existe, se devuelva una respuesta de error sin realizar la compra.</summary>
    [Fact]
    public async Task BuyAsync_WhenUserDoesNotExist_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await CreateService().BuyAsync(1, BuyDto());

        Assert.False(result.Success);
        Assert.Equal("Usuario no encontrado", result.Message);
    }

    /// <summary>Verifica que, si no existe configuración del usuario, se devuelva una respuesta de error sin realizar la compra.</summary>
    [Fact]
    public async Task BuyAsync_WhenUserSettingsDoNotExist_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(UserEntity());
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync((UserSetting?)null);

        var result = await CreateService().BuyAsync(1, BuyDto());

        Assert.False(result.Success);
        Assert.Equal("Configuración no encontrada", result.Message);
    }

    /// <summary>Verifica que, si el usuario alcanzó el límite diario de operaciones, se devuelva una respuesta de error sin realizar la compra.</summary>
    [Fact]
    public async Task BuyAsync_WhenDailyOperationsLimitIsReached_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(UserEntity());
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings(operationsUsedToday: 10));

        var result = await CreateService().BuyAsync(1, BuyDto());

        Assert.False(result.Success);
        Assert.Equal("Límite diario alcanzado", result.Message);
    }

    /// <summary>Verifica que, si el activo no existe ni puede crearse, se devuelva una respuesta de error sin realizar la compra.</summary>
    [Fact]
    public async Task BuyAsync_WhenAssetDoesNotExist_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(UserEntity());
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings());
        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync((Asset?)null);

        var result = await CreateService().BuyAsync(1, BuyDto(symbol: "XXXX"));

        Assert.False(result.Success);
        Assert.Equal("Activo no encontrado", result.Message);
    }

    /// <summary>Verifica que, si no se pudo obtener el precio del activo en el mercado, se devuelva una respuesta de error sin realizar la compra.</summary>
    [Fact]
    public async Task BuyAsync_WhenMarketPriceIsUnavailable_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(UserEntity());
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings());
        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync(AssetEntity());
        _externalProvider.Setup(p => p.GetPriceAsync(It.IsAny<string>())).ReturnsAsync((MarketPriceDto?)null);

        var result = await CreateService().BuyAsync(1, BuyDto());

        Assert.False(result.Success);
        Assert.Equal("No se pudo obtener el precio", result.Message);
    }

    /// <summary>Verifica que, si el saldo del usuario es insuficiente para cubrir la operación, se devuelva una respuesta de error sin realizar la compra.</summary>
    [Fact]
    public async Task BuyAsync_WhenUserHasInsufficientBalance_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(UserEntity(balance: 100));
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings());
        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync(AssetEntity());
        _externalProvider.Setup(p => p.GetPriceAsync(It.IsAny<string>())).ReturnsAsync(Price("AAPL", 150));

        var result = await CreateService().BuyAsync(1, BuyDto(quantity: 5));

        Assert.False(result.Success);
        Assert.Equal("Saldo insuficiente. Podés vender activos o reiniciar tu portfolio simulado.", result.Message);
        _portfolioRepository.Verify(r => r.InsertAsync(It.IsAny<Portfolio>()), Times.Never);
    }

    /// <summary>Verifica que, con datos válidos y sin posición previa del activo, se cree una nueva posición, se descuente el saldo y se registre la transacción.</summary>
    [Fact]
    public async Task BuyAsync_WhenDataIsValidAndNoPreviousPosition_ShouldCreatePositionAndDecreaseBalance()
    {
        var user = UserEntity(balance: 1000);
        var settings = Settings(operationsUsedToday: 0);
        var asset = AssetEntity();

        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(settings);
        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync(asset);
        _externalProvider.Setup(p => p.GetPriceAsync(It.IsAny<string>())).ReturnsAsync(Price("AAPL", 150));
        _portfolioRepository.Setup(r => r.GetByUserAndAssetAsync(1, asset.Id)).ReturnsAsync((Portfolio?)null);

        var result = await CreateService().BuyAsync(1, BuyDto(quantity: 5));

        Assert.True(result.Success);
        Assert.Equal("Compra realizada correctamente", result.Message);
        Assert.Equal(250, user.Balance);
        Assert.Equal(1, settings.OperationsUsedToday);
        _portfolioRepository.Verify(r => r.InsertAsync(It.Is<Portfolio>(p => p.Quantity == 5 && p.AvgPrice == 150)), Times.Once);
        _transactionRepository.Verify(r => r.InsertAsync(It.Is<Transaction>(t =>
            t.Type == TransactionType.Buy &&
            t.Total == 750 &&
            t.BalanceBefore == 1000 &&
            t.BalanceAfter == 250)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /// <summary>Verifica que, con datos válidos y una posición previa del activo, se actualice la cantidad y se recalcule el precio promedio ponderado.</summary>
    [Fact]
    public async Task BuyAsync_WhenDataIsValidAndPositionAlreadyExists_ShouldUpdatePositionWithWeightedAveragePrice()
    {
        var user = UserEntity(balance: 5000);
        var asset = AssetEntity();
        var portfolio = PortfolioEntity(asset, quantity: 10, avgPrice: 100);

        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings());
        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync(asset);
        _externalProvider.Setup(p => p.GetPriceAsync(It.IsAny<string>())).ReturnsAsync(Price("AAPL", 150));
        _portfolioRepository.Setup(r => r.GetByUserAndAssetAsync(1, asset.Id)).ReturnsAsync(portfolio);

        var result = await CreateService().BuyAsync(1, BuyDto(quantity: 10));

        Assert.True(result.Success);
        Assert.Equal(20, portfolio.Quantity);
        Assert.Equal(125, portfolio.AvgPrice);
        _portfolioRepository.Verify(r => r.UpdateAsync(portfolio), Times.Once);
        _portfolioRepository.Verify(r => r.InsertAsync(It.IsAny<Portfolio>()), Times.Never);
    }

    /// <summary>Verifica que, ante una excepción inesperada, se registre el error y se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task BuyAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().BuyAsync(1, BuyDto());

        Assert.False(result.Success);
        Assert.Equal("Error interno", result.Message);
    }

    // ---------- SellAsync ----------

    /// <summary>Verifica que, si el usuario no existe, se devuelva una respuesta de error sin realizar la venta.</summary>
    [Fact]
    public async Task SellAsync_WhenUserDoesNotExist_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await CreateService().SellAsync(1, SellDto());

        Assert.False(result.Success);
        Assert.Equal("Usuario no encontrado", result.Message);
    }

    /// <summary>Verifica que, si no existe configuración del usuario, se devuelva una respuesta de error sin realizar la venta.</summary>
    [Fact]
    public async Task SellAsync_WhenUserSettingsDoNotExist_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(UserEntity());
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync((UserSetting?)null);

        var result = await CreateService().SellAsync(1, SellDto());

        Assert.False(result.Success);
        Assert.Equal("Configuración no encontrada", result.Message);
    }

    /// <summary>Verifica que, si el usuario alcanzó el límite diario de operaciones, se devuelva una respuesta de error sin realizar la venta.</summary>
    [Fact]
    public async Task SellAsync_WhenDailyOperationsLimitIsReached_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(UserEntity());
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings(operationsUsedToday: 10));

        var result = await CreateService().SellAsync(1, SellDto());

        Assert.False(result.Success);
        Assert.Equal("Límite diario alcanzado", result.Message);
    }

    /// <summary>Verifica que, si el activo no existe, se devuelva una respuesta de error sin realizar la venta.</summary>
    [Fact]
    public async Task SellAsync_WhenAssetDoesNotExist_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(UserEntity());
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings());
        _assetRepository.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync((Asset?)null);

        var result = await CreateService().SellAsync(1, SellDto(symbol: "XXXX"));

        Assert.False(result.Success);
        Assert.Equal("Activo no encontrado", result.Message);
    }

    /// <summary>Verifica que, si el usuario no posee el activo en su portfolio, se devuelva una respuesta de error sin realizar la venta.</summary>
    [Fact]
    public async Task SellAsync_WhenUserDoesNotOwnTheAsset_ShouldReturnErrorResponse()
    {
        var asset = AssetEntity();

        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(UserEntity());
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings());
        _assetRepository.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync(asset);
        _portfolioRepository.Setup(r => r.GetByUserAndAssetAsync(1, asset.Id)).ReturnsAsync((Portfolio?)null);

        var result = await CreateService().SellAsync(1, SellDto());

        Assert.False(result.Success);
        Assert.Equal("Activo no encontrado en portfolio", result.Message);
    }

    /// <summary>Verifica que, si la cantidad a vender supera la cantidad disponible en el portfolio, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task SellAsync_WhenQuantityIsGreaterThanAvailable_ShouldReturnErrorResponse()
    {
        var asset = AssetEntity();
        var portfolio = PortfolioEntity(asset, quantity: 3);

        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(UserEntity());
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings());
        _assetRepository.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync(asset);
        _portfolioRepository.Setup(r => r.GetByUserAndAssetAsync(1, asset.Id)).ReturnsAsync(portfolio);

        var result = await CreateService().SellAsync(1, SellDto(quantity: 5));

        Assert.False(result.Success);
        Assert.Equal("Cantidad insuficiente", result.Message);
    }

    /// <summary>Verifica que, con datos válidos y vendiendo la totalidad de la posición, se elimine la posición del portfolio, se incremente el saldo y se registre la transacción.</summary>
    [Fact]
    public async Task SellAsync_WhenSellingFullPosition_ShouldDeletePositionAndIncreaseBalance()
    {
        var user = UserEntity(balance: 0);
        var asset = AssetEntity();
        var portfolio = PortfolioEntity(asset, quantity: 5, avgPrice: 100);
        var settings = Settings();

        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(settings);
        _assetRepository.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync(asset);
        _portfolioRepository.Setup(r => r.GetByUserAndAssetAsync(1, asset.Id)).ReturnsAsync(portfolio);
        _externalProvider.Setup(p => p.GetPriceAsync(It.IsAny<string>())).ReturnsAsync(Price("AAPL", 150));

        var result = await CreateService().SellAsync(1, SellDto(quantity: 5));

        Assert.True(result.Success);
        Assert.Equal("Venta realizada correctamente", result.Message);
        Assert.Equal(750, user.Balance);
        Assert.Equal(1, settings.OperationsUsedToday);
        _portfolioRepository.Verify(r => r.DeleteAsync(portfolio), Times.Once);
        _portfolioRepository.Verify(r => r.UpdateAsync(It.IsAny<Portfolio>()), Times.Never);
        _transactionRepository.Verify(r => r.InsertAsync(It.Is<Transaction>(t =>
            t.Type == TransactionType.Sell &&
            t.Total == 750 &&
            t.BalanceBefore == 0 &&
            t.BalanceAfter == 750)), Times.Once);
    }

    /// <summary>Verifica que, con datos válidos y vendiendo una parte de la posición, se actualice la cantidad restante en el portfolio en lugar de eliminarla.</summary>
    [Fact]
    public async Task SellAsync_WhenSellingPartialPosition_ShouldUpdateRemainingQuantity()
    {
        var asset = AssetEntity();
        var portfolio = PortfolioEntity(asset, quantity: 10, avgPrice: 100);

        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(UserEntity());
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings());
        _assetRepository.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync(asset);
        _portfolioRepository.Setup(r => r.GetByUserAndAssetAsync(1, asset.Id)).ReturnsAsync(portfolio);
        _externalProvider.Setup(p => p.GetPriceAsync(It.IsAny<string>())).ReturnsAsync(Price("AAPL", 150));

        var result = await CreateService().SellAsync(1, SellDto(quantity: 4));

        Assert.True(result.Success);
        Assert.Equal(6, portfolio.Quantity);
        _portfolioRepository.Verify(r => r.UpdateAsync(portfolio), Times.Once);
        _portfolioRepository.Verify(r => r.DeleteAsync(It.IsAny<Portfolio>()), Times.Never);
    }

    // ---------- Balance audit (BalanceBefore / BalanceAfter) ----------

    /// <summary>Verifica que la transacción de compra registre correctamente el balance previo y posterior a la operación.</summary>
    [Fact]
    public async Task BuyAsync_WhenPurchaseIsSuccessful_ShouldRecordBalanceBeforeAndAfterInTransaction()
    {
        var user = UserEntity(balance: 10000);
        var asset = AssetEntity();

        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings());
        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync(asset);
        _externalProvider.Setup(p => p.GetPriceAsync(It.IsAny<string>())).ReturnsAsync(Price("AAPL", 122.936m));
        _portfolioRepository.Setup(r => r.GetByUserAndAssetAsync(1, asset.Id)).ReturnsAsync((Portfolio?)null);

        // Qty=5, Price=122.936 → Total=614.68
        await CreateService().BuyAsync(1, BuyDto(quantity: 5));

        _transactionRepository.Verify(r => r.InsertAsync(It.Is<Transaction>(t =>
            t.BalanceBefore == 10000m &&
            t.BalanceAfter  == 10000m - (5 * 122.936m))), Times.Once);
    }

    /// <summary>Verifica que la transacción de venta registre correctamente el balance previo y posterior a la operación.</summary>
    [Fact]
    public async Task SellAsync_WhenSaleIsSuccessful_ShouldRecordBalanceBeforeAndAfterInTransaction()
    {
        var user = UserEntity(balance: 9385.32m);
        var asset = AssetEntity();
        var portfolio = PortfolioEntity(asset, quantity: 5, avgPrice: 122.936m);

        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings());
        _assetRepository.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync(asset);
        _portfolioRepository.Setup(r => r.GetByUserAndAssetAsync(1, asset.Id)).ReturnsAsync(portfolio);
        _externalProvider.Setup(p => p.GetPriceAsync(It.IsAny<string>())).ReturnsAsync(Price("AAPL", 160m));

        // Qty=5, Price=160 → Total=800
        await CreateService().SellAsync(1, SellDto(quantity: 5));

        _transactionRepository.Verify(r => r.InsertAsync(It.Is<Transaction>(t =>
            t.BalanceBefore == 9385.32m &&
            t.BalanceAfter  == 9385.32m + 800m)), Times.Once);
    }

    // ---------- GetPositionForSellAsync ----------

    /// <summary>Verifica que, si el activo no existe, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task GetPositionForSellAsync_WhenAssetDoesNotExist_ShouldReturnErrorResponse()
    {
        _assetRepository.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync((Asset?)null);

        var result = await CreateService().GetPositionForSellAsync(1, "XXXX");

        Assert.False(result.Success);
        Assert.Equal("Activo no encontrado", result.Message);
    }

    /// <summary>Verifica que, si el usuario no tiene una posición abierta del activo, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task GetPositionForSellAsync_WhenPositionDoesNotExist_ShouldReturnErrorResponse()
    {
        var asset = AssetEntity();

        _assetRepository.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync(asset);
        _portfolioRepository.Setup(r => r.GetByUserAndAssetAsync(1, asset.Id)).ReturnsAsync((Portfolio?)null);

        var result = await CreateService().GetPositionForSellAsync(1, "AAPL");

        Assert.False(result.Success);
        Assert.Equal("Posición no encontrada", result.Message);
    }

    /// <summary>Verifica que, si no se pudo obtener el precio actual del activo, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task GetPositionForSellAsync_WhenMarketPriceIsUnavailable_ShouldReturnErrorResponse()
    {
        var asset = AssetEntity();
        var portfolio = PortfolioEntity(asset);

        _assetRepository.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync(asset);
        _portfolioRepository.Setup(r => r.GetByUserAndAssetAsync(1, asset.Id)).ReturnsAsync(portfolio);
        _externalProvider.Setup(p => p.GetPriceAsync(It.IsAny<string>())).ReturnsAsync((MarketPriceDto?)null);

        var result = await CreateService().GetPositionForSellAsync(1, "AAPL");

        Assert.False(result.Success);
        Assert.Equal("No se pudo obtener el precio", result.Message);
    }

    /// <summary>Verifica que, con datos válidos, se devuelva la posición con la cantidad, el precio promedio y el precio actual de mercado.</summary>
    [Fact]
    public async Task GetPositionForSellAsync_WhenDataIsValid_ShouldReturnPositionDetails()
    {
        var asset = AssetEntity();
        var portfolio = PortfolioEntity(asset, quantity: 8, avgPrice: 90);

        _assetRepository.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync(asset);
        _portfolioRepository.Setup(r => r.GetByUserAndAssetAsync(1, asset.Id)).ReturnsAsync(portfolio);
        _externalProvider.Setup(p => p.GetPriceAsync(It.IsAny<string>())).ReturnsAsync(Price("AAPL", 120));

        var result = await CreateService().GetPositionForSellAsync(1, "AAPL");

        Assert.True(result.Success);
        var data = Assert.IsType<PortfolioPositionDto>(result.Data);
        Assert.Equal("AAPL", data.Symbol);
        Assert.Equal(8, data.Quantity);
        Assert.Equal(90, data.AvgPrice);
        Assert.Equal(120, data.CurrentPrice);
    }

    // ---------- GetPriceAsync ----------

    /// <summary>Verifica que, si el activo no existe, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task GetPriceAsync_WhenAssetDoesNotExist_ShouldReturnErrorResponse()
    {
        _assetRepository.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync((Asset?)null);

        var result = await CreateService().GetPriceAsync("XXXX");

        Assert.False(result.Success);
        Assert.Equal("Activo no encontrado", result.Message);
    }

    /// <summary>Verifica que, si no se pudo obtener el precio del activo en el mercado, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task GetPriceAsync_WhenMarketPriceIsUnavailable_ShouldReturnErrorResponse()
    {
        _assetRepository.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync(AssetEntity());
        _externalProvider.Setup(p => p.GetPriceAsync(It.IsAny<string>())).ReturnsAsync((MarketPriceDto?)null);

        var result = await CreateService().GetPriceAsync("AAPL");

        Assert.False(result.Success);
        Assert.Equal("No se pudo obtener el precio", result.Message);
    }

    /// <summary>Verifica que, con datos válidos, se devuelva el símbolo junto con el precio actual de mercado del activo.</summary>
    [Fact]
    public async Task GetPriceAsync_WhenDataIsValid_ShouldReturnCurrentPrice()
    {
        _assetRepository.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync(AssetEntity());
        _externalProvider.Setup(p => p.GetPriceAsync(It.IsAny<string>())).ReturnsAsync(Price("AAPL", 175));

        var result = await CreateService().GetPriceAsync("AAPL");

        Assert.True(result.Success);
        var data = Assert.IsType<AssetPriceDto>(result.Data);
        Assert.Equal("AAPL", data.Symbol);
        Assert.Equal(175, data.CurrentPrice);
    }

    // ---------- GetBalanceCardsAsync ----------

    /// <summary>Verifica que, si el usuario no existe, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task GetBalanceCardsAsync_WhenUserDoesNotExist_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await CreateService().GetBalanceCardsAsync(1);

        Assert.False(result.Success);
        Assert.Equal("Usuario no encontrado", result.Message);
    }

    /// <summary>Verifica que, si no existe configuración del usuario, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task GetBalanceCardsAsync_WhenUserSettingsDoNotExist_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(UserEntity());
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync((UserSetting?)null);

        var result = await CreateService().GetBalanceCardsAsync(1);

        Assert.False(result.Success);
        Assert.Equal("Configuración no encontrada", result.Message);
    }

    /// <summary>Verifica que, con datos válidos, se calcule correctamente el balance actual, la ganancia/pérdida y el porcentaje de rentabilidad.</summary>
    [Fact]
    public async Task GetBalanceCardsAsync_WhenDataIsValid_ShouldReturnComputedBalanceCards()
    {
        var asset = AssetEntity();
        var portfolio = new List<Portfolio> { PortfolioEntity(asset, quantity: 10) };

        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(UserEntity(balance: 9000));
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings(operationsUsedToday: 2));
        _portfolioRepository.Setup(r => r.GetByUserAsync(1)).ReturnsAsync(portfolio);
        _marketPriceCacheService.Setup(p => p.GetPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new MarketPricesResponseDto { Prices = [Price("AAPL", 150)] });
        _marketMetadataRepository.Setup(r => r.GetAsync()).ReturnsAsync((MarketMetadata?)null);

        var result = await CreateService().GetBalanceCardsAsync(1);

        Assert.True(result.Success);
        var data = Assert.IsType<PortfolioBalanceCardsDto>(result.Data);
        Assert.Equal(9000, data.CurrentBalance);
        Assert.Equal(10500, data.TotalBalance);
        Assert.Equal(500, data.ProfitLoss);
        Assert.Equal(5, data.ProfitLossPercent);
        Assert.Equal(500, data.UnrealizedProfitLoss);
        Assert.Equal(0, data.RealizedProfitLoss);
        Assert.Equal(2, data.TotalOperations);
    }

    // ---------- GetPieChartAsync ----------

    /// <summary>Verifica que, cuando el usuario no tiene posiciones en su portfolio, se devuelva una lista vacía.</summary>
    [Fact]
    public async Task GetPieChartAsync_WhenPortfolioIsEmpty_ShouldReturnEmptyList()
    {
        _portfolioRepository.Setup(r => r.GetByUserAsync(1)).ReturnsAsync(new List<Portfolio>());

        var result = await CreateService().GetPieChartAsync(1);

        Assert.True(result.Success);
        var data = Assert.IsType<List<PortfolioPieChartItemDto>>(result.Data);
        Assert.Empty(data);
        _marketPriceCacheService.Verify(p => p.GetPricesAsync(It.IsAny<List<string>>()), Times.Never);
    }

    /// <summary>Verifica que, con datos válidos, se calcule el valor actual y el porcentaje de participación de cada activo dentro del portfolio.</summary>
    [Fact]
    public async Task GetPieChartAsync_WhenDataIsValid_ShouldReturnDistributionByAsset()
    {
        var aapl = AssetEntity(1, "AAPL");
        var msft = AssetEntity(2, "MSFT");
        var portfolio = new List<Portfolio> { PortfolioEntity(aapl, quantity: 10), new() { Id = 2, UserId = 1, AssetId = msft.Id, Asset = msft, Quantity = 5, AvgPrice = 50 } };

        _portfolioRepository.Setup(r => r.GetByUserAsync(1)).ReturnsAsync(portfolio);
        _marketPriceCacheService.Setup(p => p.GetPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new MarketPricesResponseDto { Prices = [Price("AAPL", 100), Price("MSFT", 50)] });

        var result = await CreateService().GetPieChartAsync(1);

        Assert.True(result.Success);
        var data = Assert.IsType<List<PortfolioPieChartItemDto>>(result.Data);
        Assert.Equal(2, data.Count);
        Assert.Equal("AAPL", data[0].Symbol);
        Assert.Equal(1000, data[0].CurrentValue);
        Assert.Equal(80, data[0].Percentage);
        Assert.Equal(20, data[1].Percentage);
    }

    // ---------- GetOpenPositionsAsync ----------

    /// <summary>Verifica que, con datos válidos, se devuelvan las posiciones abiertas con sus variaciones y ganancias/pérdidas calculadas, junto con el total de registros.</summary>
    [Fact]
    public async Task GetOpenPositionsAsync_WhenDataIsValid_ShouldReturnPositionsWithComputedValues()
    {
        var asset = AssetEntity();
        var portfolio = new List<Portfolio> { PortfolioEntity(asset, quantity: 10, avgPrice: 100) };

        _portfolioRepository.Setup(r => r.GetPagedByUserAsync(1)).ReturnsAsync(portfolio);
        _marketPriceCacheService.Setup(p => p.GetPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new MarketPricesResponseDto { Prices = [Price("AAPL", 120)] });

        var result = await CreateService().GetOpenPositionsAsync(1, new PortfolioOpenPositionsFilterDto());

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    /// <summary>Verifica que, al filtrar por símbolo, se devuelvan únicamente las posiciones cuyo símbolo coincida con el filtro indicado.</summary>
    [Fact]
    public async Task GetOpenPositionsAsync_WhenFilteringBySymbol_ShouldReturnOnlyMatchingPositions()
    {
        var aapl = AssetEntity(1, "AAPL");
        var msft = AssetEntity(2, "MSFT");
        var portfolio = new List<Portfolio> { PortfolioEntity(aapl, quantity: 10, avgPrice: 100), new() { Id = 2, UserId = 1, AssetId = msft.Id, Asset = msft, Quantity = 5, AvgPrice = 50 } };

        _portfolioRepository.Setup(r => r.GetPagedByUserAsync(1)).ReturnsAsync(portfolio);
        _marketPriceCacheService.Setup(p => p.GetPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new MarketPricesResponseDto { Prices = [Price("AAPL", 120), Price("MSFT", 60)] });

        var result = await CreateService().GetOpenPositionsAsync(1, new PortfolioOpenPositionsFilterDto { Symbol = "AAPL" });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    // ---------- GetLineChartAsync ----------

    /// <summary>Verifica que, con datos válidos, se devuelva la evolución histórica del valor del portfolio ordenada por fecha.</summary>
    [Fact]
    public async Task GetLineChartAsync_WhenDataIsValid_ShouldReturnPortfolioHistoryOrderedByDate()
    {
        var date1 = new DateTime(2026, 1, 1);
        var date2 = new DateTime(2026, 1, 2);

        _portfolioHistoryRepository.Setup(r => r.GetByUserAndDateAsync(It.IsAny<int>(), It.IsAny<DateTime>())).ReturnsAsync(new List<PortfolioHistory> { new() { UserId = 1, Date = date2, TotalValue = 1100 }, new() { UserId = 1, Date = date1, TotalValue = 1000 } });

        var result = await CreateService().GetLineChartAsync(1, new PortfolioLineChartFilterDto { Period = "7d" });

        Assert.True(result.Success);
        var data = Assert.IsType<List<PortfolioLineChartItemDto>>(result.Data);
        Assert.Equal(2, data.Count);
        Assert.Equal(date1, data[0].Date);
        Assert.Equal(1000, data[0].TotalValue);
    }

    // ---------- ResetSimulationAsync ----------

    /// <summary>Verifica que, si el usuario no existe, se devuelva una respuesta de error sin reiniciar la simulación.</summary>
    [Fact]
    public async Task ResetSimulationAsync_WhenUserDoesNotExist_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await CreateService().ResetSimulationAsync(1, SetupDto());

        Assert.False(result.Success);
        Assert.Equal("Usuario no encontrado", result.Message);
        _portfolioRepository.Verify(r => r.DeleteByUserIdAsync(It.IsAny<int>()), Times.Never);
    }

    /// <summary>Verifica que, con datos válidos, se eliminen el historial, las transacciones y las posiciones, se restablezca el balance inicial y se reinicien las operaciones diarias.</summary>
    [Fact]
    public async Task ResetSimulationAsync_WhenDataIsValid_ShouldClearPortfolioDataAndResetBalance()
    {
        var user = UserEntity();
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var dto = SetupDto(portfolioName: "Nuevo Portfolio", initialBalance: 25000);
        var result = await CreateService().ResetSimulationAsync(1, dto);

        Assert.True(result.Success);
        Assert.Equal("Portfolio reiniciado correctamente", result.Message);
        _portfolioHistoryRepository.Verify(r => r.DeleteByUserIdAsync(1), Times.Once);
        _transactionRepository.Verify(r => r.DeleteByUserIdAsync(1), Times.Once);
        _portfolioRepository.Verify(r => r.DeleteByUserIdAsync(1), Times.Once);
        _userSettingRepository.Verify(r => r.ResetOperationsUsedTodayAsync(1), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        Assert.Equal("Nuevo Portfolio", user.PortfolioName);
        Assert.Equal(25000, user.InitialBalance);
        Assert.Equal(25000, user.Balance);
    }

    /// <summary>Verifica que, ante una excepción inesperada durante el reinicio, se registre el error y se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task ResetSimulationAsync_WhenRepositoryThrows_ShouldReturnErrorResponse()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(UserEntity());
        _portfolioHistoryRepository.Setup(r => r.DeleteByUserIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await CreateService().ResetSimulationAsync(1, SetupDto());

        Assert.False(result.Success);
        Assert.Equal("Error interno", result.Message);
    }
}
