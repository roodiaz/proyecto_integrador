namespace InvestLab.Api.Middleware;

/// <summary>
/// Agrega encabezados HTTP de seguridad a todas las respuestas del backend para
/// mitigar ataques comunes del lado del navegador (MIME sniffing, clickjacking,
/// fuga de información vía Referer, acceso a APIs sensibles del navegador e
/// inyección de contenido vía XSS).
///
/// La Content-Security-Policy se omite para Swagger (solo disponible en
/// Development) porque su UI utiliza un script inline para inicializarse.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";

            var isSwagger = context.Request.Path.StartsWithSegments("/swagger");

            if (!isSwagger)
            {
                headers["Content-Security-Policy"] =
                    "default-src 'self'; " +
                    "script-src 'self'; " +
                    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                    "font-src 'self' https://fonts.gstatic.com data:; " +
                    "img-src 'self' data: https:; " +
                    "connect-src 'self'; " +
                    "frame-ancestors 'none'; " +
                    "object-src 'none'; " +
                    "base-uri 'self'; " +
                    "form-action 'self'";
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
