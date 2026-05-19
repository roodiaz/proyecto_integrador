using InvestLab.Api.Extensions;
using InvestLab.Workers.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Application services
builder.Services.AddWorkerApplicationServices(builder.Configuration);

// Workers
builder.Services.AddHostedService<MarketSeederWorker>();
builder.Services.AddHostedService<MarketDailySnapshotWorker>();

var host = builder.Build();

host.Run();