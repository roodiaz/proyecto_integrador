using System.Net;
using System.Net.Mail;

public class EmailService : IEmailService
{
    public async Task SendAsync(string to, string subject, string body)
    {
        var smtp = new SmtpClient("smtp.gmail.com", 587)
        {
            Credentials = new NetworkCredential("TU_MAIL", "TU_PASSWORD_APP"),
            EnableSsl = true
        };

        var mail = new MailMessage
        {
            From = new MailAddress("TU_MAIL"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mail.To.Add(to);

        await smtp.SendMailAsync(mail);
    }
}