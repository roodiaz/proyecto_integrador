namespace InvestLab.Integrations.Configuration;

public class MarketDataOptions
{
    public string DefaultProvider { get; set; } = "Yahoo";
    public MarketDataProvidersOptions Providers { get; set; } = new();
}

public class MarketDataProvidersOptions
{
    public MarketProviderOptions Yahoo { get; set; } = new();
    public MarketProviderOptions EodHistoricalData { get; set; } = new();
}

public class MarketProviderOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
