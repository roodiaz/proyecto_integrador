using InvestLab.Business.Interfaces.Api;
using InvestLab.Business.Services.Api;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Models.DTOs.Favorite;
using InvestLab.Models.DTOs.Market;
using InvestLab.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace InvestLab.Tests.Business.Services.Api;

/// <summary>
/// Pruebas unitarias de <see cref="FavoriteService"/>, cubriendo los métodos invocados desde <c>FavoriteController</c>
/// para listar, agregar, eliminar y consultar la existencia de activos favoritos del usuario.
/// </summary>
public class FavoriteServiceTests
{
    private readonly Mock<IFavoriteRepository> _favoriteRepository = new();
    private readonly Mock<IAssetRepository> _assetRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<FavoriteService>> _logger = new();
    private readonly Mock<IAssetService> _assetService = new();
    private readonly Mock<IUserSettingRepository> _userSettingRepository = new();
    private readonly Mock<IMarketPriceCacheService> _marketPriceCacheService = new();
    private readonly LimitsOptions _limits = new() { MaxFavorites = 5 };

    private FavoriteService CreateService() => new(_favoriteRepository.Object, _assetRepository.Object, _unitOfWork.Object, _logger.Object, Options.Create(_limits), _assetService.Object, _userSettingRepository.Object, _marketPriceCacheService.Object);

    private static UserSetting Settings(int favoritesUsed = 0) => new() { Id = 1, UserId = 1, FavoritesUsed = favoritesUsed };

    private static Asset AssetEntity(int id = 1, string symbol = "AAPL") => new() { Id = id, Symbol = symbol };

    private static Favorite FavoriteEntity(Asset asset) => new() { Id = 1, UserId = 1, AssetId = asset.Id, Asset = asset };

    // ---------- GetAsync ----------

    /// <summary>Verifica que, cuando el usuario tiene favoritos, se devuelva una respuesta exitosa con los datos enriquecidos con precios de mercado.</summary>
    [Fact]
    public async Task GetAsync_WhenUserHasFavorites_ShouldReturnSuccessResponseWithMarketData()
    {
        var asset = AssetEntity();
        var favorites = new List<Favorite> { FavoriteEntity(asset) };

        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings(favoritesUsed: 1));
        _favoriteRepository.Setup(r => r.GetFilteredAsync(1, It.IsAny<FavoriteFilterDto>())).ReturnsAsync(favorites);
        _marketPriceCacheService.Setup(p => p.GetPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new MarketPricesResponseDto { Prices = [new() { Symbol = "AAPL", Price = 150, VariationPercent = 2.5m }] });

        var result = await CreateService().GetAsync(1, new FavoriteFilterDto());

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    /// <summary>Verifica que, cuando el usuario no tiene favoritos, se devuelva una respuesta exitosa con una colección vacía.</summary>
    [Fact]
    public async Task GetAsync_WhenUserHasNoFavorites_ShouldReturnSuccessResponseWithEmptyData()
    {
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings());
        _favoriteRepository.Setup(r => r.GetFilteredAsync(1, It.IsAny<FavoriteFilterDto>())).ReturnsAsync(new List<Favorite>());
        var result = await CreateService().GetAsync(1, new FavoriteFilterDto());

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        _marketPriceCacheService.Verify(p => p.GetPricesAsync(It.IsAny<List<string>>()), Times.Never);
    }

    // ---------- AddAsync ----------

    /// <summary>Verifica que, si no existe configuración del usuario, se devuelva una respuesta de error sin agregar el favorito.</summary>
    [Fact]
    public async Task AddAsync_WhenUserSettingsDoNotExist_ShouldReturnErrorResponse()
    {
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(It.IsAny<int>())).ReturnsAsync((UserSetting?)null);

        var result = await CreateService().AddAsync(1, new AddFavoriteDto { Symbol = "AAPL" });

        Assert.False(result.Success);
        Assert.Equal("Configuración de usuario no encontrada", result.Message);
        _favoriteRepository.Verify(r => r.AddAsync(It.IsAny<Favorite>()), Times.Never);
    }

    /// <summary>Verifica que, si el activo no existe ni puede crearse, se devuelva una respuesta de error sin agregar el favorito.</summary>
    [Fact]
    public async Task AddAsync_WhenAssetDoesNotExist_ShouldReturnErrorResponse()
    {
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings());
        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync((Asset?)null);

        var result = await CreateService().AddAsync(1, new AddFavoriteDto { Symbol = "XXXX" });

        Assert.False(result.Success);
        Assert.Equal("Activo no encontrado", result.Message);
        _favoriteRepository.Verify(r => r.AddAsync(It.IsAny<Favorite>()), Times.Never);
    }

    /// <summary>Verifica que, si el usuario alcanzó el límite máximo de favoritos permitido, se devuelva una respuesta de error sin agregar el favorito.</summary>
    [Fact]
    public async Task AddAsync_WhenUserReachedFavoritesLimit_ShouldReturnErrorResponse()
    {
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings(favoritesUsed: 5));
        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync(AssetEntity());

        var result = await CreateService().AddAsync(1, new AddFavoriteDto { Symbol = "AAPL" });

        Assert.False(result.Success);
        Assert.Equal("Límite de favoritos alcanzado", result.Message);
        _favoriteRepository.Verify(r => r.AddAsync(It.IsAny<Favorite>()), Times.Never);
    }

    /// <summary>Verifica que, si el activo ya se encuentra entre los favoritos del usuario, se devuelva una respuesta de error sin duplicarlo.</summary>
    [Fact]
    public async Task AddAsync_WhenAssetIsAlreadyFavorite_ShouldReturnErrorResponse()
    {
        var asset = AssetEntity();

        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings());
        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync(asset);
        _favoriteRepository.Setup(r => r.ExistsAsync(1, asset.Id)).ReturnsAsync(true);

        var result = await CreateService().AddAsync(1, new AddFavoriteDto { Symbol = "AAPL" });

        Assert.False(result.Success);
        Assert.Equal("El activo ya está en favoritos", result.Message);
        _favoriteRepository.Verify(r => r.AddAsync(It.IsAny<Favorite>()), Times.Never);
    }

    /// <summary>Verifica que, con datos válidos, se agregue el favorito, se incremente el contador de favoritos usados y se confirmen los cambios.</summary>
    [Fact]
    public async Task AddAsync_WhenDataIsValid_ShouldAddFavoriteAndIncrementCounter()
    {
        var asset = AssetEntity();
        var settings = Settings(favoritesUsed: 1);

        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(settings);
        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync(asset);
        _favoriteRepository.Setup(r => r.ExistsAsync(1, asset.Id)).ReturnsAsync(false);

        var result = await CreateService().AddAsync(1, new AddFavoriteDto { Symbol = "AAPL" });

        Assert.True(result.Success);
        Assert.Equal(2, settings.FavoritesUsed);
        _favoriteRepository.Verify(r => r.AddAsync(It.Is<Favorite>(f => f.UserId == 1 && f.AssetId == asset.Id)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ---------- RemoveAsync ----------

    /// <summary>Verifica que, si no existe configuración del usuario, se devuelva una respuesta de error sin eliminar el favorito.</summary>
    [Fact]
    public async Task RemoveAsync_WhenUserSettingsDoNotExist_ShouldReturnErrorResponse()
    {
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(It.IsAny<int>())).ReturnsAsync((UserSetting?)null);

        var result = await CreateService().RemoveAsync(1, "AAPL");

        Assert.False(result.Success);
        Assert.Equal("Configuración de usuario no encontrada", result.Message);
    }

    /// <summary>Verifica que, si el activo no existe, se devuelva una respuesta de error sin eliminar el favorito.</summary>
    [Fact]
    public async Task RemoveAsync_WhenAssetDoesNotExist_ShouldReturnErrorResponse()
    {
        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings());
        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync((Asset?)null);

        var result = await CreateService().RemoveAsync(1, "XXXX");

        Assert.False(result.Success);
        Assert.Equal("Activo no encontrado", result.Message);
    }

    /// <summary>Verifica que, si el activo no está registrado como favorito del usuario, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task RemoveAsync_WhenFavoriteIsNotRegistered_ShouldReturnErrorResponse()
    {
        var asset = AssetEntity();

        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(Settings());
        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync(asset);
        _favoriteRepository.Setup(r => r.GetByUserAndAssetAsync(1, asset.Id)).ReturnsAsync((Favorite?)null);

        var result = await CreateService().RemoveAsync(1, "AAPL");

        Assert.False(result.Success);
        Assert.Equal("Favorito no encontrado", result.Message);
        _favoriteRepository.Verify(r => r.Remove(It.IsAny<Favorite>()), Times.Never);
    }

    /// <summary>Verifica que, con datos válidos, se elimine el favorito, se decremente el contador de favoritos usados y se confirmen los cambios.</summary>
    [Fact]
    public async Task RemoveAsync_WhenDataIsValid_ShouldRemoveFavoriteAndDecrementCounter()
    {
        var asset = AssetEntity();
        var favorite = FavoriteEntity(asset);
        var settings = Settings(favoritesUsed: 1);

        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(settings);
        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync(asset);
        _favoriteRepository.Setup(r => r.GetByUserAndAssetAsync(1, asset.Id)).ReturnsAsync(favorite);

        var result = await CreateService().RemoveAsync(1, "aapl");

        Assert.True(result.Success);
        Assert.Equal(0, settings.FavoritesUsed);
        _favoriteRepository.Verify(r => r.Remove(favorite), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /// <summary>Verifica el caso borde donde el contador de favoritos usados ya está en cero: no debe decrementarse por debajo de ese valor.</summary>
    [Fact]
    public async Task RemoveAsync_WhenFavoritesUsedCounterIsAlreadyZero_ShouldNotDecrementBelowZero()
    {
        var asset = AssetEntity();
        var favorite = FavoriteEntity(asset);
        var settings = Settings(favoritesUsed: 0);

        _userSettingRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(settings);
        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync(asset);
        _favoriteRepository.Setup(r => r.GetByUserAndAssetAsync(1, asset.Id)).ReturnsAsync(favorite);

        var result = await CreateService().RemoveAsync(1, "AAPL");

        Assert.True(result.Success);
        Assert.Equal(0, settings.FavoritesUsed);
    }

    // ---------- ExistsAsync ----------

    /// <summary>Verifica que, si no se indica un símbolo válido, se devuelva una respuesta de error sin consultar el repositorio.</summary>
    [Fact]
    public async Task ExistsAsync_WhenSymbolIsEmpty_ShouldReturnErrorResponse()
    {
        var result = await CreateService().ExistsAsync(1, "   ");

        Assert.False(result.Success);
        Assert.Equal("Debe indicar un símbolo válido", result.Message);
        _assetService.Verify(s => s.GetOrCreateAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary>Verifica que, si el activo no existe, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task ExistsAsync_WhenAssetDoesNotExist_ShouldReturnErrorResponse()
    {
        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync((Asset?)null);

        var result = await CreateService().ExistsAsync(1, "XXXX");

        Assert.False(result.Success);
        Assert.Equal("Activo no encontrado", result.Message);
    }

    /// <summary>Verifica que, cuando el activo está marcado como favorito, se devuelva una respuesta exitosa indicando true.</summary>
    [Fact]
    public async Task ExistsAsync_WhenAssetIsFavorite_ShouldReturnSuccessResponseWithTrue()
    {
        var asset = AssetEntity();

        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync(asset);
        _favoriteRepository.Setup(r => r.ExistsAsync(1, asset.Id)).ReturnsAsync(true);

        var result = await CreateService().ExistsAsync(1, "AAPL");

        Assert.True(result.Success);
        Assert.Equal(true, result.Data);
    }

    /// <summary>Verifica que, cuando el activo no está marcado como favorito, se devuelva una respuesta exitosa indicando false.</summary>
    [Fact]
    public async Task ExistsAsync_WhenAssetIsNotFavorite_ShouldReturnSuccessResponseWithFalse()
    {
        var asset = AssetEntity();

        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync(asset);
        _favoriteRepository.Setup(r => r.ExistsAsync(1, asset.Id)).ReturnsAsync(false);

        var result = await CreateService().ExistsAsync(1, "AAPL");

        Assert.True(result.Success);
        Assert.Equal(false, result.Data);
    }
}
