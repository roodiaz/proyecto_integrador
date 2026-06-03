namespace InvestLab.Business.Interfaces
{
    public interface IMarketPriceService
    {
        Task<Dictionary<string, decimal>> GetHistoricalPricesAsync(List<string> symbols);
    }
}
