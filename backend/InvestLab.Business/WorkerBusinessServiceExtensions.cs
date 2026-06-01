using InvestLab.Business.Interfaces.Api;
using InvestLab.Business.Interfaces.Workers;
using InvestLab.Business.Services.Workers;
using Microsoft.Extensions.DependencyInjection;

public static class WorkerBusinessServiceExtensions
{
    public static IServiceCollection AddWorkerBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IMarketHistoryService, MarketHistoryService>();
        services.AddScoped<IAlertProcessingService, AlertProcessingService>();
        services.AddScoped<IUserDailyLimitsResetService, UserDailyLimitsResetService>();
        services.AddScoped<IMarketHistoryCleanupService, MarketHistoryCleanupService>();
        services.AddScoped<IPortfolioHistoryWorkerService, PortfolioHistoryWorkerService>();
        services.AddScoped<IAssetService, AssetService>();

        return services;
    }
}