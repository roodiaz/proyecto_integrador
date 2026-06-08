using InvestLab.Api.Extensions;
using InvestLab.Api.Middleware;
using InvestLab.Integrations.DependencyInjection;
using InvestLab.Models.DTOs.Auth;
using InvestLab.Models.Options;
using InvestLab.Workers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Resend;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine(builder.Configuration.GetDebugView());

// ─── CONFIG ────────────────────────────────────────────────────────────────
builder.Configuration.AddEnvironmentVariables();

// ─── SERVICES ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();

builder.Services.AddCustomSwagger();
builder.Services.AddCustomAuthentication(builder.Configuration);
builder.Services.AddCustomHealthChecks(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddExternalProviders(builder.Configuration);
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<LimitsOptions>(builder.Configuration.GetSection("Limits"));
builder.Services.Configure<ContactOptions>(builder.Configuration.GetSection("Contact"));
builder.Services.AddResend(options =>
{
    options.ApiToken = builder.Configuration["Email:ApiKey"]!;
});


builder.Services.AddMemoryCache();
builder.Services.AddHostedService<MarketPriceRefreshWorker>();

// ─── CORS ─────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// ───────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ─── MIDDLEWARE ────────────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

// ─── ENDPOINTS ─────────────────────────────────────────────────────────────
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                service = e.Key,
                status = e.Value.Status.ToString(),
                error = e.Value.Exception?.Message
            })
        });

        await context.Response.WriteAsync(result);
    }
});

app.MapControllers();
app.Run();