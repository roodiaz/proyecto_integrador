using InvestLab.Integrations.Configuration;
using InvestLab.Integrations.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static InvestLab.Models.Enums;

namespace InvestLab.Integrations.Resolvers;

/// <summary>
/// Selecciona el proveedor de envío de correo electrónico activo según "Email:Provider".
/// Si el proveedor configurado no existe o no es válido, hace fallback a Gmail.
/// </summary>
public class EmailProviderResolver : IEmailProviderResolver
{
    private readonly List<IEmailProvider> _providers;
    private readonly EmailOptions _options;
    private readonly ILogger<EmailProviderResolver> _logger;

    public EmailProviderResolver(IEnumerable<IEmailProvider> providers, IOptions<EmailOptions> options, ILogger<EmailProviderResolver> logger)
    {
        _providers = providers.ToList();
        _options = options.Value;
        _logger = logger;
    }

    public IEmailProvider GetProvider()
    {
        var fallback = _providers.First(x => x.ProviderType == EmailProviderType.Gmail);
        var providerName = _options.Provider?.Trim();

        if (string.IsNullOrWhiteSpace(providerName))
        {
            _logger.LogWarning("Email:Provider no está configurado, se utiliza Gmail como fallback");
            return fallback;
        }

        if (!Enum.TryParse<EmailProviderType>(providerName, ignoreCase: true, out var providerType))
        {
            _logger.LogWarning("Email:Provider tiene un valor desconocido ({Provider}), se utiliza Gmail como fallback", providerName);
            return fallback;
        }

        var provider = _providers.FirstOrDefault(x => x.ProviderType == providerType);

        if (provider == null)
        {
            _logger.LogWarning("No se encontró un proveedor de correo registrado para {ProviderType}, se utiliza Gmail como fallback", providerType);
            return fallback;
        }

        return provider;
    }
}
