using InvestLab.Business.Interfaces;
using InvestLab.Models.Options;
using Microsoft.Extensions.Options;
using Resend;

public class EmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly EmailOptions _options;

    /// <summary>
    /// Inicializa una nueva instancia del servicio de correo electrónico.
    /// </summary>
    /// <param name="resend">Cliente utilizado para el envío de correos electrónicos.</param>
    /// <param name="options">Opciones de configuración del correo electrónico.</param>
    public EmailService(IResend resend, IOptions<EmailOptions> options)
    {
        _resend = resend;
        _options = options.Value;
    }

    /// <summary>
    /// Envía un correo electrónico de forma asincrónica al destinatario indicado.
    /// </summary>
    /// <param name="to">Dirección de correo electrónico del destinatario.</param>
    /// <param name="subject">Asunto del correo electrónico.</param>
    /// <param name="body">Cuerpo del correo electrónico en formato HTML.</param>
    public async Task SendAsync(   string to,  string subject,string body)
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