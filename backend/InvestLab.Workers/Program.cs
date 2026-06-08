using InvestLab.Workers.Extensions;
using InvestLab.Integrations.DependencyInjection;
using InvestLab.Models.Options;
using InvestLab.Workers;
using InvestLab.Workers.Workers;
using Resend;

var builder = Host.CreateApplicationBuilder(args);

// Application services
builder.Services.AddWorkerApplicationServices(builder.Configuration);
builder.Services.AddExternalProviders(builder.Configuration);
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddResend(options =>
{
    options.ApiToken = builder.Configuration["Email:ApiKey"]!;
});

// Workers
builder.Services.AddHostedService<MarketSeederWorker>();
builder.Services.AddHostedService<DailySnapshotWorker>();
builder.Services.AddHostedService<AlertWorker>();
builder.Services.AddHostedService<UserDailyLimitsResetWorker>();
builder.Services.AddHostedService<MarketHistoryCleanupWorker>();

var host = builder.Build();

host.Run();