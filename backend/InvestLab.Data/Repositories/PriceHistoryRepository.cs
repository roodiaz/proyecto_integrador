using InvestLab.Data.Interfaces;
using MongoDB.Driver;

namespace InvestLab.Data.Repositories;

public class PriceHistoryRepository : IPriceHistoryRepository
{
    private readonly IMongoCollection<PriceHistory> _collection;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="PriceHistoryRepository"/> obteniendo la colección de historial de precios de la base de datos.
    /// </summary>
    /// <param name="database">Instancia de la base de datos MongoDB desde donde se obtiene la colección.</param>
    public PriceHistoryRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<PriceHistory>("price_history");
    }

    /// <summary>
    /// Verifica de forma asincrónica si existe al menos un registro de historial de precios para el símbolo indicado.
    /// </summary>
    /// <param name="symbol">Símbolo del activo a verificar.</param>
    /// <returns>Una tarea que representa la operación asincrónica, cuyo resultado es <c>true</c> si existe al menos un registro; de lo contrario, <c>false</c>.</returns>
    public async Task<bool> ExistsAsync(string symbol)
    {
        return await _collection
            .Find(x => x.Symbol == symbol)
            .AnyAsync();
    }

    /// <summary>
    /// Verifica de forma asincrónica si existe un registro de historial de precios para el símbolo indicado en la fecha especificada.
    /// </summary>
    /// <param name="symbol">Símbolo del activo a verificar.</param>
    /// <param name="date">Fecha para la cual se desea comprobar la existencia de un registro (se considera el día completo).</param>
    /// <returns>Una tarea que representa la operación asincrónica, cuyo resultado es <c>true</c> si existe un registro para esa fecha; de lo contrario, <c>false</c>.</returns>
    public async Task<bool> ExistsByDateAsync(string symbol, DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);

        return await _collection
            .Find(x => x.Symbol == symbol && x.Date >= start && x.Date < end)
            .AnyAsync();
    }

    /// <summary>
    /// Obtiene de forma asincrónica la fecha más reciente registrada en el historial de precios para el símbolo indicado.
    /// </summary>
    /// <param name="symbol">Símbolo del activo a consultar.</param>
    /// <returns>Una tarea que representa la operación asincrónica, cuyo resultado es la fecha más reciente encontrada o <c>null</c> si no hay registros.</returns>
    public async Task<DateTime?> GetLatestDateAsync(string symbol)
    {
        var latest = await _collection
            .Find(x => x.Symbol == symbol)
            .SortByDescending(x => x.Date)
            .FirstOrDefaultAsync();

        return latest?.Date;
    }

    /// <summary>
    /// Obtiene de forma asincrónica la fecha más reciente registrada en el historial de precios, considerando todos los símbolos.
    /// </summary>
    /// <returns>Una tarea que representa la operación asincrónica, cuyo resultado es la fecha más reciente encontrada o <c>null</c> si no hay registros.</returns>
    public async Task<DateTime?> GetLatestDateAsync()
    {
        var latest = await _collection
            .Find(_ => true)
            .SortByDescending(x => x.Date)
            .FirstOrDefaultAsync();

        return latest?.Date;
    }

    /// <summary>
    /// Inserta de forma asincrónica un único registro de historial de precios en la colección.
    /// </summary>
    /// <param name="history">Registro de historial de precios a insertar.</param>
    /// <returns>Una tarea que representa la operación asincrónica de inserción.</returns>
    public async Task InsertAsync(PriceHistory history)
    {
        await _collection.InsertOneAsync(history);
    }

    /// <summary>
    /// Inserta de forma asincrónica una lista de registros de historial de precios en la colección, si la lista no está vacía.
    /// </summary>
    /// <param name="history">Lista de registros de historial de precios a insertar.</param>
    /// <returns>Una tarea que representa la operación asincrónica de inserción.</returns>
    public async Task InsertManyAsync(List<PriceHistory> history)
    {
        if (!history.Any())
            return;

        await _collection.InsertManyAsync(history);
    }

    /// <summary>
    /// Elimina de forma asincrónica todos los registros de historial de precios cuya fecha sea anterior a la indicada.
    /// </summary>
    /// <param name="date">Fecha límite; se eliminan los registros con fecha menor a esta.</param>
    /// <returns>Una tarea que representa la operación asincrónica de eliminación.</returns>
    public async Task DeleteOlderThanAsync(DateTime date)
    {
        await _collection.DeleteManyAsync(x => x.Date < date);
    }

    /// <summary>
    /// Obtiene de forma asincrónica el historial de precios de un símbolo a partir de una fecha determinada, ordenado por fecha ascendente.
    /// </summary>
    /// <param name="symbol">Símbolo del activo a consultar.</param>
    /// <param name="fromDate">Fecha desde la cual se incluyen los registros (inclusive).</param>
    /// <returns>Una tarea que representa la operación asincrónica, cuyo resultado es la lista de registros de historial de precios encontrados.</returns>
    public async Task<List<PriceHistory>> GetBySymbolAndDateAsync(string symbol, DateTime fromDate)
    {
        // TODO: revisar fecha hay qe sacar
        return await _collection
            .Find(x => x.Symbol == symbol && x.Date >= fromDate)
            .SortBy(x => x.Date)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene de forma asincrónica el precio más reciente registrado para cada símbolo presente en la colección.
    /// </summary>
    /// <returns>Una tarea que representa la operación asincrónica, cuyo resultado es la lista con el último registro de precio de cada símbolo.</returns>
    public async Task<List<PriceHistory>> GetLatestPricesAsync()
    {
        var pipeline = _collection.Aggregate()
            .SortByDescending(x => x.Date)
            .Group(
                x => x.Symbol,
                g => g.First());

        return await pipeline.ToListAsync();
    }
}