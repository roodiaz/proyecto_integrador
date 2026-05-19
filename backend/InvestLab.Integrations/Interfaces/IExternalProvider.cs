using InvestLab.Models.DTOs.Market;

namespace InvestLab.Integrations.Interfaces;

public interface IExternalProvider
{
    Task<MarketPriceDto?> GetPriceAsync(string symbol);
}
