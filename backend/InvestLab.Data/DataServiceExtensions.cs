using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using InvestLab.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class DataServiceExtensions
{
    public static IServiceCollection AddDataServices(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<InvestLabDbContext>(options =>
            options.UseNpgsql(connectionString));

        // postgreSQL specific configuration
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserTempCredentialRepository, UserTempCredentialRepository>();
        services.AddScoped<IUserSettingRepository, UserSettingRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();

        // mongo specific configuration
        services.AddScoped<IPriceHistoryRepository,PriceHistoryRepository>();
        services.AddScoped<IPortfolioHistoryRepository,PortfolioHistoryRepository>();
        services.AddScoped<IMarketMetadataRepository, MarketMetadataRepository>();

        return services;
    }
}