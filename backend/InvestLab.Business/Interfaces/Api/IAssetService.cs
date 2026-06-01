using InvestLab.Data;

namespace InvestLab.Business.Interfaces.Api;

public interface IAssetService
{
    Task<Asset?> GetOrCreateAsync(string symbol);
}
