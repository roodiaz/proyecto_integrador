using InvestLab.Business.Interfaces.Api;
using InvestLab.Data.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Dashboard;
using InvestLab.Models.DTOs.Market;
using Microsoft.Extensions.Logging;
using static InvestLab.Models.Enums;
using static InvestLab.Models.MessageCodes;

namespace InvestLab.Business.Services.Api;

public class DashboardService : IDashboardService
{
    private readonly IMarketPriceCacheService _marketPriceCacheService;
    private readonly ILogger<DashboardService> _logger;

    // Repositorios
    private readonly IUserPortfolioRepository _userPortfolioRepository;
    private readonly IPortfolioHoldingRepository _portfolioHoldingRepository;
    private readonly IPortfolioHistoryRepository _portfolioHistoryRepository;
    private readonly IAlertRepository _alertRepository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly INotificationRepository _notificationRepository;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="DashboardService"/> con los repositorios y servicios necesarios para construir la información del dashboard.
    /// </summary>
    /// <param name="userPortfolioRepository">Repositorio de portfolios de usuario.</param>
    /// <param name="portfolioHoldingRepository">Repositorio de posiciones de portfolio.</param>
    /// <param name="portfolioHistoryRepository">Repositorio del historial de carteras.</param>
    /// <param name="marketPriceCacheService">Servicio de caché de precios de mercado.</param>
    /// <param name="logger">Logger para registrar información y errores del servicio.</param>
    /// <param name="alertRepository">Repositorio de alertas.</param>
    /// <param name="priceHistoryRepository">Repositorio del historial de precios.</param>
    /// <param name="transactionRepository">Repositorio de transacciones.</param>
    /// <param name="notificationRepository">Repositorio de notificaciones.</param>
    public DashboardService(IUserPortfolioRepository userPortfolioRepository, IPortfolioHoldingRepository portfolioHoldingRepository, IPortfolioHistoryRepository portfolioHistoryRepository, IMarketPriceCacheService marketPriceCacheService, ILogger<DashboardService> logger, IAlertRepository alertRepository, IPriceHistoryRepository priceHistoryRepository, ITransactionRepository transactionRepository, INotificationRepository notificationRepository)
    {
        _logger = logger;
        _marketPriceCacheService = marketPriceCacheService;

        _userPortfolioRepository = userPortfolioRepository;
        _portfolioHoldingRepository = portfolioHoldingRepository;
        _portfolioHistoryRepository = portfolioHistoryRepository;
        _alertRepository = alertRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _transactionRepository = transactionRepository;
        _notificationRepository = notificationRepository;
    }

    /// <summary>
    /// Obtiene las tarjetas superiores del dashboard con el valor total del portfolio, la ganancia del día, los activos activos y la cantidad de alertas activas del usuario.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="portfolioId">Identificador del portfolio.</param>
    /// <returns>Una respuesta con los datos de las tarjetas superiores del dashboard, o un mensaje de error si el portfolio no existe o ocurre un fallo interno.</returns>
    public async Task<Response> GetTopCardsAsync(int userId, int portfolioId)
    {
        try
        {
            var portfolio = await _userPortfolioRepository.GetByIdAndUserAsync(portfolioId, userId);
            if (portfolio == null)
            {
                _logger.LogWarning("Portfolio no encontrado: UserId={UserId}, PortfolioId={PortfolioId}", userId, portfolioId);
                return Response.Fail("Portfolio no encontrado", PORTFOLIO_NOT_FOUND);
            }

            var holdings = await _portfolioHoldingRepository.GetByPortfolioAsync(portfolioId);
            var symbols = holdings.Where(x => x.Asset != null && !string.IsNullOrWhiteSpace(x.Asset.Symbol) && x.Quantity > 0).Select(x => x.Asset.Symbol).Distinct().ToList();

            var marketPricesResponse = symbols.Count == 0 ? new MarketPricesResponseDto() : await _marketPriceCacheService.GetPricesAsync(symbols);
            var pricesBySymbol = marketPricesResponse.Prices.ToDictionary(x => x.Symbol, x => x.Price);

            decimal holdingsValue = 0;

            foreach (var item in holdings)
            {
                if (item.Asset == null || string.IsNullOrWhiteSpace(item.Asset.Symbol) || item.Quantity <= 0)
                    continue;

                if (!pricesBySymbol.TryGetValue(item.Asset.Symbol, out var currentPrice))
                    continue;

                holdingsValue += item.Quantity * currentPrice;
            }

            var totalValue = portfolio.CurrentBalance + holdingsValue;
            var previous = await _portfolioHistoryRepository.GetPreviousAsync(portfolioId);

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
                ActiveAssets = holdings.Count(x => x.Quantity > 0),
                ActiveAlerts = activeAlerts
            };

            _logger.LogInformation("Dashboard top cards obtenidas: UserId={UserId}, PortfolioId={PortfolioId}", userId, portfolioId);

            return Response.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener dashboard top cards");
            return Response.Fail("Error interno");
        }
    }

    /// <summary>
    /// Calcula la distribución del portfolio del usuario por sector, indicando el valor y el porcentaje que representa cada sector sobre el total invertido.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="portfolioId">Identificador del portfolio.</param>
    /// <returns>Una respuesta con la lista de distribución del portfolio por sector, o un mensaje de error si ocurre un fallo interno.</returns>
    public async Task<Response> GetPortfolioDistributionAsync(int userId, int portfolioId)
    {
        try
        {
            var portfolio = await _userPortfolioRepository.GetByIdAndUserAsync(portfolioId, userId);
            if (portfolio == null)
            {
                _logger.LogWarning("Portfolio no encontrado: UserId={UserId}, PortfolioId={PortfolioId}", userId, portfolioId);
                return Response.Fail("Portfolio no encontrado", PORTFOLIO_NOT_FOUND);
            }

            var holdings = await _portfolioHoldingRepository.GetByPortfolioAsync(portfolioId);
            var symbols = holdings.Where(x => x.Asset != null && !string.IsNullOrWhiteSpace(x.Asset.Symbol) && x.Quantity > 0).Select(x => x.Asset.Symbol).Distinct().ToList();

            var marketPricesResponse = symbols.Count == 0 ? new MarketPricesResponseDto() : await _marketPriceCacheService.GetPricesAsync(symbols);
            var pricesBySymbol = marketPricesResponse.Prices.ToDictionary(x => x.Symbol, x => x.Price);

            var items = new List<(string Sector, decimal Value)>();

            foreach (var item in holdings)
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

            _logger.LogInformation("Dashboard portfolio distribution obtenido: UserId={UserId}, PortfolioId={PortfolioId}", userId, portfolioId);

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
    /// Genera el gráfico de desempeño del portfolio del usuario comparándolo contra los índices S&amp;P 500 y NASDAQ en el período solicitado, calculando además el valor actual y la variación porcentual.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="portfolioId">Identificador del portfolio.</param>
    /// <param name="filter">Filtro con el período del gráfico a generar (por ejemplo "1W", "1M", "3M" o "1Y").</param>
    /// <returns>Una respuesta con los datos del gráfico de desempeño, o un mensaje de error si el portfolio no existe, no hay datos suficientes o ocurre un fallo interno.</returns>
    public async Task<Response> GetPerformanceChartAsync(int userId, int portfolioId, DashboardPerformanceChartFilterDto filter)
    {
        try
        {
            var portfolio = await _userPortfolioRepository.GetByIdAndUserAsync(portfolioId, userId);
            if (portfolio == null)
            {
                _logger.LogWarning("Portfolio no encontrado: UserId={UserId}, PortfolioId={PortfolioId}", userId, portfolioId);
                return Response.Fail("Portfolio no encontrado", PORTFOLIO_NOT_FOUND);
            }

            DateTime fromDate = filter.Period switch
            {
                "1W" => DateTime.UtcNow.AddDays(-7),
                "1M" => DateTime.UtcNow.AddMonths(-1),
                "3M" => DateTime.UtcNow.AddMonths(-3),
                "1Y" => DateTime.UtcNow.AddYears(-1),
                _ => DateTime.UtcNow.AddMonths(-1)
            };

            var portfolioHistory = await _portfolioHistoryRepository.GetByPortfolioAndDateAsync(portfolioId, fromDate);
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

            var sp500Sorted = sp500History.OrderBy(x => x.Date).ToList();
            var nasdaqSorted = nasdaqHistory.OrderBy(x => x.Date).ToList();

            decimal GetClosest(List<PriceHistory> sorted, DateTime target)
            {
                var exact = sorted.LastOrDefault(x => x.Date.Date <= target.Date);
                return exact?.Close ?? sorted.First().Close;
            }

            var chartData = portfolioHistory
                .Select(x =>
                {
                    var sp500Close = GetClosest(sp500Sorted, x.Date);
                    var nasdaqClose = GetClosest(nasdaqSorted, x.Date);
                    return new DashboardPerformanceChartPointDto
                    {
                        Label = monthlyView ? x.Date.ToString("MM/yyyy") : x.Date.ToString("dd/MM"),
                        Portfolio = Math.Round((x.TotalValue / portfolioBase) * 100, 2),
                        Sp500 = Math.Round((sp500Close / sp500Base) * 100, 2),
                        Nasdaq = Math.Round((nasdaqClose / nasdaqBase) * 100, 2),
                        PortfolioValue = Math.Round(x.TotalValue, 2),
                        Sp500Value = Math.Round(sp500Close, 2),
                        NasdaqValue = Math.Round(nasdaqClose, 2)
                    };
                })
                .ToList();

            var holdings = await _portfolioHoldingRepository.GetByPortfolioAsync(portfolioId);
            var symbols = holdings.Where(x => x.Asset != null && !string.IsNullOrWhiteSpace(x.Asset.Symbol) && x.Quantity > 0).Select(x => x.Asset.Symbol).Distinct().ToList();

            var marketPricesResponse = symbols.Count == 0 ? new MarketPricesResponseDto() : await _marketPriceCacheService.GetPricesAsync(symbols);
            var pricesBySymbol = marketPricesResponse.Prices.ToDictionary(x => x.Symbol, x => x.Price);

            decimal holdingsValue = 0;

            foreach (var holding in holdings)
            {
                if (holding.Asset == null || string.IsNullOrWhiteSpace(holding.Asset.Symbol) || holding.Quantity <= 0)
                    continue;

                if (!pricesBySymbol.TryGetValue(holding.Asset.Symbol, out var currentPrice))
                    continue;

                holdingsValue += holding.Quantity * currentPrice;
            }

            var currentValue = portfolio.CurrentBalance + holdingsValue;
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

            _logger.LogInformation("Performance chart obtenido para UserId={UserId}, PortfolioId={PortfolioId}", userId, portfolioId);

            return Response.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener performance chart para UserId={UserId}, PortfolioId={PortfolioId}", userId, portfolioId);
            return Response.Fail("Error interno");
        }
    }

    /// <summary>
    /// Obtiene las cinco operaciones más recientes realizadas en el portfolio para mostrarlas en el dashboard.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="portfolioId">Identificador del portfolio.</param>
    /// <returns>Una respuesta con la lista de las últimas operaciones del portfolio, o un mensaje de error si ocurre un fallo interno.</returns>
    public async Task<Response> GetLatestTransactionsAsync(int userId, int portfolioId)
    {
        try
        {
            var portfolio = await _userPortfolioRepository.GetByIdAndUserAsync(portfolioId, userId);
            if (portfolio == null)
            {
                _logger.LogWarning("Portfolio no encontrado: UserId={UserId}, PortfolioId={PortfolioId}", userId, portfolioId);
                return Response.Fail("Portfolio no encontrado", PORTFOLIO_NOT_FOUND);
            }

            var transactions = await _transactionRepository.GetLatestByPortfolioAsync(portfolioId, 5);

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

    /// <summary>
    /// Obtiene la composición del portfolio del usuario para una fecha determinada a partir de los snapshots históricos.
    /// Si no existe snapshot exacto para esa fecha, retorna el snapshot más reciente anterior.
    /// Si no existe ningún snapshot, retorna un objeto con valores en cero.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="portfolioId">Identificador del portfolio.</param>
    /// <param name="date">Fecha de referencia para buscar el snapshot.</param>
    /// <returns>Composición del portfolio (efectivo disponible, capital invertido y total) junto con la fecha efectiva del snapshot.</returns>
    public async Task<Response> GetPortfolioCompositionAsync(int userId, int portfolioId, DateTime date)
    {
        try
        {
            var portfolio = await _userPortfolioRepository.GetByIdAndUserAsync(portfolioId, userId);
            if (portfolio == null)
            {
                _logger.LogWarning("Portfolio no encontrado: UserId={UserId}, PortfolioId={PortfolioId}", userId, portfolioId);
                return Response.Fail("Portfolio no encontrado", PORTFOLIO_NOT_FOUND);
            }

            var snapshot = await _portfolioHistoryRepository.GetOnOrBeforeAsync(portfolioId, date);

            if (snapshot == null)
                return Response.Ok(new DashboardPortfolioCompositionDto());

            var response = new DashboardPortfolioCompositionDto
            {
                AvailableBalance = snapshot.AvailableBalance ?? 0,
                InvestedValue    = snapshot.InvestedValue ?? 0,
                TotalValue       = snapshot.TotalValue,
                EffectiveDate    = snapshot.Date
            };

            _logger.LogInformation("Composición del portfolio obtenida: UserId={UserId}, PortfolioId={PortfolioId}, Fecha={Date}, Efectiva={EffectiveDate}",
                userId, portfolioId, date.Date, snapshot.Date.Date);

            return Response.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener composición del portfolio para UserId={UserId}, PortfolioId={PortfolioId}", userId, portfolioId);
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
