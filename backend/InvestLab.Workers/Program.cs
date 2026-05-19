using InvestLab.Api.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// Application services
builder.Services.AddWorkerApplicationServices(builder.Configuration);

// Workers
builder.Services.AddHostedService<MarketSeederWorker>();

var host = builder.Build();

host.Run();