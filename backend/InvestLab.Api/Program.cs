using InvestLab.Api.Extensions;
using InvestLab.Api.Middleware;
using InvestLab.Business;
using InvestLab.Integrations.Configuration;
using InvestLab.Integrations.Interfaces;
using InvestLab.Integrations.Providers;
using InvestLab.Models;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ─── CONFIG ────────────────────────────────────────────────────────────────
builder.Configuration.AddEnvironmentVariables();

// ─── SERVICES ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();

builder.Services.AddCustomSwagger();
builder.Services.AddCustomAuthentication(builder.Configuration);
builder.Services.AddCustomHealthChecks(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddMongoServices(builder.Configuration);
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<LimitsOptions>(builder.Configuration.GetSection("Limits"));
builder.Services.AddHttpClient<IExternalProvider, YahooMarketProvider>();
builder.Services.Configure<YahooOptions>(builder.Configuration.GetSection("Yahoo"));

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