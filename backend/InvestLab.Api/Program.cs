using InvestLab.Api.Middleware;
using InvestLab.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ─── CONFIG ────────────────────────────────────────────────────────────────
builder.Configuration.AddEnvironmentVariables();

// ─── SERVICES ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();

builder.Services.AddCustomSwagger();
builder.Services.AddCustomAuthentication(builder.Configuration);
builder.Services.AddCustomHealthChecks(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);

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
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();