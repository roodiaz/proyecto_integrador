using InvestLab.Business.Interfaces.Workers;
using InvestLab.Business.Services.Workers;
using Microsoft.Extensions.DependencyInjection;

public static class WorkerBusinessServiceExtensions
{
    public static IServiceCollection AddWorkerBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IMarketHistoryService, MarketHistoryService>();
        services.AddScoped<IMarketHistoryService, MarketHistoryService>();

        return services;
    }
}