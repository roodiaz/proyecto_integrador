using System.Net;
using System.Net.Mail;
using InvestLab.Integrations.Configuration;
using InvestLab.Integrations.Interfaces;
using Microsoft.Extensions.Options;
using static InvestLab.Models.Enums;

namespace InvestLab.Integrations.Providers;

/// <summary>
/// Proveedor de envío de correo electrónico mediante el servidor SMTP de Gmail.
/// </summary>
public class GmailEmailProvider : IEmailProvider
{
    private readonly GmailOptions _options;

    public EmailProviderType ProviderType => EmailProviderType.Gmail;

    public GmailEmailProvider(IOptions<EmailOptions> options)
    {
        _options = options.Value.Gmail;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        using var client = new SmtpClient(_options.Host, _options.Port);
        client.EnableSsl = true;
        client.UseDefaultCredentials = false;
        client.Credentials = new NetworkCredential(_options.Username, _options.Password);

        using var message = new MailMessage(_options.Username, to, subject, body)
        {
            IsBodyHtml = true
        };

        await client.SendMailAsync(message);
    }
}
