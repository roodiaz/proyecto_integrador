using InvestLab.Api.Extensions;
using InvestLab.Workers.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Application services
builder.Services.AddWorkerApplicationServices(builder.Configuration);

// Workers
builder.Services.AddHostedService<MarketSeederWorker>();
builder.Services.AddHostedService<MarketDailySnapshotWorker>();
builder.Services.AddHostedService<AlertWorker>();

var host = builder.Build();

host.Run();