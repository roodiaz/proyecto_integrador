using InvestLab.Api.Extensions;
using InvestLab.Integrations.Configuration;
using InvestLab.Integrations.Interfaces;
using InvestLab.Integrations.Providers;
using InvestLab.Models.Options;
using InvestLab.Workers;
using InvestLab.Workers.Workers;
using Resend;

var builder = Host.CreateApplicationBuilder(args);

// Application services
builder.Services.AddWorkerApplicationServices(builder.Configuration);
//builder.Services.AddHttpClient<IExternalProvider, FinnhubMarketProvider>();
builder.Services.AddHttpClient<IExternalProvider, YahooMarketProvider>();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<YahooOptions>(builder.Configuration.GetSection("Yahoo"));
builder.Services.Configure<FinnhubOptions>(builder.Configuration.GetSection("Finnhub"));
builder.Services.AddResend(options =>
{
    options.ApiToken = builder.Configuration["Email:ApiKey"]!;
});

// Workers
builder.Services.AddHostedService<MarketSeederWorker>();
//builder.Services.AddHostedService<DailySnapshotWorker>();
//builder.Services.AddHostedService<AlertWorker>();
//builder.Services.AddHostedService<UserDailyLimitsResetWorker>();
//builder.Services.AddHostedService<MarketHistoryCleanupWorker>();

var host = builder.Build();

host.Run();