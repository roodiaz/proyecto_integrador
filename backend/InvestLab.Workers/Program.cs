using InvestLab.Api.Extensions;
using InvestLab.Integrations.Configuration;
using InvestLab.Models.Options;
using InvestLab.Workers.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Application services
builder.Services.AddWorkerApplicationServices(builder.Configuration);
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<YahooOptions>(builder.Configuration.GetSection("Yahoo"));

// Workers
builder.Services.AddHostedService<MarketSeederWorker>();
builder.Services.AddHostedService<MarketDailySnapshotWorker>();
builder.Services.AddHostedService<AlertWorker>();
builder.Services.AddHostedService<UserDailyLimitsResetWorker>();
builder.Services.AddHostedService <MarketHistoryCleanupWorker>();
builder.Services.AddHostedService <PortfolioHistoryWorker>();

var host = builder.Build();

host.Run();