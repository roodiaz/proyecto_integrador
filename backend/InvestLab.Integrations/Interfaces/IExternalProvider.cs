using InvestLab.Models.DTOs;
using InvestLab.Models.DTOs.Market;
using static InvestLab.Models.Enums;

namespace InvestLab.Integrations.Interfaces;

public interface IExternalProvider
{
    MarketProviderType ProviderType { get; }

    Task<MarketPriceDto?> GetPriceAsync(string symbol);

    Task<List<MarketPriceDto>> GetPricesAsync(List<string> symbols);

    Task<List<HistoricalPriceDto>> GetChartHistoryAsync(string symbol, string range);

    Task<List<HistoricalPriceDto>> GetHistoricalAsync( string symbol,DateTime from,DateTime to);

    Task<AssetProfileDto?> GetProfileAsync(string symbol);

    Task<List<MarketMoverDto>> GetMarketMoversAsync(string screenerId, int count);

    Task<List<MarketNewsDto>> GetMarketNewsAsync(int count);
}
