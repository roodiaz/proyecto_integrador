using InvestLab.Integrations.Configuration;
using InvestLab.Integrations.Interfaces;
using InvestLab.Integrations.Providers;
using InvestLab.Integrations.Resolvers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvestLab.Integrations.DependencyInjection;

public static class ExternalProviderServiceExtensions
{
    /// <summary>
    /// Registra todos los proveedores externos de datos de mercado (Yahoo, EOD Historical Data, etc.)
    /// junto con el resolver que decide cuál utilizar según "MarketData:DefaultProvider".
    /// Yahoo se mantiene como proveedor activo por defecto y como fallback seguro.
    /// </summary>
    public static IServiceCollection AddExternalProviders(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<MarketDataOptions>(config.GetSection("MarketData"));

        services.AddHttpClient<YahooMarketProvider>();
        services.AddHttpClient<EodHistoricalDataProvider>();

        services.AddScoped<IExternalProvider, YahooMarketProvider>();
        services.AddScoped<IExternalProvider, EodHistoricalDataProvider>();

        services.AddScoped<IMarketProviderResolver, MarketProviderResolver>();

        return services;
    }
}
