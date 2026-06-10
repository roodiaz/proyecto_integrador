namespace InvestLab.Models.Options;

public class RateLimitingOptions
{
    public RateLimitRule Login { get; set; } = new();
    public RateLimitRule Register { get; set; } = new();
    public RateLimitRule PasswordRecovery { get; set; } = new();
    public RateLimitRule EmailChange { get; set; } = new();
    public RateLimitRule Contact { get; set; } = new();
}

public class RateLimitRule
{
    public int PermitLimit { get; set; }
    public int WindowMinutes { get; set; }
}
