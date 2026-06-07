using InvestLab.Business.Interfaces.Api;
using InvestLab.Business.Services.Workers;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using Moq;
using Xunit;

namespace InvestLab.Tests.Business.Services.Workers;

/// <summary>
/// Pruebas unitarias de <see cref="MarketPriceRefreshService"/>, cubriendo la actualización
/// de precios de mercado en caché a partir de los símbolos de los activos registrados.
/// </summary>
public class MarketPriceRefreshServiceTests
{
    private readonly Mock<IAssetRepository> _assetRepository = new();
    private readonly Mock<IMarketPriceCacheService> _marketPriceCacheService = new();

    private MarketPriceRefreshService CreateService() => new(_assetRepository.Object, _marketPriceCacheService.Object);

    private static Asset AssetEntity(int id, string symbol) => new() { Id = id, Symbol = symbol };

    // ---------- RefreshAsync ----------

    /// <summary>Verifica que se obtengan los símbolos únicos y normalizados (en mayúsculas y sin espacios) de los activos registrados, y se solicite la actualización de sus precios en caché.</summary>
    [Fact]
    public async Task RefreshAsync_WhenAssetsExist_ShouldRefreshUniqueNormalizedSymbols()
    {
        var assets = new List<Asset> { AssetEntity(1, "aapl"), AssetEntity(2, " msft "), AssetEntity(3, "AAPL") };
        _assetRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(assets);

        await CreateService().RefreshAsync();

        _marketPriceCacheService.Verify(s => s.RefreshPricesAsync(It.Is<List<string>>(l => l.Count == 2 && l.Contains("AAPL") && l.Contains("MSFT"))), Times.Once);
    }

    /// <summary>Verifica que se ignoren los activos cuyo símbolo es nulo, vacío o contiene únicamente espacios en blanco.</summary>
    [Fact]
    public async Task RefreshAsync_WhenSomeAssetsHaveBlankSymbols_ShouldExcludeThemFromRefresh()
    {
        var assets = new List<Asset> { AssetEntity(1, "AAPL"), AssetEntity(2, "   "), AssetEntity(3, "") };
        _assetRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(assets);

        await CreateService().RefreshAsync();

        _marketPriceCacheService.Verify(s => s.RefreshPricesAsync(It.Is<List<string>>(l => l.Count == 1 && l[0] == "AAPL")), Times.Once);
    }

    /// <summary>Verifica que, si no existen símbolos válidos para actualizar, no se invoque al servicio de caché de precios.</summary>
    [Fact]
    public async Task RefreshAsync_WhenThereAreNoValidSymbols_ShouldNotCallPriceCacheService()
    {
        var assets = new List<Asset> { AssetEntity(1, "   "), AssetEntity(2, "") };
        _assetRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(assets);

        await CreateService().RefreshAsync();

        _marketPriceCacheService.Verify(s => s.RefreshPricesAsync(It.IsAny<List<string>>()), Times.Never);
    }
}
