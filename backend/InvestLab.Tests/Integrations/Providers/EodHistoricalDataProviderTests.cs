using InvestLab.Integrations.Configuration;
using InvestLab.Integrations.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using static InvestLab.Models.Enums;

namespace InvestLab.Tests.Integrations.Providers;

/// <summary>
/// Pruebas unitarias de <see cref="EodHistoricalDataProvider"/>, cubriendo el comportamiento
/// controlado del proveedor cuando falta la configuración necesaria (ApiKey) para consumir la API.
/// </summary>
public class EodHistoricalDataProviderTests
{
    private readonly Mock<ILogger<EodHistoricalDataProvider>> _logger = new();

    private EodHistoricalDataProvider CreateProvider(string apiKey = "")
    {
        var options = new MarketDataOptions
        {
            Providers = new MarketDataProvidersOptions
            {
                EodHistoricalData = new MarketProviderOptions
                {
                    Enabled = true,
                    BaseUrl = "https://eodhd.com/api",
                    ApiKey = apiKey
                }
            }
        };

        return new EodHistoricalDataProvider(new HttpClient(), Options.Create(options), _logger.Object);
    }

    /// <summary>Verifica que el provider exponga su tipo correctamente para que el resolver pueda identificarlo.</summary>
    [Fact]
    public void ProviderType_ShouldBeEodHistoricalData()
    {
        // Arrange
        var provider = CreateProvider("key");

        // Act
        var providerType = provider.ProviderType;

        // Assert
        Assert.Equal(MarketProviderType.EodHistoricalData, providerType);
    }

    /// <summary>Verifica que, si falta la ApiKey, GetPriceAsync responda de forma controlada sin consultar la API externa.</summary>
    [Fact]
    public async Task GetPriceAsync_WhenApiKeyIsMissing_ShouldReturnNull()
    {
        // Arrange
        var provider = CreateProvider(apiKey: string.Empty);

        // Act
        var result = await provider.GetPriceAsync("AAPL");

        // Assert
        Assert.Null(result);
    }

    /// <summary>Verifica que, si falta la ApiKey, GetHistoricalAsync responda con una lista vacía sin lanzar excepciones.</summary>
    [Fact]
    public async Task GetHistoricalAsync_WhenApiKeyIsMissing_ShouldReturnEmptyList()
    {
        // Arrange
        var provider = CreateProvider(apiKey: string.Empty);

        // Act
        var result = await provider.GetHistoricalAsync("AAPL", DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        // Assert
        Assert.Empty(result);
    }

    /// <summary>Verifica que, si el símbolo es inválido (vacío), el provider no intente consultar la API y devuelva null.</summary>
    [Fact]
    public async Task GetPriceAsync_WhenSymbolIsInvalid_ShouldReturnNull()
    {
        // Arrange
        var provider = CreateProvider(apiKey: "key");

        // Act
        var result = await provider.GetPriceAsync(string.Empty);

        // Assert
        Assert.Null(result);
    }
}
