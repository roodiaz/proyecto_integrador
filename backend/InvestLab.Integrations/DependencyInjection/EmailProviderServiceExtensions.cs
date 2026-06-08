using InvestLab.Integrations.Configuration;
using InvestLab.Integrations.Interfaces;
using InvestLab.Integrations.Providers;
using InvestLab.Integrations.Resolvers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Resend;

namespace InvestLab.Integrations.DependencyInjection;

public static class EmailProviderServiceExtensions
{
    /// <summary>
    /// Registra todos los proveedores de envío de correo electrónico (Gmail, Resend, etc.)
    /// junto con el resolver que decide cuál utilizar según "Email:Provider".
    /// Gmail se mantiene como proveedor activo por defecto y como fallback seguro.
    /// </summary>
    public static IServiceCollection AddEmailProviders(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<EmailOptions>(config.GetSection("Email"));

        services.AddResend(options =>
        {
            options.ApiToken = config["Email:Resend:ApiKey"] ?? string.Empty;
        });

        services.AddScoped<IEmailProvider, GmailEmailProvider>();
        services.AddScoped<IEmailProvider, ResendEmailProvider>();

        services.AddScoped<IEmailProviderResolver, EmailProviderResolver>();

        return services;
    }
}
