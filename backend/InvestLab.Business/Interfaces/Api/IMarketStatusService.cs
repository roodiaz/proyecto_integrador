using InvestLab.Models.DTOs.Market;

namespace InvestLab.Business.Interfaces.Api
{
    public interface IMarketStatusService
    {
        MarketStatusDto GetStatus();

        bool IsMarketOpen();
    }
}
