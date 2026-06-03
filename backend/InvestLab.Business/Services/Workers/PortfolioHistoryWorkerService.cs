using InvestLab.Business.Interfaces;
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
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly IMarketPriceService _marketPriceService;
    private readonly ILogger<PortfolioHistoryWorkerService> _logger;

    public PortfolioHistoryWorkerService(IUserRepository userRepository, IPortfolioRepository portfolioRepository, IPortfolioHistoryRepository portfolioHistoryRepository, IExternalProvider externalProvider, ILogger<PortfolioHistoryWorkerService> logger, IPriceHistoryRepository priceHistoryRepository, IMarketPriceService marketPriceService)
    {
        _userRepository = userRepository;
        _portfolioRepository = portfolioRepository;
        _portfolioHistoryRepository = portfolioHistoryRepository;
        _externalProvider = externalProvider;
        _logger = logger;
        _priceHistoryRepository = priceHistoryRepository;
        _marketPriceService = marketPriceService;
    }

    public async Task GenerateDailySnapshotsAsync()
    {
        // Obtiene todos los usuarios activos del sistema
        var users = await _userRepository.GetAllAsync();

        foreach (var user in users)
        {
            try
            {
                // Evita generar más de un snapshot por día para el mismo usuario
                var exists = await _portfolioHistoryRepository.ExistsByDateAsync(user.Id, DateTime.UtcNow.Date);

                if (exists)
                {
                    _logger.LogInformation("Snapshot ya existe para UserId={UserId}", user.Id);
                    continue;
                }

                // Obtiene las posiciones abiertas del usuario
                var portfolio = await _portfolioRepository.GetByUserAsync(user.Id);

                // Obtiene los símbolos necesarios para calcular el valor actual
                var symbols = portfolio
                    .Select(x => x.Asset.Symbol)
                    .Distinct()
                    .ToList();

                // Obtiene precios desde Mongo y realiza fallback al proveedor externo
                // cuando no existe información histórica
                var pricesBySymbol = await _marketPriceService.GetHistoricalPricesAsync(symbols);

                decimal holdingsValue = 0;

                foreach (var item in portfolio)
                {
                    // Si no existe precio disponible para el símbolo,
                    // se omite del cálculo
                    if (!pricesBySymbol.TryGetValue(item.Asset.Symbol, out var price))
                    {
                        _logger.LogWarning("Sin precio disponible para {Symbol}", item.Asset.Symbol);
                        continue;
                    }

                    // Calcula el valor actual de la posición
                    holdingsValue += item.Quantity * price;
                }

                // Valor total = efectivo disponible + valor de posiciones abiertas
                var totalValue = user.Balance + holdingsValue;

                // Guarda una foto diaria del portfolio para gráficos históricos
                var history = new PortfolioHistory
                {
                    UserId = user.Id,
                    Date = DateTime.UtcNow,
                    TotalValue = Math.Round(totalValue, 2)
                };

                await _portfolioHistoryRepository.InsertAsync(history);

                _logger.LogInformation("Snapshot portfolio generado: UserId={UserId} Valor={TotalValue}", user.Id, totalValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando snapshot portfolio: UserId={UserId}", user.Id);
            }
        }
    }
}