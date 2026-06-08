using InvestLab.Workers.Extensions;
using InvestLab.Integrations.DependencyInjection;
using InvestLab.Workers;
using InvestLab.Workers.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Application services
builder.Services.AddWorkerApplicationServices(builder.Configuration);
builder.Services.AddExternalProviders(builder.Configuration);
builder.Services.AddEmailProviders(builder.Configuration);

// Workers
builder.Services.AddHostedService<MarketSeederWorker>();
builder.Services.AddHostedService<DailySnapshotWorker>();
builder.Services.AddHostedService<AlertWorker>();
builder.Services.AddHostedService<UserDailyLimitsResetWorker>();
builder.Services.AddHostedService<MarketHistoryCleanupWorker>();

var host = builder.Build();

host.Run();