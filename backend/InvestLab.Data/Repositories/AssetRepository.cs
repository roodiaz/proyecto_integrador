using InvestLab.Data;
using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

public class AssetRepository : IAssetRepository
{
    private readonly InvestLabDbContext _context;

    /// <summary>
    /// Inicializa una nueva instancia del repositorio de activos.
    /// </summary>
    /// <param name="context">Contexto de base de datos de InvestLab.</param>
    public AssetRepository(InvestLabDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene un activo a partir de su símbolo, comparándolo en mayúsculas.
    /// </summary>
    /// <param name="symbol">Símbolo del activo a buscar.</param>
    /// <returns>El activo encontrado o <c>null</c> si no existe.</returns>
    public async Task<Asset?> GetAsync(string symbol)
    {
        return await _context.Assets
            .FirstOrDefaultAsync(x => x.Symbol == symbol.ToUpper());
    }

    /// <summary>
    /// Obtiene la lista completa de activos almacenados.
    /// </summary>
    /// <returns>Una lista con todos los activos.</returns>
    public async Task<List<Asset>> GetAllAsync()
    {
        return await _context.Assets .ToListAsync();
    }

    /// <summary>
    /// Agrega un nuevo activo al contexto de base de datos.
    /// </summary>
    /// <param name="asset">Activo a agregar.</param>
    public async Task AddAsync(Asset asset)
    {
        await _context.Assets.AddAsync(asset);
    }
    /// <summary>
    /// Obtiene los activos cuyo historial todavía no fue cargado.
    /// </summary>
    /// <returns>Una lista de activos con historial pendiente de carga.</returns>
    public async Task<List<Asset>> GetPendingHistoryAsync()
    {
        return await _context.Assets
            .Where(x => !x.HistoryLoaded)
            .ToListAsync();
    }

    /// <summary>
    /// Marca un activo como modificado en el contexto para su posterior actualización.
    /// </summary>
    /// <param name="asset">Activo con los datos actualizados.</param>
    public async Task UpdateAsync(Asset asset)
    {
        _context.Assets.Update(asset);

        await Task.CompletedTask;
    }

}
