using InvestLab.Data;
using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

public class AssetRepository : IAssetRepository
{
    private readonly InvestLabDbContext _context;

    public AssetRepository(InvestLabDbContext context)
    {
        _context = context;
    }

    public async Task<Asset?> GetBySymbolAsync(string symbol)
    {
        return await _context.Assets
            .FirstOrDefaultAsync(x => x.Symbol == symbol.ToUpper());
    }

    public async Task<List<Asset>> GetAllSymbolsAsync()
    {
        return await _context.Assets .ToListAsync();
    }

    public async Task AddAsync(Asset asset)
    {
        await _context.Assets.AddAsync(asset);
    }
    public async Task<List<Asset>> GetPendingHistoryAsync()
    {
        return await _context.Assets
            .Where(x => !x.HistoryLoaded)
            .ToListAsync();
    }

    public async Task UpdateAsync(Asset asset)
    {
        _context.Assets.Update(asset);

        await Task.CompletedTask;
    }

}
