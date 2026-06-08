using InvestLab.Business.Interfaces;
using InvestLab.Business.Interfaces.Api;
using InvestLab.Business.Interfaces.Workers;
using InvestLab.Business.Services;
using InvestLab.Business.Services.Workers;
using Microsoft.Extensions.DependencyInjection;

public static class WorkerBusinessServiceExtensions
{
    /// <summary>
    /// Registra en el contenedor de inyección de dependencias los servicios de negocio utilizados por los workers en segundo plano.
    /// </summary>
    /// <param name="services">Colección de servicios a la que se agregan las dependencias.</param>
    /// <returns>La colección de servicios con los servicios de los workers registrados, permitiendo encadenar llamadas.</returns>
    public static IServiceCollection AddWorkerBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IMarketHistoryService, MarketHistoryService>();
        services.AddScoped<IAlertProcessingService, AlertProcessingService>();
        services.AddScoped<IUserDailyLimitsResetService, UserDailyLimitsResetService>();
        services.AddScoped<IMarketHistoryCleanupService, MarketHistoryCleanupService>();
        services.AddScoped<IDailySnapshotWorker, DailySnapshotService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IMarketPriceService, MarketPriceService>();

        return services;
    }
}