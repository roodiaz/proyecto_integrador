namespace InvestLab.Integrations.Configuration;

public class EmailOptions
{
    public string Provider { get; set; } = "Gmail";
    public GmailOptions Gmail { get; set; } = new();
    public ResendOptions Resend { get; set; } = new();
}

public class GmailOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ResendOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
}
