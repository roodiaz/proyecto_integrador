using static InvestLab.Models.Enums;

namespace InvestLab.Integrations.Interfaces;

public interface IEmailProvider
{
    EmailProviderType ProviderType { get; }

    Task SendAsync(string to, string subject, string body);
}
