using InvestLab.Business.Interfaces;
using InvestLab.Models.Options;
using Microsoft.Extensions.Options;
using Resend;

public class EmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly EmailOptions _options;

    public EmailService(IResend resend, IOptions<EmailOptions> options)
    {
        _resend = resend;
        _options = options.Value;
    }

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