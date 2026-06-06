using System.Runtime.InteropServices;
using InvestLab.Business.Interfaces.Workers;

namespace InvestLab.Workers.Workers;

/// <summary>
/// Worker encargado de almacenar el cierre diario del mercado.
///
/// IMPORTANTE:
/// El mercado de EE.UU. (NYSE/Nasdaq) cierra a las 16:00 hora de Nueva York.
/// Debido al Daylight Saving Time (DST), el cierre puede ser:
///
/// - 20:00 UTC (horario de verano)
/// - 21:00 UTC (horario estándar)
///
/// Por este motivo NO se debe utilizar una hora UTC fija.
/// El worker calcula siempre las 16:05 de New York y convierte a UTC.
/// </summary>
public class DailySnapshotWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public DailySnapshotWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var nyTimeZone = GetNewYorkTimeZone();

            var nowUtc = DateTime.UtcNow;
            var nowNy = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, nyTimeZone);

            // Ejecutar 5 minutos después del cierre del mercado (16:05 NY)
            var nextRunNy = nowNy.Date.AddHours(16).AddMinutes(5);

            if (nowNy >= nextRunNy)
                nextRunNy = nextRunNy.AddDays(1);

            var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRunNy, nyTimeZone);

            var delay = nextRunUtc - nowUtc;

            await Task.Delay(delay, stoppingToken);

            using var scope = _serviceProvider.CreateScope();

            var service = scope.ServiceProvider.GetRequiredService<IDailySnapshotWorker>();

            await service.SaveDailyMarketHistoryAsync();
            await service.GenerateDailyPortfolioSnapshotsAsync();

        }
    }

    /// <summary>
    /// Obtiene la zona horaria de New York compatible
    /// con Windows y Linux/Docker.
    /// </summary>
    private static TimeZoneInfo GetNewYorkTimeZone()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")
            : TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    }

    // TODO:
    // Actualmente se ejecuta a las 16:05 NY para asegurar que Yahoo Finance
    // ya haya consolidado el precio de cierre.
    // Si en el futuro se utiliza otro proveedor, validar si requiere
    // mayor tiempo de espera luego del market close.
}