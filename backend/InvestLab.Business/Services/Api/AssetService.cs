using InvestLab.Business.Interfaces.Api;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Integrations.Interfaces;

public class AssetService : IAssetService
{
    private readonly IAssetRepository _assetRepository;
    private readonly IMarketProviderResolver _providerResolver;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AssetService"/> inyectando el repositorio de activos, el proveedor externo de datos de mercado y la unidad de trabajo.
    /// </summary>
    /// <param name="assetRepository">Repositorio de activos.</param>
    /// <param name="externalProvider">Proveedor externo de datos de mercado.</param>
    /// <param name="unitOfWork">Unidad de trabajo para confirmar los cambios en la base de datos.</param>
    public AssetService(IAssetRepository assetRepository, IMarketProviderResolver providerResolver, IUnitOfWork unitOfWork)
    {
        _assetRepository = assetRepository;
        _providerResolver = providerResolver;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Busca un activo por su símbolo en el repositorio local; si no existe, obtiene su perfil desde el proveedor externo, lo crea y lo persiste.
    /// </summary>
    /// <param name="symbol">Símbolo del activo a buscar o crear.</param>
    /// <returns>El activo encontrado o creado, o <c>null</c> si no se pudo obtener su perfil desde el proveedor externo.</returns>
    public async Task<Asset?> GetOrCreateAsync(string symbol)
    {
        symbol = symbol.Trim().ToUpper();

        var asset = await _assetRepository.GetAsync(symbol);
        if (asset != null)
            return asset;

        var profile = await _providerResolver.GetProvider().GetProfileAsync(symbol);
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