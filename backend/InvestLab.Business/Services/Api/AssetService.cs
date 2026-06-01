using InvestLab.Business.Interfaces.Api;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Integrations.Interfaces;

public class AssetService : IAssetService
{
    private readonly IAssetRepository _assetRepository;
    private readonly IExternalProvider _externalProvider;
    private readonly IUnitOfWork _unitOfWork;

    public AssetService(IAssetRepository assetRepository, IExternalProvider externalProvider, IUnitOfWork unitOfWork)
    {
        _assetRepository = assetRepository;
        _externalProvider = externalProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Asset?> GetOrCreateAsync(string symbol)
    {
        symbol = symbol.Trim().ToUpper();

        var asset = await _assetRepository.GetBySymbolAsync(symbol);
        if (asset != null)
            return asset;

        var profile = await _externalProvider.GetProfileAsync(symbol);
        if (profile == null)
            return null;

        asset = new Asset
        {
            Symbol = profile.Symbol,
            Name = profile.Name,
            Sector = profile.Sector
        };

        await _assetRepository.AddAsync(asset);
        await _unitOfWork.SaveChangesAsync();

        return asset;
    }
}