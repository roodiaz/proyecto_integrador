using InvestLab.Business.Interfaces;
using InvestLab.Business.Interfaces.Api;
using InvestLab.Business.Interfaces.Workers;
using InvestLab.Business.Services;
using InvestLab.Business.Services.Api;
using InvestLab.Business.Services.Workers;
using InvestLab.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

public static class ApiBusinessServiceExtensions
{
    public static IServiceCollection AddApiBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMarketService, MarketService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IMarketPriceService, MarketPriceService>();

        // interfaz externa
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        // servicio segundo plano
        services.AddScoped<IMarketPriceCacheService, MarketPriceCacheService>();
        services.AddScoped<IMarketPriceRefreshService, MarketPriceRefreshService>();

        return services;
    }
}