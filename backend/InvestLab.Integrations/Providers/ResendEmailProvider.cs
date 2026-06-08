using InvestLab.Integrations.Configuration;
using InvestLab.Integrations.Interfaces;
using Microsoft.Extensions.Options;
using Resend;
using static InvestLab.Models.Enums;

namespace InvestLab.Integrations.Providers;

/// <summary>
/// Proveedor de envío de correo electrónico mediante el servicio Resend.
/// </summary>
public class ResendEmailProvider : IEmailProvider
{
    private readonly IResend _resend;
    private readonly ResendOptions _options;

    public EmailProviderType ProviderType => EmailProviderType.Resend;

    public ResendEmailProvider(IResend resend, IOptions<EmailOptions> options)
    {
        _resend = resend;
        _options = options.Value.Resend;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        var message = new EmailMessage
        {
            From = _options.From,
            Subject = subject,
            HtmlBody = body
        };

        message.To.Add(to);

        await _resend.EmailSendAsync(message);
    }
}
