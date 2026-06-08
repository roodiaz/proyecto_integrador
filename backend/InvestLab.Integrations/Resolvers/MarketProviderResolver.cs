using InvestLab.Integrations.Configuration;
using InvestLab.Integrations.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static InvestLab.Models.Enums;

namespace InvestLab.Integrations.Resolvers;

/// <summary>
/// Selecciona el proveedor externo de datos de mercado activo según "MarketData:DefaultProvider".
/// Si el proveedor configurado no existe o está deshabilitado, hace fallback a Yahoo.
/// </summary>
public class MarketProviderResolver : IMarketProviderResolver
{
    private readonly List<IExternalProvider> _providers;
    private readonly MarketDataOptions _options;
    private readonly ILogger<MarketProviderResolver> _logger;

    public MarketProviderResolver(IEnumerable<IExternalProvider> providers, IOptions<MarketDataOptions> options, ILogger<MarketProviderResolver> logger)
    {
        _providers = providers.ToList();
        _options = options.Value;
        _logger = logger;
    }

    public IExternalProvider GetProvider()
    {
        var fallback = _providers.First(x => x.ProviderType == MarketProviderType.Yahoo);
        var defaultProviderName = _options.DefaultProvider?.Trim();

        if (string.IsNullOrWhiteSpace(defaultProviderName))
        {
            _logger.LogWarning("MarketData:DefaultProvider no está configurado, se utiliza Yahoo como fallback");
            return fallback;
        }

        if (!Enum.TryParse<MarketProviderType>(defaultProviderName, ignoreCase: true, out var providerType))
        {
            _logger.LogWarning("MarketData:DefaultProvider tiene un valor desconocido ({DefaultProvider}), se utiliza Yahoo como fallback", defaultProviderName);
            return fallback;
        }

        var provider = _providers.FirstOrDefault(x => x.ProviderType == providerType);

        if (provider == null)
        {
            _logger.LogWarning("No se encontró un proveedor registrado para {ProviderType}, se utiliza Yahoo como fallback", providerType);
            return fallback;
        }

        if (!IsEnabled(providerType))
        {
            _logger.LogWarning("El proveedor configurado {ProviderType} está deshabilitado, se utiliza Yahoo como fallback", providerType);
            return fallback;
        }

        return provider;
    }

    private bool IsEnabled(MarketProviderType providerType)
    {
        return providerType switch
        {
            MarketProviderType.Yahoo => _options.Providers.Yahoo.Enabled,
            MarketProviderType.EodHistoricalData => _options.Providers.EodHistoricalData.Enabled,
            _ => false
        };
    }
}
