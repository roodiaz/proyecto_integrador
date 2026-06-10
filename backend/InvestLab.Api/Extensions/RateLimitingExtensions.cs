using InvestLab.Models;
using InvestLab.Models.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace InvestLab.Api.Extensions
{
    /// <summary>
    /// Nombres de las políticas de Rate Limiting aplicadas a los endpoints sensibles.
    /// </summary>
    public static class RateLimitPolicies
    {
        public const string Login = "RateLimit_Login";
        public const string Register = "RateLimit_Register";
        public const string PasswordRecovery = "RateLimit_PasswordRecovery";
        public const string EmailChange = "RateLimit_EmailChange";
        public const string Contact = "RateLimit_Contact";
    }

    /// <summary>
    /// Mensajes mostrados al usuario cuando se excede el límite de solicitudes de una política.
    /// </summary>
    internal static class RateLimitMessages
    {
        public static readonly Dictionary<string, string> ByPolicy = new()
        {
            [RateLimitPolicies.Login] = "Has realizado demasiados intentos de inicio de sesión. Intenta nuevamente en unos minutos.",
            [RateLimitPolicies.Register] = "Se alcanzó el límite de registros permitidos. Intenta nuevamente más tarde.",
            [RateLimitPolicies.PasswordRecovery] = "Se alcanzó temporalmente el límite de solicitudes de recuperación de contraseña.",
            [RateLimitPolicies.EmailChange] = "Has realizado demasiadas solicitudes. Intenta nuevamente más tarde.",
            [RateLimitPolicies.Contact] = "Has alcanzado el límite de mensajes permitidos. Intenta nuevamente más tarde."
        };

        public const string Default = "Has realizado demasiadas solicitudes. Intenta nuevamente más tarde.";
    }

    /// <summary>
    /// Resuelve la dirección IP del cliente que origina la solicitud, considerando proxys reversos.
    /// </summary>
    internal static class ClientIpResolver
    {
        public static string GetClientIp(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();

            if (!string.IsNullOrWhiteSpace(forwardedFor))
                return forwardedFor.Split(',')[0].Trim();

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

    public static class RateLimitingExtensions
    {
        /// <summary>
        /// Registra el sistema nativo de Rate Limiting de ASP.NET Core, definiendo políticas
        /// por IP para los endpoints sensibles (login, registro, recuperación de contraseña,
        /// cambio de email y contacto) y un manejador de respuesta 429 acorde a la estructura
        /// de <see cref="Response"/>.
        /// </summary>
        public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services, IConfiguration config)
        {
            var options = config.GetSection("RateLimiting").Get<RateLimitingOptions>() ?? new RateLimitingOptions();

            services.AddRateLimiter(rateLimiterOptions =>
            {
                AddFixedWindowPolicy(rateLimiterOptions, RateLimitPolicies.Login, options.Login);
                AddFixedWindowPolicy(rateLimiterOptions, RateLimitPolicies.Register, options.Register);
                AddFixedWindowPolicy(rateLimiterOptions, RateLimitPolicies.PasswordRecovery, options.PasswordRecovery);
                AddFixedWindowPolicy(rateLimiterOptions, RateLimitPolicies.EmailChange, options.EmailChange);
                AddFixedWindowPolicy(rateLimiterOptions, RateLimitPolicies.Contact, options.Contact);

                rateLimiterOptions.OnRejected = async (context, cancellationToken) =>
                {
                    var policyName = context.HttpContext.GetEndpoint()?
                        .Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;

                    var message = policyName is not null && RateLimitMessages.ByPolicy.TryGetValue(policyName, out var policyMessage)
                        ? policyMessage
                        : RateLimitMessages.Default;

                    var clientIp = ClientIpResolver.GetClientIp(context.HttpContext);
                    var endpoint = context.HttpContext.Request.Path;

                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("RateLimiting");

                    logger.LogWarning(
                        "Rate limit excedido. Política={Policy}, Endpoint={Endpoint}, IP={ClientIp}, Fecha={Timestamp}",
                        policyName ?? "(desconocida)", endpoint, clientIp, DateTime.UtcNow);

                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/json";

                    await context.HttpContext.Response.WriteAsJsonAsync(
                        Response.Fail(message, MessageCodes.RATE_LIMIT_EXCEEDED),
                        cancellationToken);
                };
            });

            return services;
        }

        private static void AddFixedWindowPolicy(RateLimiterOptions rateLimiterOptions, string policyName, RateLimitRule rule)
        {
            rateLimiterOptions.AddPolicy(policyName, httpContext =>
            {
                var clientIp = ClientIpResolver.GetClientIp(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rule.PermitLimit,
                    Window = TimeSpan.FromMinutes(rule.WindowMinutes),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });
        }
    }
}
