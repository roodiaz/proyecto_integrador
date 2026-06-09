using InvestLab.Business.Interfaces.Workers;
using System.Runtime.InteropServices;

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
///
/// Al iniciar, el worker ejecuta un catch-up automático:
/// verifica si existe el snapshot del último cierre bursátil y lo genera
/// en caso de que falte (por ejemplo, si el worker estuvo apagado al momento
/// del cierre). Las operaciones realizadas después del cierre quedan excluidas
/// del snapshot del día y se reflejan en el siguiente día bursátil.
/// </summary>
public class DailySnapshotWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailySnapshotWorker> _logger;

    public DailySnapshotWorker(IServiceProvider serviceProvider, ILogger<DailySnapshotWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta el ciclo principal del worker.
    /// Primero realiza un catch-up del último cierre bursátil,
    /// luego entra al loop que espera al próximo cierre (16:05 NY) para generar
    /// el historial diario de mercado y los snapshots de portfolio.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCatchUpAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nyTimeZone = GetNewYorkTimeZone();
                var nowUtc = DateTime.UtcNow;
                var nowNy = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, nyTimeZone);

                var nextRunNy = nowNy.Date.AddHours(16).AddMinutes(5);

                if (nowNy >= nextRunNy)
                    nextRunNy = nextRunNy.AddDays(1);

                while (nextRunNy.DayOfWeek == DayOfWeek.Saturday || nextRunNy.DayOfWeek == DayOfWeek.Sunday)
                    nextRunNy = nextRunNy.AddDays(1);

                var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRunNy, nyTimeZone);
                var delay = nextRunUtc - nowUtc;

                _logger.LogInformation("Worker de snapshots programado para {NextRunNy} NY / {NextRunUtc} UTC", nextRunNy, nextRunUtc);

                await Task.Delay(delay, stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IDailySnapshotWorker>();

                try
                {
                    await service.SaveDailyMarketHistoryAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al guardar el historial diario de mercado");
                }

                try
                {
                    await service.GenerateDailyPortfolioSnapshotsAsync(nextRunUtc);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al generar snapshots diarios de portfolio");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Worker de snapshots diarios cancelado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en el worker de snapshots diarios");

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Worker de snapshots diarios cancelado durante la espera posterior al error");
                }
            }
        }
    }

    /// <summary>
    /// Ejecuta una única vez al iniciar el worker.
    /// Determina el último cierre bursátil ocurrido y garantiza que existan snapshots
    /// para TODOS los días bursátiles faltantes desde la creación de cada usuario.
    /// </summary>
    private async Task RunCatchUpAsync(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested) return;

        try
        {
            var nyTimeZone = GetNewYorkTimeZone();
            var nowUtc = DateTime.UtcNow;
            var nowNy = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, nyTimeZone);

            var lastCloseNy = GetLastMarketCloseNy(nowNy);
            var lastCloseUtc = TimeZoneInfo.ConvertTimeToUtc(lastCloseNy, nyTimeZone);

            _logger.LogInformation("Catch-up histórico iniciado. Último cierre bursátil: {LastCloseNy} NY ({LastCloseUtc} UTC)", lastCloseNy, lastCloseUtc);

            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IDailySnapshotWorker>();

            await service.RunHistoricalCatchUpAsync(lastCloseUtc);

            _logger.LogInformation("Catch-up histórico completado");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el catch-up histórico de snapshots al iniciar el worker");
        }
    }

    /// <summary>
    /// Retorna la fecha/hora NY del último cierre bursátil que ya ocurrió.
    /// Si el mercado ya cerró hoy (día hábil, 16:05+ NY), devuelve las 16:05 de hoy.
    /// En caso contrario, retrocede al último día hábil anterior.
    /// </summary>
    private static DateTime GetLastMarketCloseNy(DateTime nowNy)
    {
        if (nowNy.DayOfWeek != DayOfWeek.Saturday && nowNy.DayOfWeek != DayOfWeek.Sunday)
        {
            var closeToday = nowNy.Date.AddHours(16).AddMinutes(5);
            if (nowNy >= closeToday) return closeToday;
        }

        var candidate = nowNy.Date.AddDays(-1);
        while (candidate.DayOfWeek == DayOfWeek.Saturday || candidate.DayOfWeek == DayOfWeek.Sunday)
            candidate = candidate.AddDays(-1);

        return candidate.AddHours(16).AddMinutes(5);
    }

    /// <summary>
    /// Obtiene la zona horaria de New York compatible con Windows y Linux/Docker.
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
