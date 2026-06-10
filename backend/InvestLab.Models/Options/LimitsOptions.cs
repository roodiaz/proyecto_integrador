namespace InvestLab.Models.Options;

public class LimitsOptions
{
    public int MaxAlerts { get; set; }
    public int MaxFavorites { get; set; }
    public int MaxOperationsPerDay { get; set; }
    public int MaxDailySearches { get; set; }

    public decimal InitialBalance { get; set; }

    public int MaxFailedLoginAttempts { get; set; }
    public int LockoutDurationMinutes { get; set; }
}
