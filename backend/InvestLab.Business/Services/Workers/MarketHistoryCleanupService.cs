using InvestLab.Business.Interfaces.Workers;
using InvestLab.Data.Interfaces;

namespace InvestLab.Business.Services.Workers;

/// <summary>
/// Servicio encargado de eliminar
/// históricos viejos de mercado.
/// </summary>
public class MarketHistoryCleanupService: IMarketHistoryCleanupService
{
    private readonly IPriceHistoryRepository  _repository;

    /// <summary>
    /// Inicializa una nueva instancia del servicio de limpieza de históricos de mercado.
    /// </summary>
    /// <param name="repository">Repositorio de históricos de precios utilizado para acceder y eliminar los datos persistidos.</param>
    public MarketHistoryCleanupService(  IPriceHistoryRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Elimina de forma asíncrona los registros históricos de mercado con una antigüedad mayor a un año.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona de limpieza.</returns>
    public async Task CleanupOldHistoryAsync()
    {
        var limitDate = DateTime.UtcNow.AddYears(-1);

        await _repository .DeleteOlderThanAsync(limitDate);
    }
}