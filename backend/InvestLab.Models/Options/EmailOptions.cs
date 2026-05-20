namespace InvestLab.Models.Options;

public class EmailOptions
{
    public string Provider { get; set; } = default!;

    public string ApiKey { get; set; } = default!;

    public string From { get; set; } = default!;
}