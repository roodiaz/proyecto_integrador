using InvestLab.Integrations.Configuration;
using InvestLab.Integrations.Interfaces;
using InvestLab.Integrations.Resolvers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using static InvestLab.Models.Enums;

namespace InvestLab.Tests.Integrations.Resolvers;

/// <summary>
/// Pruebas unitarias de <see cref="MarketProviderResolver"/>, cubriendo la selección del proveedor
/// activo según configuración y el fallback a Yahoo cuando la configuración es inválida o el
/// proveedor solicitado no está disponible.
/// </summary>
public class MarketProviderResolverTests
{
    private readonly Mock<ILogger<MarketProviderResolver>> _logger = new();

    private static Mock<IExternalProvider> ProviderMock(MarketProviderType type)
    {
        var mock = new Mock<IExternalProvider>();
        mock.Setup(x => x.ProviderType).Returns(type);
        return mock;
    }

    private MarketProviderResolver CreateResolver(MarketDataOptions options, params Mock<IExternalProvider>[] providers)
    {
        return new MarketProviderResolver(providers.Select(x => x.Object), Options.Create(options), _logger.Object);
    }

    /// <summary>Verifica que el resolver devuelva Yahoo cuando es el proveedor por defecto configurado.</summary>
    [Fact]
    public void GetProvider_WhenDefaultProviderIsYahoo_ShouldReturnYahoo()
    {
        // Arrange
        var yahoo = ProviderMock(MarketProviderType.Yahoo);
        var eod = ProviderMock(MarketProviderType.EodHistoricalData);
        var options = new MarketDataOptions
        {
            DefaultProvider = "Yahoo",
            Providers = new MarketDataProvidersOptions
            {
                Yahoo = new MarketProviderOptions { Enabled = true },
                EodHistoricalData = new MarketProviderOptions { Enabled = false }
            }
        };
        var resolver = CreateResolver(options, yahoo, eod);

        // Act
        var provider = resolver.GetProvider();

        // Assert
        Assert.Equal(MarketProviderType.Yahoo, provider.ProviderType);
    }

    /// <summary>Verifica que el resolver devuelva EOD Historical Data cuando está configurado como proveedor por defecto y habilitado.</summary>
    [Fact]
    public void GetProvider_WhenDefaultProviderIsEodAndEnabled_ShouldReturnEod()
    {
        // Arrange
        var yahoo = ProviderMock(MarketProviderType.Yahoo);
        var eod = ProviderMock(MarketProviderType.EodHistoricalData);
        var options = new MarketDataOptions
        {
            DefaultProvider = "EodHistoricalData",
            Providers = new MarketDataProvidersOptions
            {
                Yahoo = new MarketProviderOptions { Enabled = true },
                EodHistoricalData = new MarketProviderOptions { Enabled = true, ApiKey = "key" }
            }
        };
        var resolver = CreateResolver(options, yahoo, eod);

        // Act
        var provider = resolver.GetProvider();

        // Assert
        Assert.Equal(MarketProviderType.EodHistoricalData, provider.ProviderType);
    }

    /// <summary>Verifica que el resolver haga fallback a Yahoo cuando el proveedor configurado no existe entre los registrados.</summary>
    [Fact]
    public void GetProvider_WhenConfiguredProviderDoesNotExist_ShouldFallbackToYahoo()
    {
        // Arrange
        var yahoo = ProviderMock(MarketProviderType.Yahoo);
        var options = new MarketDataOptions
        {
            DefaultProvider = "Unknown",
            Providers = new MarketDataProvidersOptions
            {
                Yahoo = new MarketProviderOptions { Enabled = true }
            }
        };
        var resolver = CreateResolver(options, yahoo);

        // Act
        var provider = resolver.GetProvider();

        // Assert
        Assert.Equal(MarketProviderType.Yahoo, provider.ProviderType);
    }

    /// <summary>Verifica que el resolver haga fallback a Yahoo cuando el proveedor configurado por defecto está deshabilitado.</summary>
    [Fact]
    public void GetProvider_WhenConfiguredProviderIsDisabled_ShouldFallbackToYahoo()
    {
        // Arrange
        var yahoo = ProviderMock(MarketProviderType.Yahoo);
        var eod = ProviderMock(MarketProviderType.EodHistoricalData);
        var options = new MarketDataOptions
        {
            DefaultProvider = "EodHistoricalData",
            Providers = new MarketDataProvidersOptions
            {
                Yahoo = new MarketProviderOptions { Enabled = true },
                EodHistoricalData = new MarketProviderOptions { Enabled = false }
            }
        };
        var resolver = CreateResolver(options, yahoo, eod);

        // Act
        var provider = resolver.GetProvider();

        // Assert
        Assert.Equal(MarketProviderType.Yahoo, provider.ProviderType);
    }

    /// <summary>Verifica que el resolver haga fallback a Yahoo cuando la configuración de MarketData viene vacía.</summary>
    [Fact]
    public void GetProvider_WhenConfigurationIsEmpty_ShouldFallbackToYahoo()
    {
        // Arrange
        var yahoo = ProviderMock(MarketProviderType.Yahoo);
        var eod = ProviderMock(MarketProviderType.EodHistoricalData);
        var options = new MarketDataOptions { DefaultProvider = string.Empty };
        var resolver = CreateResolver(options, yahoo, eod);

        // Act
        var provider = resolver.GetProvider();

        // Assert
        Assert.Equal(MarketProviderType.Yahoo, provider.ProviderType);
    }
}
