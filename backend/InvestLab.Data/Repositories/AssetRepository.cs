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
}
