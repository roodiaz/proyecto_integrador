using InvestLab.Business.Interfaces.Workers;
using InvestLab.Data.Interfaces;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models.Documents;
using Microsoft.Extensions.Logging;

namespace InvestLab.Business.Services.Workers;

public class PortfolioHistoryWorkerService : IPortfolioHistoryWorkerService
{
    private readonly IUserRepository _userRepository;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IPortfolioHistoryRepository _portfolioHistoryRepository;
    private readonly IExternalProvider _externalProvider;
    private readonly ILogger<PortfolioHistoryWorkerService> _logger;

    public PortfolioHistoryWorkerService( IUserRepository userRepository,   IPortfolioRepository portfolioRepository,   IPortfolioHistoryRepository portfolioHistoryRepository, IExternalProvider externalProvider,  ILogger<PortfolioHistoryWorkerService> logger)
    {
        _userRepository = userRepository;
        _portfolioRepository = portfolioRepository;
        _portfolioHistoryRepository = portfolioHistoryRepository;
        _externalProvider = externalProvider;
        _logger = logger;
    }

    public async Task GenerateDailySnapshotsAsync()
    {
        try
        {
            var users = await _userRepository.GetAllAsync();

            foreach (var user in users)
            {
                var portfolio = await _portfolioRepository.GetByUserAsync(user.Id);

                decimal holdingsValue = 0;

                foreach (var item in portfolio)
                {
                    var market = await _externalProvider.GetPriceAsync(item.Asset.Symbol);

                    if (market == null)
                        continue;

                    holdingsValue += item.Quantity * market.Price;
                }

                var totalValue = user.Balance + holdingsValue;

                var history = new PortfolioHistory
                {
                    UserId = user.Id,
                    Date = DateTime.UtcNow,
                    TotalValue = Math.Round(totalValue, 2)
                };

                await _portfolioHistoryRepository.InsertAsync(history);

                _logger.LogInformation( "Snapshot portfolio generado: UserId={UserId}", user.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar snapshots portfolio");
        }
    }
}