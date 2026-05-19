using InvestLab.Business.Interfaces.Workers;

/// <summary>
/// Worker encargado de realizar la carga inicial
/// de históricos de mercado en MongoDB.
/// </summary>
/// <remarks>
/// Este proceso se ejecuta una única vez al iniciar
/// la aplicación y verifica si los activos principales
/// ya poseen histórico almacenado.
/// </remarks>
public class MarketSeederWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public MarketSeederWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IMarketHistoryService>();

        await service.SeedDefaultAssetsAsync();
    }
}