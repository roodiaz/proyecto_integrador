using InvestLab.Business.Interfaces.Api;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Data.Repositories;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Dashboard;
using Microsoft.Extensions.Logging;
using static InvestLab.Models.Enums;

namespace InvestLab.Business.Services.Api;

public class DashboardService : IDashboardService
{
    private readonly IExternalProvider _externalProvider;
    private readonly ILogger<DashboardService> _logger;

    // Repositorios
    private readonly IUserRepository _userRepository;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IPortfolioHistoryRepository _portfolioHistoryRepository;
    private readonly IAlertRepository _alertRepository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly INotificationRepository _notificationRepository;

    public DashboardService(IUserRepository userRepository, IPortfolioRepository portfolioRepository, IPortfolioHistoryRepository portfolioHistoryRepository, IExternalProvider externalProvider, ILogger<DashboardService> logger, IAlertRepository alertRepository, IPriceHistoryRepository priceHistoryRepository, ITransactionRepository transactionRepository, INotificationRepository notificationRepository)
    {
        _logger = logger;
        _externalProvider = externalProvider;

        _userRepository = userRepository;
        _portfolioRepository = portfolioRepository;
        _portfolioHistoryRepository = portfolioHistoryRepository;
        _alertRepository = alertRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _transactionRepository = transactionRepository;
        _notificationRepository = notificationRepository;
    }

    public async Task<Response> GetTopCardsAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Usuario no encontrado: {UserId}", userId);
                return Response.Fail("Usuario no encontrado");
            }

            var portfolio = await _portfolioRepository.GetByUserAsync(userId);
            decimal holdingsValue = 0;

            foreach (var item in portfolio)
            {
                if (item.Asset == null || string.IsNullOrWhiteSpace(item.Asset.Symbol))
                    continue;

                var market = await _externalProvider.GetPriceAsync(item.Asset.Symbol);
                if (market == null)
                    continue;

                holdingsValue += item.Quantity * market.Price;
            }

            var totalValue = user.Balance + holdingsValue;
            var previous = await _portfolioHistoryRepository.GetPreviousAsync(userId);

            decimal todayProfit = 0;
            decimal todayProfitPercent = 0;

            if (previous != null)
            {
                todayProfit = totalValue - previous.TotalValue;

                if (previous.TotalValue > 0)
                    todayProfitPercent = (todayProfit / previous.TotalValue) * 100;
            }

            var activeAlerts = await _alertRepository.CountByUserAsync(userId);

            var response = new DashboardTopCardsDto
            {
                TotalValue = Math.Round(totalValue, 2),
                TodayProfit = Math.Round(todayProfit, 2),
                TodayProfitPercent = Math.Round(todayProfitPercent, 2),
                ActiveAssets = portfolio.Count(x => x.Quantity > 0),
                ActiveAlerts = activeAlerts
            };

            _logger.LogInformation("Dashboard top cards obtenidas: UserId={UserId}", userId);

            return Response.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener dashboard top cards");
            return Response.Fail("Error interno");
        }
    }

    public async Task<Response> GetPortfolioDistributionAsync(int userId)
    {
        try
        {
            var portfolio = await _portfolioRepository.GetByUserAsync(userId);
            var items = new List<(string Sector, decimal Value)>();

            foreach (var item in portfolio)
            {
                if (item.Asset == null || string.IsNullOrWhiteSpace(item.Asset.Symbol) || item.Quantity <= 0)
                    continue;

                var market = await _externalProvider.GetPriceAsync(item.Asset.Symbol);
                if (market == null)
                    continue;

                var sector = string.IsNullOrWhiteSpace(item.Asset.Sector) ? "Sin sector" : item.Asset.Sector;
                var value = item.Quantity * market.Price;

                items.Add((sector, value));
            }

            var total = items.Sum(x => x.Value);

            if (total <= 0)
                return Response.Ok(new List<DashboardPortfolioDistributionDto>());

            var response = items
                .GroupBy(x => x.Sector)
                .Select(x => new DashboardPortfolioDistributionDto
                {
                    Sector = x.Key,
                    Value = Math.Round(x.Sum(y => y.Value), 2),
                    Percentage = Math.Round((x.Sum(y => y.Value) / total) * 100, 2)
                })
                .OrderByDescending(x => x.Percentage)
                .ToList();

            _logger.LogInformation("Dashboard portfolio distribution obtenido: UserId={UserId}", userId);

            return Response.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener dashboard portfolio distribution");
            return Response.Fail("Error interno");
        }
    }

    public async Task<Response> GetRecentNotificationsAsync(int userId)
    {
        try
        {
            var notifications = await _notificationRepository.GetLatestByUserAsync(userId, 5);

            var response = notifications.Select(x => new DashboardRecentNotificationDto
            {
                Message = x.Message ?? string.Empty,
                Price = Math.Round(x.Price, 2),
                CreatedAt = x.CreatedAt,
                IsRead = x.IsRead
            }).ToList();

            return Response.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener notificaciones recientes");
            return Response.Fail("Error interno");
        }
    }

    public async Task<Response> GetPerformanceChartAsync(int userId, DashboardPerformanceChartFilterDto filter)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return Response.Fail("Usuario no encontrado");

            DateTime fromDate = filter.Period switch
            {
                "1W" => DateTime.UtcNow.AddDays(-7),
                "1M" => DateTime.UtcNow.AddMonths(-1),
                "3M" => DateTime.UtcNow.AddMonths(-3),
                "1Y" => DateTime.UtcNow.AddYears(-1),
                _ => DateTime.UtcNow.AddMonths(-1)
            };

            var portfolioHistory = await _portfolioHistoryRepository.GetByUserAndDateAsync(userId, fromDate);
            var sp500History = await _priceHistoryRepository.GetBySymbolAndDateAsync("^GSPC", fromDate);
            var nasdaqHistory = await _priceHistoryRepository.GetBySymbolAndDateAsync("^IXIC", fromDate);

            if (!portfolioHistory.Any() || !sp500History.Any() || !nasdaqHistory.Any())
                return Response.Ok(new DashboardPerformanceChartDto());

            bool monthlyView = filter.Period is "1Y";

            if (monthlyView)
            {
                portfolioHistory = portfolioHistory
                    .GroupBy(x => new { x.Date.Year, x.Date.Month })
                    .Select(x => x.OrderByDescending(y => y.Date).First())
                    .OrderBy(x => x.Date)
                    .ToList();

                sp500History = sp500History
                    .GroupBy(x => new { x.Date.Year, x.Date.Month })
                    .Select(x => x.OrderByDescending(y => y.Date).First())
                    .OrderBy(x => x.Date)
                    .ToList();

                nasdaqHistory = nasdaqHistory
                    .GroupBy(x => new { x.Date.Year, x.Date.Month })
                    .Select(x => x.OrderByDescending(y => y.Date).First())
                    .OrderBy(x => x.Date)
                    .ToList();
            }
            else
            {
                portfolioHistory = portfolioHistory
                    .OrderBy(x => x.Date)
                    .ToList();

                sp500History = sp500History
                    .OrderBy(x => x.Date)
                    .ToList();
            }

            var portfolioBase = portfolioHistory.First().TotalValue;
            var sp500Base = sp500History.First().Close;
            var nasdaqBase = nasdaqHistory.First().Close;

            var sp500Dictionary = sp500History.ToDictionary(x => x.Date.Date, x => x.Close);
            var nasdaqDictionary = nasdaqHistory.ToDictionary(x => x.Date.Date, x => x.Close);

            var benchmarkDictionary = sp500History.ToDictionary(x => x.Date.Date, x => x.Close);

            var chartData = portfolioHistory
                .Where(x => sp500Dictionary.ContainsKey(x.Date.Date) && nasdaqDictionary.ContainsKey(x.Date.Date))
                .Select(x => new DashboardPerformanceChartPointDto
                {
                    Label = monthlyView ? x.Date.ToString("MM/yyyy") : x.Date.ToString("dd/MM"),
                    Portfolio = Math.Round((x.TotalValue / portfolioBase) * 100, 2),
                    Sp500 = Math.Round((sp500Dictionary[x.Date.Date] / sp500Base) * 100, 2),
                    Nasdaq = Math.Round((nasdaqDictionary[x.Date.Date] / nasdaqBase) * 100, 2)
                })
                .ToList();

            var positions = await _portfolioRepository.GetByUserAsync(userId);

            decimal holdingsValue = 0;

            foreach (var position in positions)
            {
                var market = await _externalProvider.GetPriceAsync(position.Asset.Symbol);
                if (market == null)
                    continue;

                holdingsValue += position.Quantity * market.Price;
            }

            var currentValue = user.Balance + holdingsValue;
            var firstValue = portfolioHistory.First().TotalValue;

            decimal variationPercent = 0;

            if (firstValue > 0)
                variationPercent = ((currentValue - firstValue) / firstValue) * 100;

            var response = new DashboardPerformanceChartDto
            {
                CurrentValue = Math.Round(currentValue, 2),
                VariationPercent = Math.Round(variationPercent, 2),
                Data = chartData,

                VariationText = filter.Period switch
                {
                    "1W" => "vs. semana anterior",
                    "1M" => "vs. mes anterior",
                    "3M" => "vs. 3 meses anteriores",
                    "1Y" => "vs. año anterior",
                    _ => string.Empty
                }
            };

            _logger.LogInformation("Performance chart obtenido para UserId={UserId}", userId);

            return Response.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener performance chart para UserId={UserId}", userId);
            return Response.Fail("Error interno");
        }
    }

    public async Task<Response> GetLatestTransactionsAsync(int userId)
    {
        try
        {
            var transactions = await _transactionRepository.GetLatestByUserAsync(userId, 5);

            var response = transactions.Select(x => new DashboardLatestTransactionDto
            {
                AssetSymbol = x.Asset.Symbol,
                Type = BuildTransactionTypeText(x.Type),
                Total = Math.Round(x.Total, 2)
            }).ToList();

            return Response.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener últimas operaciones");
            return Response.Fail("Error interno");
        }
    }

    //
    // HELPERS
    private static string BuildConditionText(Alert alert)
    {
        return ((AlertOperator)alert.Operator) switch
        {
            AlertOperator.GreaterThan => ">",
            AlertOperator.LessThan => "<",
            AlertOperator.GreaterThanOrEqual => ">=",
            AlertOperator.LessThanOrEqual => "<=",
            AlertOperator.Equal => "=",
            _ => "-"
        };
    }

    private static string BuildTransactionTypeText(TransactionType type)
    {
        return ((TransactionType)type) switch
        {
            TransactionType.Buy => "Compra",
            TransactionType.Sell => "Venta",
            _ => "-"
        };
    }
}