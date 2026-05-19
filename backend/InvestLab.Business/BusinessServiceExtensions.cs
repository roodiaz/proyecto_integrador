using InvestLab.Business;
using InvestLab.Business.Interfaces;
using InvestLab.Business.Services;
using InvestLab.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class BusinessServiceExtensions
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IMarketService, MarketService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();

        // interfaz externa
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        return services;
    }
}