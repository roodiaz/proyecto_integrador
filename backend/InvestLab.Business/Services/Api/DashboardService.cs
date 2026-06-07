using InvestLab.Business.Interfaces.Api;
using InvestLab.Data.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Dashboard;
using InvestLab.Models.DTOs.Market;
using Microsoft.Extensions.Logging;
using static InvestLab.Models.Enums;

namespace InvestLab.Business.Services.Api;

public class DashboardService : IDashboardService
{
    private readonly IMarketPriceCacheService _marketPriceCacheService;
    private readonly ILogger<DashboardService> _logger;

    // Repositorios
    private readonly IUserRepository _userRepository;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IPortfolioHistoryRepository _portfolioHistoryRepository;
    private readonly IAlertRepository _alertRepository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly INotificationRepository _notificationRepository;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="DashboardService"/> con los repositorios y servicios necesarios para construir la información del dashboard.
    /// </summary>
    /// <param name="userRepository">Repositorio de usuarios.</param>
    /// <param name="portfolioRepository">Repositorio de carteras de inversión.</param>
    /// <param name="portfolioHistoryRepository">Repositorio del historial de carteras.</param>
    /// <param name="marketPriceCacheService">Servicio de caché de precios de mercado.</param>
    /// <param name="logger">Logger para registrar información y errores del servicio.</param>
    /// <param name="alertRepository">Repositorio de alertas.</param>
    /// <param name="priceHistoryRepository">Repositorio del historial de precios.</param>
    /// <param name="transactionRepository">Repositorio de transacciones.</param>
    /// <param name="notificationRepository">Repositorio de notificaciones.</param>
    public DashboardService(IUserRepository userRepository, IPortfolioRepository portfolioRepository, IPortfolioHistoryRepository portfolioHistoryRepository, IMarketPriceCacheService marketPriceCacheService, ILogger<DashboardService> logger, IAlertRepository alertRepository, IPriceHistoryRepository priceHistoryRepository, ITransactionRepository transactionRepository, INotificationRepository notificationRepository)
    {
        _logger = logger;
        _marketPriceCacheService = marketPriceCacheService;

        _userRepository = userRepository;
        _portfolioRepository = portfolioRepository;
        _portfolioHistoryRepository = portfolioHistoryRepository;
        _alertRepository = alertRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _transactionRepository = transactionRepository;
        _notificationRepository = notificationRepository;
    }

    /// <summary>
    /// Obtiene las tarjetas superiores del dashboard con el valor total de la cartera, la ganancia del día, los activos activos y la cantidad de alertas activas del usuario.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <returns>Una respuesta con los datos de las tarjetas superiores del dashboard, o un mensaje de error si el usuario no existe o ocurre un fallo interno.</returns>
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
            var symbols = portfolio.Where(x => x.Asset != null && !string.IsNullOrWhiteSpace(x.Asset.Symbol) && x.Quantity > 0).Select(x => x.Asset.Symbol).Distinct().ToList();

            var marketPricesResponse = symbols.Count == 0 ? new MarketPricesResponseDto() : await _marketPriceCacheService.GetPricesAsync(symbols);
            var pricesBySymbol = marketPricesResponse.Prices.ToDictionary(x => x.Symbol, x => x.Price);

            decimal holdingsValue = 0;

            foreach (var item in portfolio)
            {
                if (item.Asset == null || string.IsNullOrWhiteSpace(item.Asset.Symbol) || item.Quantity <= 0)
                    continue;

                if (!pricesBySymbol.TryGetValue(item.Asset.Symbol, out var currentPrice))
                    continue;

                holdingsValue += item.Quantity * currentPrice;
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

    /// <summary>
    /// Calcula la distribución de la cartera del usuario por sector, indicando el valor y el porcentaje que representa cada sector sobre el total invertido.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <returns>Una respuesta con la lista de distribución de la cartera por sector, o un mensaje de error si ocurre un fallo interno.</returns>
    public async Task<Response> GetPortfolioDistributionAsync(int userId)
    {
        try
        {
            var portfolio = await _portfolioRepository.GetByUserAsync(userId);
            var symbols = portfolio.Where(x => x.Asset != null && !string.IsNullOrWhiteSpace(x.Asset.Symbol) && x.Quantity > 0).Select(x => x.Asset.Symbol).Distinct().ToList();

            var marketPricesResponse = symbols.Count == 0 ? new MarketPricesResponseDto() : await _marketPriceCacheService.GetPricesAsync(symbols);
            var pricesBySymbol = marketPricesResponse.Prices.ToDictionary(x => x.Symbol, x => x.Price);

            var items = new List<(string Sector, decimal Value)>();

            foreach (var item in portfolio)
            {
                if (item.Asset == null || string.IsNullOrWhiteSpace(item.Asset.Symbol) || item.Quantity <= 0)
                    continue;

                if (!pricesBySymbol.TryGetValue(item.Asset.Symbol, out var currentPrice))
                    continue;

                var sector = string.IsNullOrWhiteSpace(item.Asset.Sector) ? "Sin sector" : item.Asset.Sector;
                var value = item.Quantity * currentPrice;

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

    /// <summary>
    /// Obtiene las cinco notificaciones más recientes del usuario para mostrarlas en el dashboard.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <returns>Una respuesta con la lista de notificaciones recientes, o un mensaje de error si ocurre un fallo interno.</returns>
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

    /// <summary>
    /// Genera el gráfico de desempeño de la cartera del usuario comparándolo contra los índices S&amp;P 500 y NASDAQ en el período solicitado, calculando además el valor actual y la variación porcentual.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="filter">Filtro con el período del gráfico a generar (por ejemplo "1W", "1M", "3M" o "1Y").</param>
    /// <returns>Una respuesta con los datos del gráfico de desempeño, o un mensaje de error si el usuario no existe, no hay datos suficientes o ocurre un fallo interno.</returns>
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
                portfolioHistory = portfolioHistory.OrderBy(x => x.Date).ToList();
                sp500History = sp500History.OrderBy(x => x.Date).ToList();
                nasdaqHistory = nasdaqHistory.OrderBy(x => x.Date).ToList();
            }

            var portfolioBase = portfolioHistory.First().TotalValue;
            var sp500Base = sp500History.First().Close;
            var nasdaqBase = nasdaqHistory.First().Close;

            var sp500Dictionary = sp500History.ToDictionary(x => x.Date.Date, x => x.Close);
            var nasdaqDictionary = nasdaqHistory.ToDictionary(x => x.Date.Date, x => x.Close);

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
            var symbols = positions.Where(x => x.Asset != null && !string.IsNullOrWhiteSpace(x.Asset.Symbol) && x.Quantity > 0).Select(x => x.Asset.Symbol).Distinct().ToList();

            var marketPricesResponse = symbols.Count == 0 ? new MarketPricesResponseDto() : await _marketPriceCacheService.GetPricesAsync(symbols);
            var pricesBySymbol = marketPricesResponse.Prices.ToDictionary(x => x.Symbol, x => x.Price);

            decimal holdingsValue = 0;

            foreach (var position in positions)
            {
                if (position.Asset == null || string.IsNullOrWhiteSpace(position.Asset.Symbol) || position.Quantity <= 0)
                    continue;

                if (!pricesBySymbol.TryGetValue(position.Asset.Symbol, out var currentPrice))
                    continue;

                holdingsValue += position.Quantity * currentPrice;
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

    /// <summary>
    /// Obtiene las cinco operaciones más recientes realizadas por el usuario para mostrarlas en el dashboard.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <returns>Una respuesta con la lista de las últimas operaciones del usuario, o un mensaje de error si ocurre un fallo interno.</returns>
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
    /// <summary>
    /// Convierte un tipo de transacción en su representación textual en español.
    /// </summary>
    /// <param name="type">Tipo de transacción a convertir.</param>
    /// <returns>La cadena "Compra", "Venta" o "-" según el tipo de transacción recibido.</returns>
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