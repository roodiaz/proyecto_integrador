using InvestLab.Business.Interfaces.Api;
using InvestLab.Business.Services;
using InvestLab.Data;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models.DTOs;
using InvestLab.Models.DTOs.Market;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InvestLab.Tests.Business.Services.Api;

/// <summary>
/// Pruebas unitarias de <see cref="MarketService"/>, cubriendo los métodos invocados desde <c>MarketController</c>
/// para obtener el panorama de mercado, el detalle de activos, tendencias, ganadores, perdedores, noticias,
/// el estado de la caché de precios y los históricos de comparación y de activos.
/// </summary>
public class MarketServiceTests
{
    private readonly Mock<IMarketPriceCacheService> _marketPriceCacheService = new();
    private readonly Mock<IExternalProvider> _externalProvider = new();
    private readonly Mock<IMarketProviderResolver> _providerResolver = new();
    private readonly Mock<IAssetService> _assetService = new();
    private readonly Mock<ILogger<MarketService>> _logger = new();

    private MarketService CreateService()
    {
        _providerResolver.Setup(x => x.GetProvider()).Returns(_externalProvider.Object);
        return new(_providerResolver.Object, _logger.Object, _assetService.Object, _marketPriceCacheService.Object);
    }

    private static MarketPriceDto Price(string symbol, decimal price = 100, decimal previousClose = 95, decimal variationPercent = 5) =>
        new() { Symbol = symbol, Price = price, PreviousClose = previousClose, VariationPercent = variationPercent };

    private static HistoricalPriceDto HistoryPoint(DateTime date, decimal close = 100) =>
        new() { Date = date, Open = close, High = close, Low = close, Close = close, Volume = 1000 };

    // ---------- GetMarketOverviewAsync ----------

    /// <summary>Verifica que, cuando se obtienen los precios de los índices, se devuelva el panorama de mercado mapeado correctamente.</summary>
    [Fact]
    public async Task GetMarketOverviewAsync_WhenPricesAreAvailable_ShouldReturnMarketOverview()
    {
        _marketPriceCacheService.Setup(s => s.GetPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new MarketPricesResponseDto { Prices = new List<MarketPriceDto> { Price("^GSPC", 4500, 4400, 2.27m) } });

        var result = await CreateService().GetMarketOverviewAsync();

        Assert.True(result.Success);
        var data = Assert.IsType<MarketOverviewDto>(result.Data);
        Assert.Single(data.Indices);
        Assert.Equal("S&P 500", data.Indices[0].Name);
        Assert.Equal(100, data.Indices[0].Change);
        Assert.Equal("up", data.Indices[0].Trend);
    }

    /// <summary>Verifica que, cuando no se obtienen precios de los índices, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task GetMarketOverviewAsync_WhenNoPricesAreReturned_ShouldReturnErrorResponse()
    {
        _marketPriceCacheService.Setup(s => s.GetPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new MarketPricesResponseDto { Prices = new List<MarketPriceDto>() });

        var result = await CreateService().GetMarketOverviewAsync();

        Assert.False(result.Success);
        Assert.Equal("No se pudieron obtener los índices del mercado", result.Message);
    }

    /// <summary>Verifica que, ante una excepción del servicio de caché de precios, se registre el error y se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task GetMarketOverviewAsync_WhenCacheServiceThrows_ShouldReturnErrorResponse()
    {
        _marketPriceCacheService.Setup(s => s.GetPricesAsync(It.IsAny<List<string>>())).ThrowsAsync(new Exception("cache error"));

        var result = await CreateService().GetMarketOverviewAsync();

        Assert.False(result.Success);
        Assert.Equal("Ocurrió un error al obtener el panorama de mercado", result.Message);
    }

    // ---------- GetAssetDetailAsync ----------

    /// <summary>Verifica que, si no se indica un símbolo válido, se devuelva una respuesta de error sin consultar el activo.</summary>
    [Fact]
    public async Task GetAssetDetailAsync_WhenSymbolIsEmpty_ShouldReturnErrorResponse()
    {
        var result = await CreateService().GetAssetDetailAsync("   ");

        Assert.False(result.Success);
        Assert.Equal("Debe ingresar un símbolo válido", result.Message);
        _assetService.Verify(s => s.GetOrCreateAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary>Verifica que, si el activo no existe ni puede crearse, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task GetAssetDetailAsync_WhenAssetDoesNotExist_ShouldReturnErrorResponse()
    {
        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync((Asset?)null);

        var result = await CreateService().GetAssetDetailAsync("XXXX");

        Assert.False(result.Success);
        Assert.Equal("Activo no encontrado", result.Message);
    }

    /// <summary>Verifica que, si no se encuentra información de precio para el activo, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task GetAssetDetailAsync_WhenPriceIsNotFound_ShouldReturnErrorResponse()
    {
        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync(new Asset { Id = 1, Symbol = "AAPL" });
        _marketPriceCacheService.Setup(s => s.GetPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new MarketPricesResponseDto { Prices = new List<MarketPriceDto>() });

        var result = await CreateService().GetAssetDetailAsync("AAPL");

        Assert.False(result.Success);
        Assert.Equal("No se encontró información de precio para el activo solicitado", result.Message);
    }

    /// <summary>Verifica que, con datos válidos, se combine la información de precio y de perfil del activo en la respuesta del detalle.</summary>
    [Fact]
    public async Task GetAssetDetailAsync_WhenDataIsValid_ShouldReturnAssetDetailWithProfileData()
    {
        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync(new Asset { Id = 1, Symbol = "AAPL" });
        _marketPriceCacheService.Setup(s => s.GetPricesAsync(It.IsAny<List<string>>())).ReturnsAsync(new MarketPricesResponseDto { Prices = new List<MarketPriceDto> { Price("AAPL", 180, 175) } });
        _externalProvider.Setup(p => p.GetProfileAsync("AAPL")).ReturnsAsync(new AssetProfileDto { Symbol = "AAPL", Name = "Apple Inc.", Sector = "Tecnología" });

        var result = await CreateService().GetAssetDetailAsync("aapl");

        Assert.True(result.Success);
        var data = Assert.IsType<MarketAssetDetailDto>(result.Data);
        Assert.Equal("Apple Inc.", data.Name);
        Assert.Equal("Tecnología", data.Sector);
        Assert.Equal(5, data.Change);
    }

    /// <summary>Verifica que, ante una excepción del proveedor externo, se registre el error y se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task GetAssetDetailAsync_WhenExternalProviderThrows_ShouldReturnErrorResponse()
    {
        _assetService.Setup(s => s.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync(new Asset { Id = 1, Symbol = "AAPL" });
        _marketPriceCacheService.Setup(s => s.GetPricesAsync(It.IsAny<List<string>>())).ThrowsAsync(new Exception("provider error"));

        var result = await CreateService().GetAssetDetailAsync("AAPL");

        Assert.False(result.Success);
        Assert.Equal("Ocurrió un error al obtener el detalle del activo", result.Message);
    }

    // ---------- GetTrendingAsync ----------

    /// <summary>Verifica que, cuando el proveedor externo devuelve datos, se devuelva una respuesta exitosa con los activos en tendencia.</summary>
    [Fact]
    public async Task GetTrendingAsync_WhenDataIsAvailable_ShouldReturnSuccessResponse()
    {
        _externalProvider.Setup(p => p.GetMarketMoversAsync("most_actives", 6)).ReturnsAsync(new List<MarketMoverDto> { new() { Symbol = "AAPL" } });

        var result = await CreateService().GetTrendingAsync();

        Assert.True(result.Success);
        Assert.Equal("Tendencias obtenidas correctamente", result.Message);
        Assert.NotNull(result.Data);
    }

    /// <summary>Verifica que, ante una excepción del proveedor externo, se registre el error y se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task GetTrendingAsync_WhenExternalProviderThrows_ShouldReturnErrorResponse()
    {
        _externalProvider.Setup(p => p.GetMarketMoversAsync(It.IsAny<string>(), It.IsAny<int>())).ThrowsAsync(new Exception("provider error"));

        var result = await CreateService().GetTrendingAsync();

        Assert.False(result.Success);
        Assert.Equal("Ocurrió un error al obtener las tendencias del mercado", result.Message);
    }

    // ---------- GetGainersAsync ----------

    /// <summary>Verifica que, cuando el proveedor externo devuelve datos, se devuelva una respuesta exitosa con los ganadores del día.</summary>
    [Fact]
    public async Task GetGainersAsync_WhenDataIsAvailable_ShouldReturnSuccessResponse()
    {
        _externalProvider.Setup(p => p.GetMarketMoversAsync("day_gainers", 6)).ReturnsAsync(new List<MarketMoverDto> { new() { Symbol = "AAPL" } });

        var result = await CreateService().GetGainersAsync();

        Assert.True(result.Success);
        Assert.Equal("Ganadores obtenidos correctamente", result.Message);
        Assert.NotNull(result.Data);
    }

    /// <summary>Verifica que, ante una excepción del proveedor externo, se registre el error y se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task GetGainersAsync_WhenExternalProviderThrows_ShouldReturnErrorResponse()
    {
        _externalProvider.Setup(p => p.GetMarketMoversAsync(It.IsAny<string>(), It.IsAny<int>())).ThrowsAsync(new Exception("provider error"));

        var result = await CreateService().GetGainersAsync();

        Assert.False(result.Success);
        Assert.Equal("Ocurrió un error al obtener los ganadores del mercado", result.Message);
    }

    // ---------- GetLosersAsync ----------

    /// <summary>Verifica que, cuando el proveedor externo devuelve datos, se devuelva una respuesta exitosa con los perdedores del día.</summary>
    [Fact]
    public async Task GetLosersAsync_WhenDataIsAvailable_ShouldReturnSuccessResponse()
    {
        _externalProvider.Setup(p => p.GetMarketMoversAsync("day_losers", 6)).ReturnsAsync(new List<MarketMoverDto> { new() { Symbol = "AAPL" } });

        var result = await CreateService().GetLosersAsync();

        Assert.True(result.Success);
        Assert.Equal("Perdedores obtenidos correctamente", result.Message);
        Assert.NotNull(result.Data);
    }

    /// <summary>Verifica que, ante una excepción del proveedor externo, se registre el error y se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task GetLosersAsync_WhenExternalProviderThrows_ShouldReturnErrorResponse()
    {
        _externalProvider.Setup(p => p.GetMarketMoversAsync(It.IsAny<string>(), It.IsAny<int>())).ThrowsAsync(new Exception("provider error"));

        var result = await CreateService().GetLosersAsync();

        Assert.False(result.Success);
        Assert.Equal("Ocurrió un error al obtener los perdedores del mercado", result.Message);
    }

    // ---------- GetMarketNewsAsync ----------

    /// <summary>Verifica que, cuando el proveedor externo devuelve datos, se devuelva una respuesta exitosa con las noticias del mercado.</summary>
    [Fact]
    public async Task GetMarketNewsAsync_WhenDataIsAvailable_ShouldReturnSuccessResponse()
    {
        _externalProvider.Setup(p => p.GetMarketNewsAsync(6)).ReturnsAsync(new List<MarketNewsDto> { new() { Id = "1", Title = "Noticia" } });

        var result = await CreateService().GetMarketNewsAsync();

        Assert.True(result.Success);
        Assert.Equal("Noticias obtenidas correctamente", result.Message);
        Assert.NotNull(result.Data);
    }

    /// <summary>Verifica que, ante una excepción del proveedor externo, se registre el error y se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task GetMarketNewsAsync_WhenExternalProviderThrows_ShouldReturnErrorResponse()
    {
        _externalProvider.Setup(p => p.GetMarketNewsAsync(It.IsAny<int>())).ThrowsAsync(new Exception("provider error"));

        var result = await CreateService().GetMarketNewsAsync();

        Assert.False(result.Success);
        Assert.Equal("Ocurrió un error al obtener las noticias del mercado", result.Message);
    }

    // ---------- GetMarketPriceStatusAsync ----------

    /// <summary>Verifica que, con datos válidos, se devuelva la fecha de la última actualización de la caché de precios.</summary>
    [Fact]
    public async Task GetMarketPriceStatusAsync_WhenDataIsValid_ShouldReturnLastUpdatedDate()
    {
        var updatedAt = new DateTime(2026, 1, 1, 10, 0, 0);
        _marketPriceCacheService.Setup(s => s.GetLastUpdatedAtAsync()).ReturnsAsync(updatedAt);

        var result = await CreateService().GetMarketPriceStatusAsync();

        Assert.True(result.Success);
        var data = Assert.IsType<MarketPriceCacheStatusDto>(result.Data);
        Assert.Equal(updatedAt, data.UpdatedAt);
    }

    /// <summary>Verifica que, ante una excepción del servicio de caché de precios, se registre el error y se devuelva una respuesta genérica de error.</summary>
    [Fact]
    public async Task GetMarketPriceStatusAsync_WhenCacheServiceThrows_ShouldReturnErrorResponse()
    {
        _marketPriceCacheService.Setup(s => s.GetLastUpdatedAtAsync()).ThrowsAsync(new Exception("cache error"));

        var result = await CreateService().GetMarketPriceStatusAsync();

        Assert.False(result.Success);
        Assert.Equal("Error interno", result.Message);
    }

    // ---------- GetAssetHistoryAsync ----------

    /// <summary>Verifica que, si no se indica un símbolo válido, se devuelva una respuesta de error sin consultar el histórico.</summary>
    [Fact]
    public async Task GetAssetHistoryAsync_WhenSymbolIsEmpty_ShouldReturnErrorResponse()
    {
        var result = await CreateService().GetAssetHistoryAsync("   ", "1m");

        Assert.False(result.Success);
        Assert.Equal("Debe ingresar un símbolo válido", result.Message);
        _externalProvider.Verify(p => p.GetChartHistoryAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>Verifica que, si el rango indicado no es uno de los valores permitidos, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task GetAssetHistoryAsync_WhenRangeIsInvalid_ShouldReturnErrorResponse()
    {
        var result = await CreateService().GetAssetHistoryAsync("AAPL", "2y");

        Assert.False(result.Success);
        Assert.Equal("Rango inválido. Los valores permitidos son: 1d, 1w, 1m, 3m, 6m, 1y", result.Message);
    }

    /// <summary>Verifica que, si el proveedor externo no devuelve datos históricos, se devuelva una respuesta de error.</summary>
    [Fact]
    public async Task GetAssetHistoryAsync_WhenNoHistoricalDataIsFound_ShouldReturnErrorResponse()
    {
        _externalProvider.Setup(p => p.GetChartHistoryAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<HistoricalPriceDto>());

        var result = await CreateService().GetAssetHistoryAsync("AAPL", "1m");

        Assert.False(result.Success);
        Assert.Equal("No se encontraron datos históricos para el activo", result.Message);
    }

    /// <summary>Verifica que, con datos válidos, se devuelva el histórico del activo mapeado con sus puntos de precio.</summary>
    [Fact]
    public async Task GetAssetHistoryAsync_WhenDataIsValid_ShouldReturnMappedAssetHistory()
    {
        var date = new DateTime(2026, 1, 1);
        _externalProvider.Setup(p => p.GetChartHistoryAsync("AAPL", "1m")).ReturnsAsync(new List<HistoricalPriceDto> { HistoryPoint(date, 180) });

        var result = await CreateService().GetAssetHistoryAsync("aapl", "1M");

        Assert.True(result.Success);
        var data = Assert.IsType<MarketAssetHistoryDto>(result.Data);
        Assert.Equal("AAPL", data.Symbol);
        Assert.Equal("1m", data.Range);
        Assert.Single(data.Series.Points);
        Assert.Equal(180, data.Series.Points[0].Close);
    }

    // ---------- GetComparisonHistoryAsync ----------

    /// <summary>Verifica que, si el rango indicado no es uno de los valores permitidos, se devuelva una respuesta de error sin consultar el proveedor externo.</summary>
    [Fact]
    public async Task GetComparisonHistoryAsync_WhenRangeIsInvalid_ShouldReturnErrorResponse()
    {
        var result = await CreateService().GetComparisonHistoryAsync("2y");

        Assert.False(result.Success);
        Assert.Equal("Rango inválido. Los valores permitidos son: 1d, 1w, 1m, 3m, 6m, 1y", result.Message);
        _externalProvider.Verify(p => p.GetChartHistoryAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>Verifica que, con un rango válido, se devuelva el histórico de comparación con las series de los tres índices principales.</summary>
    [Fact]
    public async Task GetComparisonHistoryAsync_WhenRangeIsValid_ShouldReturnComparisonHistoryWithIndexSeries()
    {
        var date = new DateTime(2026, 1, 1);
        _externalProvider.Setup(p => p.GetChartHistoryAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<HistoricalPriceDto> { HistoryPoint(date) });

        var result = await CreateService().GetComparisonHistoryAsync("1M");

        Assert.True(result.Success);
        var data = Assert.IsType<MarketComparisonHistoryDto>(result.Data);
        Assert.Equal("1m", data.Range);
        Assert.Equal(3, data.Series.Count);
        Assert.Contains(data.Series, s => s.Symbol == "^GSPC" && s.Name == "S&P 500");
        Assert.Contains(data.Series, s => s.Symbol == "^IXIC" && s.Name == "NASDAQ");
        Assert.Contains(data.Series, s => s.Symbol == "^DJI" && s.Name == "Dow Jones");
    }
}
