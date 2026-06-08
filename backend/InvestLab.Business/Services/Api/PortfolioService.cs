using InvestLab.Business.Interfaces;
using InvestLab.Business.Interfaces.Api;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Market;
using InvestLab.Models.DTOs.Portfolio;
using InvestLab.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static InvestLab.Models.Enums;

namespace InvestLab.Business.Services.Api;

public class PortfolioService : IPortfolioService
{
    private readonly LimitsOptions _limits;
    private readonly ILogger<PortfolioService> _logger;
    private readonly IMarketProviderResolver _providerResolver;
    private readonly IMarketPriceService _marketPriceService;
    private readonly IMarketPriceCacheService _marketPriceCacheService;
    private readonly IAssetService _assetService;

    // repositorios
    private readonly IUserRepository _userRepository;
    private readonly IUserSettingRepository _userSettingRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPortfolioHistoryRepository _portfolioHistoryRepository;
    private readonly IMarketMetadataRepository _marketMetadataRepository;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="PortfolioService"/> inyectando los repositorios, opciones y servicios necesarios para gestionar el portfolio del usuario.
    /// </summary>
    /// <param name="userRepository">Repositorio de usuarios.</param>
    /// <param name="userSettingRepository">Repositorio de configuraciones de usuario.</param>
    /// <param name="assetRepository">Repositorio de activos.</param>
    /// <param name="portfolioRepository">Repositorio de portfolios.</param>
    /// <param name="transactionRepository">Repositorio de transacciones.</param>
    /// <param name="externalProvider">Proveedor externo de datos de mercado.</param>
    /// <param name="unitOfWork">Unidad de trabajo para confirmar los cambios en la base de datos.</param>
    /// <param name="portfolioHistoryRepository">Repositorio del historial de portfolio.</param>
    /// <param name="limits">Opciones de límites de operaciones y balance inicial.</param>
    /// <param name="logger">Logger para registrar información y errores del servicio.</param>
    /// <param name="marketPriceService">Servicio de precios de mercado.</param>
    /// <param name="marketMetadataRepository">Repositorio de metadatos de mercado.</param>
    /// <param name="assetService">Servicio de activos.</param>
    /// <param name="marketPriceCacheService">Servicio de cache de precios de mercado para datos informativos.</param>
    public PortfolioService(IUserRepository userRepository, IUserSettingRepository userSettingRepository, IAssetRepository assetRepository, IPortfolioRepository portfolioRepository, ITransactionRepository transactionRepository, IMarketProviderResolver providerResolver, IUnitOfWork unitOfWork, IPortfolioHistoryRepository portfolioHistoryRepository, IOptions<LimitsOptions> limits, ILogger<PortfolioService> logger, IMarketPriceService marketPriceService, IMarketMetadataRepository marketMetadataRepository, IAssetService assetService, IMarketPriceCacheService marketPriceCacheService)
    {
        _userRepository = userRepository;
        _userSettingRepository = userSettingRepository;
        _assetRepository = assetRepository;
        _portfolioRepository = portfolioRepository;
        _transactionRepository = transactionRepository;
        _providerResolver = providerResolver;
        _unitOfWork = unitOfWork;
        _portfolioHistoryRepository = portfolioHistoryRepository;
        _limits = limits.Value;
        _logger = logger;
        _marketPriceService = marketPriceService;
        _marketMetadataRepository = marketMetadataRepository;
        _assetService = assetService;
        _marketPriceCacheService = marketPriceCacheService;
    }

    /// <summary>
    /// Realiza la compra de un activo para un usuario, validando saldo, límites diarios y disponibilidad del precio, y actualiza el portfolio y las transacciones correspondientes.
    /// </summary>
    /// <param name="userId">Identificador del usuario que realiza la compra.</param>
    /// <param name="dto">Datos de la operación de compra, incluyendo símbolo y cantidad.</param>
    /// <returns>Una respuesta indicando si la compra se realizó correctamente o el motivo del error.</returns>
    public async Task<Response> BuyAsync(int userId, BuyAssetDto dto)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Usuario no encontrado: {UserId}", userId);
                return Response.Fail("Usuario no encontrado");
            }

            var settings = await _userSettingRepository.GetByUserIdAsync(userId);
            if (settings == null)
            {
                _logger.LogWarning("Settings no encontrados: {UserId}", userId);
                return Response.Fail("Configuración no encontrada");
            }

            if (settings.OperationsUsedToday >= _limits.MaxOperationsPerDay)
            {
                _logger.LogWarning("Límite diario alcanzado: {UserId}", userId);
                return Response.Fail("Límite diario alcanzado");
            }

            var asset = await _assetService.GetOrCreateAsync(dto.Symbol);
            if (asset == null)
                return Response.Fail("Activo no encontrado");

            var market = await _providerResolver.GetProvider().GetPriceAsync(asset.Symbol);
            if (market == null)
            {
                _logger.LogWarning("No se pudo obtener precio: {Symbol}", asset.Symbol);
                return Response.Fail("No se pudo obtener el precio");
            }

            var total = dto.Quantity * market.Price;

            if (user.Balance < total)
            {
                _logger.LogWarning("Saldo insuficiente: {UserId}", userId);
                return Response.Fail("Saldo insuficiente. Podés vender activos o reiniciar tu portfolio simulado.");
            }

            var portfolio = await _portfolioRepository.GetByUserAndAssetAsync(userId, asset.Id);
            if (portfolio == null)
            {
                portfolio = new Portfolio
                {
                    UserId = userId,
                    AssetId = asset.Id,
                    Quantity = dto.Quantity,
                    AvgPrice = market.Price
                };

                await _portfolioRepository.InsertAsync(portfolio);
            }
            else
            {
                var currentTotal = portfolio.Quantity * portfolio.AvgPrice;
                var newTotal = dto.Quantity * market.Price;

                portfolio.Quantity += dto.Quantity;
                portfolio.AvgPrice = (currentTotal + newTotal) / portfolio.Quantity;

                await _portfolioRepository.UpdateAsync(portfolio);
            }

            var balanceBefore = user.Balance;
            user.Balance -= total;

            var transaction = new Transaction
            {
                UserId = userId,
                AssetId = asset.Id,
                Type = TransactionType.Buy,
                Quantity = dto.Quantity,
                Price = market.Price,
                Total = total,
                BalanceBefore = balanceBefore,
                BalanceAfter = user.Balance,
                CreatedAt = DateTime.UtcNow
            };

            await _transactionRepository.InsertAsync(transaction);
            settings.OperationsUsedToday++;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Compra realizada: UserId={UserId}, Asset={Asset}, Quantity={Quantity}", userId, asset.Symbol, dto.Quantity);

            return Response.Ok(null, "Compra realizada correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al comprar activo");

            return Response.Fail("Error interno");
        }
    }

    /// <summary>
    /// Realiza la venta de un activo perteneciente al portfolio de un usuario, validando cantidad disponible, límites diarios y disponibilidad del precio, y actualiza el portfolio y las transacciones correspondientes.
    /// </summary>
    /// <param name="userId">Identificador del usuario que realiza la venta.</param>
    /// <param name="dto">Datos de la operación de venta, incluyendo símbolo y cantidad.</param>
    /// <returns>Una respuesta indicando si la venta se realizó correctamente o el motivo del error.</returns>
    public async Task<Response> SellAsync(int userId, SellAssetDto dto)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Usuario no encontrado: {UserId}", userId);
                return Response.Fail("Usuario no encontrado");
            }

            var settings = await _userSettingRepository.GetByUserIdAsync(userId);
            if (settings == null)
            {
                _logger.LogWarning("Settings no encontrados: {UserId}", userId);
                return Response.Fail("Configuración no encontrada");
            }
            if (settings.OperationsUsedToday >= _limits.MaxOperationsPerDay)
            {
                _logger.LogWarning("Límite diario alcanzado: {UserId}", userId);
                return Response.Fail("Límite diario alcanzado");
            }

            var asset = await _assetRepository.GetAsync(dto.Symbol);
            if (asset == null)
            {
                _logger.LogWarning("Activo no encontrado: {AssetId}", dto.Symbol);
                return Response.Fail("Activo no encontrado");
            }

            var portfolio = await _portfolioRepository.GetByUserAndAssetAsync(userId, asset.Id);
            if (portfolio == null)
            {
                _logger.LogWarning("Portfolio no encontrado: UserId={UserId}, AssetId={AssetId}", userId, asset.Id);
                return Response.Fail("Activo no encontrado en portfolio");
            }
            if (portfolio.Quantity < dto.Quantity)
            {
                _logger.LogWarning("Cantidad insuficiente: UserId={UserId}, AssetId={AssetId}", userId, asset.Id);
                return Response.Fail("Cantidad insuficiente");
            }

            var market = await _providerResolver.GetProvider().GetPriceAsync(asset.Symbol);
            if (market == null)
            {
                _logger.LogWarning("No se pudo obtener precio: {Symbol}", asset.Symbol);
                return Response.Fail("No se pudo obtener el precio");
            }

            var total = dto.Quantity * market.Price;
            portfolio.Quantity -= dto.Quantity;

            if (portfolio.Quantity == 0)
                await _portfolioRepository.DeleteAsync(portfolio);
            else
                await _portfolioRepository.UpdateAsync(portfolio);

            var balanceBefore = user.Balance;
            user.Balance += total;

            var transaction = new Transaction
            {
                UserId = userId,
                AssetId = asset.Id,
                Type = TransactionType.Sell,
                Quantity = dto.Quantity,
                Price = market.Price,
                Total = total,
                BalanceBefore = balanceBefore,
                BalanceAfter = user.Balance,
                CreatedAt = DateTime.UtcNow
            };

            await _transactionRepository.InsertAsync(transaction);
            settings.OperationsUsedToday++;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Venta realizada: UserId={UserId}, Asset={Asset}, Quantity={Quantity}", userId, asset.Symbol, dto.Quantity);

            return Response.Ok(null, "Venta realizada correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al vender activo");

            return Response.Fail("Error interno");
        }
    }

    /// <summary>
    /// Obtiene la información de la posición actual de un usuario sobre un activo específico, incluyendo cantidad, precio promedio y precio actual de mercado, para utilizarse antes de una venta.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="symbol">Símbolo del activo a consultar.</param>
    /// <returns>Una respuesta con los datos de la posición o el motivo del error si no se encuentra.</returns>
    public async Task<Response> GetPositionForSellAsync(int userId, string symbol)
    {
        try
        {
            var asset = await _assetRepository.GetAsync(symbol);
            if (asset == null)
            {
                _logger.LogWarning("Activo no encontrado: {AssetId}", symbol);
                return Response.Fail("Activo no encontrado");
            }

            var portfolio = await _portfolioRepository.GetByUserAndAssetAsync(userId, asset.Id);
            if (portfolio == null)
            {
                _logger.LogWarning("Posición no encontrada: UserId={UserId}, AssetId={AssetId}", userId, asset.Id);
                return Response.Fail("Posición no encontrada");
            }

            var market = await _providerResolver.GetProvider().GetPriceAsync(asset.Symbol);
            if (market == null)
            {
                _logger.LogWarning("No se pudo obtener precio: {Symbol}", asset.Symbol);
                return Response.Fail("No se pudo obtener el precio");
            }

            var response = new PortfolioPositionDto
            {
                Symbol = asset.Symbol,
                Quantity = portfolio.Quantity,
                AvgPrice = portfolio.AvgPrice,
                CurrentPrice = market.Price
            };

            _logger.LogInformation("Posición obtenida: UserId={UserId}, Asset={Asset}", userId, asset.Symbol);

            return Response.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener posición");

            return Response.Fail("Error interno");
        }
    }

    /// <summary>
    /// Obtiene el precio actual de mercado de un activo a partir de su símbolo.
    /// </summary>
    /// <param name="symbol">Símbolo del activo cuyo precio se desea consultar.</param>
    /// <returns>Una respuesta con el precio actual del activo o el motivo del error si no se pudo obtener.</returns>
    public async Task<Response> GetPriceAsync(string symbol)
    {
        try
        {
            var asset = await _assetRepository.GetAsync(symbol);
            if (asset == null)
            {
                _logger.LogWarning("Activo no encontrado: {Symbol}", symbol);
                return Response.Fail("Activo no encontrado");
            }

            var market = await _providerResolver.GetProvider().GetPriceAsync(asset.Symbol);
            if (market == null)
            {
                _logger.LogWarning("No se pudo obtener precio: {Symbol}", symbol);
                return Response.Fail("No se pudo obtener el precio");
            }

            var response = new AssetPriceDto
            {
                Symbol = asset.Symbol,
                CurrentPrice = market.Price
            };

            _logger.LogInformation("Precio obtenido: {Symbol}", symbol);

            return Response.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener precio");

            return Response.Fail("Error interno");
        }
    }

    /// <summary>
    /// Obtiene un resumen del estado financiero del portfolio del usuario, incluyendo balance actual, ganancia/pérdida, porcentaje de rentabilidad y cantidad de operaciones realizadas.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <returns>Una respuesta con las tarjetas de balance del portfolio o el motivo del error.</returns>
    public async Task<Response> GetBalanceCardsAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Usuario no encontrado: {UserId}", userId);
                return Response.Fail("Usuario no encontrado");
            }

            var settings = await _userSettingRepository.GetByUserIdAsync(userId);
            if (settings == null)
            {
                _logger.LogWarning("Settings no encontrados: {UserId}", userId);
                return Response.Fail("Configuración no encontrada");
            }

            var portfolio = await _portfolioRepository.GetByUserAsync(userId);

            var symbols = portfolio.Select(x => x.Asset.Symbol).Distinct().ToList();

            var marketPricesResponse = symbols.Count == 0 ? new MarketPricesResponseDto() : await _marketPriceCacheService.GetPricesAsync(symbols);
            var pricesBySymbol = marketPricesResponse.Prices.ToDictionary(x => x.Symbol, x => x.Price);

            decimal holdingsValue = 0;
            decimal unrealizedProfitLoss = 0;
            foreach (var item in portfolio)
            {
                if (!pricesBySymbol.TryGetValue(item.Asset.Symbol, out var currentPrice))
                {
                    _logger.LogWarning("Sin precio disponible para {Symbol}", item.Asset.Symbol);
                    continue;
                }

                holdingsValue += item.Quantity * currentPrice;
                unrealizedProfitLoss += (currentPrice - item.AvgPrice) * item.Quantity;
            }

            var marketMetadata = await _marketMetadataRepository.GetAsync();

            var totalBalance = user.Balance + holdingsValue;
            var profitLoss = totalBalance - _limits.InitialBalance;
            var profitPercent = _limits.InitialBalance == 0 ? 0 : (profitLoss / _limits.InitialBalance) * 100;
            var realizedProfitLoss = profitLoss - unrealizedProfitLoss;

            var response = new PortfolioBalanceCardsDto
            {
                CurrentBalance = Math.Round(user.Balance, 2),
                TotalBalance = Math.Round(totalBalance, 2),
                ProfitLoss = Math.Round(profitLoss, 2),
                ProfitLossPercent = Math.Round(profitPercent, 2),
                RealizedProfitLoss = Math.Round(realizedProfitLoss, 2),
                UnrealizedProfitLoss = Math.Round(unrealizedProfitLoss, 2),
                TotalOperations = settings.OperationsUsedToday,
                MaxOperations = _limits.MaxOperationsPerDay,
                LastMarketCloseDate = marketMetadata?.LastMarketCloseDate
            };

            _logger.LogInformation("Resumen portfolio obtenido: UserId={UserId}", userId);

            return Response.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener resumen portfolio");
            return Response.Fail("Error interno");
        }
    }

    /// <summary>
    /// Calcula la distribución porcentual del valor del portfolio del usuario por activo, para ser representada en un gráfico de torta.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <returns>Una respuesta con la lista de elementos del gráfico de torta del portfolio o el motivo del error.</returns>
    public async Task<Response> GetPieChartAsync(int userId)
    {
        try
        {
            var portfolio = await _portfolioRepository.GetByUserAsync(userId);

            if (!portfolio.Any())
            {
                _logger.LogWarning("Portfolio vacío: UserId={UserId}", userId);
                return Response.Ok(new List<PortfolioPieChartItemDto>());
            }

            var symbols = portfolio.Select(x => x.Asset.Symbol).Distinct().ToList();

            var marketPricesResponse = symbols.Count == 0 ? new MarketPricesResponseDto() : await _marketPriceCacheService.GetPricesAsync(symbols);
            var pricesBySymbol = marketPricesResponse.Prices.ToDictionary(x => x.Symbol, x => x.Price);

            var positions = new List<(string Symbol, decimal Value)>();
            decimal totalPortfolioValue = 0;

            foreach (var item in portfolio)
            {
                if (!pricesBySymbol.TryGetValue(item.Asset.Symbol, out var currentPrice))
                    continue;

                var currentValue = item.Quantity * currentPrice;

                totalPortfolioValue += currentValue;
                positions.Add((item.Asset.Symbol, currentValue));
            }

            var response = positions
                .Select(x => new PortfolioPieChartItemDto
                {
                    Symbol = x.Symbol,
                    CurrentValue = Math.Round(x.Value, 2),
                    Percentage = totalPortfolioValue == 0 ? 0 : Math.Round((x.Value / totalPortfolioValue) * 100, 2)
                })
                .OrderByDescending(x => x.Percentage)
                .ToList();

            _logger.LogInformation("Pie chart portfolio obtenido: UserId={UserId}", userId);

            return Response.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener pie chart portfolio");
            return Response.Fail("Error interno");
        }
    }

    /// <summary>
    /// Obtiene las posiciones abiertas del portfolio de un usuario, aplicando filtros de búsqueda, estado de ganancia/pérdida, ordenamiento y paginación.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="filter">Filtros de búsqueda, ordenamiento y paginación a aplicar sobre las posiciones.</param>
    /// <returns>Una respuesta con el total de posiciones y la lista paginada de posiciones abiertas, o el motivo del error.</returns>
    public async Task<Response> GetOpenPositionsAsync(int userId, PortfolioOpenPositionsFilterDto filter)
    {
        try
        {
            var portfolio = await _portfolioRepository.GetPagedByUserAsync(userId);

            var symbols = portfolio.Select(x => x.Asset.Symbol).Distinct().ToList();

            var marketPricesResponse = symbols.Count == 0 ? new MarketPricesResponseDto() : await _marketPriceCacheService.GetPricesAsync(symbols);
            var pricesBySymbol = marketPricesResponse.Prices.ToDictionary(x => x.Symbol, x => x.Price);

            var positions = new List<PortfolioOpenPositionDto>();

            foreach (var item in portfolio)
            {
                if (!pricesBySymbol.TryGetValue(item.Asset.Symbol, out var currentPrice))
                    continue;

                var variationPercent = ((currentPrice - item.AvgPrice) / item.AvgPrice) * 100;
                var profitLoss = (currentPrice - item.AvgPrice) * item.Quantity;

                positions.Add(new PortfolioOpenPositionDto
                {
                    Symbol = item.Asset.Symbol,
                    Sector = item.Asset.Sector,
                    Quantity = item.Quantity,
                    AveragePrice = Math.Round(item.AvgPrice, 2),
                    CurrentPrice = Math.Round(currentPrice, 2),
                    VariationPercent = Math.Round(variationPercent, 2),
                    ProfitLoss = Math.Round(profitLoss, 2),
                    IsOpen = true
                });
            }

            if (!string.IsNullOrWhiteSpace(filter.Symbol))
            {
                positions = positions
                    .Where(x => x.Symbol.Contains(filter.Symbol, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var desc = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            positions = filter.SortBy switch
            {
                "symbol" => desc ? positions.OrderByDescending(x => x.Symbol).ToList() : positions.OrderBy(x => x.Symbol).ToList(),

                "sector" => desc ? positions.OrderByDescending(x => x.Sector).ToList() : positions.OrderBy(x => x.Sector).ToList(),

                "quantity" => desc ? positions.OrderByDescending(x => x.Quantity).ToList() : positions.OrderBy(x => x.Quantity).ToList(),

                "averagePrice" => desc ? positions.OrderByDescending(x => x.AveragePrice).ToList() : positions.OrderBy(x => x.AveragePrice).ToList(),

                "currentPrice" => desc ? positions.OrderByDescending(x => x.CurrentPrice).ToList() : positions.OrderBy(x => x.CurrentPrice).ToList(),

                "variationPercent" => desc ? positions.OrderByDescending(x => x.VariationPercent).ToList() : positions.OrderBy(x => x.VariationPercent).ToList(),

                "profitLoss" => desc ? positions.OrderByDescending(x => x.ProfitLoss).ToList() : positions.OrderBy(x => x.ProfitLoss).ToList(),

                _ => positions
            };

            var total = positions.Count;

            positions = positions.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();

            _logger.LogInformation("Open positions obtenidas: UserId={UserId}", userId);

            return Response.Ok(new
            {
                Total = total,
                Items = positions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener open positions");
            return Response.Fail("Error interno");
        }
    }

    /// <summary>
    /// Obtiene la evolución histórica del valor total del portfolio de un usuario dentro de un período determinado, para ser representada en un gráfico de líneas.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="filter">Filtro que indica el período de tiempo a consultar.</param>
    /// <returns>Una respuesta con la lista de puntos del gráfico de líneas del portfolio o el motivo del error.</returns>
    public async Task<Response> GetLineChartAsync(int userId, PortfolioLineChartFilterDto filter)
    {
        try
        {
            var fromDate = filter.Period switch
            {
                "7d" => DateTime.UtcNow.AddDays(-7),
                "1m" => DateTime.UtcNow.AddMonths(-1),
                "3m" => DateTime.UtcNow.AddMonths(-3),
                "1y" => DateTime.UtcNow.AddYears(-1),
                _ => DateTime.UtcNow.AddDays(-7)
            };

            var history = await _portfolioHistoryRepository.GetByUserAndDateAsync(userId, fromDate);

            var response = history
                .OrderBy(x => x.Date)
                .Select(x => new PortfolioLineChartItemDto
                {
                    Date = x.Date,
                    TotalValue = Math.Round(x.TotalValue, 2)
                })
                .ToList();

            _logger.LogInformation("Line chart portfolio obtenido: UserId={UserId}", userId);

            return Response.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener line chart portfolio");

            return Response.Fail("Error interno");
        }
    }

    /// <summary>
    /// Reinicia la simulación del portfolio de un usuario, eliminando su historial, transacciones y posiciones, restableciendo el balance al valor inicial y reiniciando el contador de operaciones diarias.
    /// </summary>
    /// <param name="userId">Identificador del usuario cuyo portfolio se desea reiniciar.</param>
    /// <returns>Una respuesta indicando si el portfolio se reinició correctamente o el motivo del error.</returns>
    public async Task<Response> ResetSimulationAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("Usuario no encontrado al reiniciar portfolio: {UserId}", userId);
                return Response.Fail("Usuario no encontrado");
            }

            await _portfolioHistoryRepository.DeleteByUserIdAsync(userId);
            await _transactionRepository.DeleteByUserIdAsync(userId);
            await _portfolioRepository.DeleteByUserIdAsync(userId);
            await _userRepository.UpdateBalanceAsync(userId, _limits.InitialBalance);
            await _userSettingRepository.ResetOperationsUsedTodayAsync(userId);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Portfolio reiniciado correctamente: UserId={UserId}", userId);

            return Response.Ok(null, "Portfolio reiniciado correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reiniciar portfolio: UserId={UserId}", userId);
            return Response.Fail("Error interno");
        }
    }
}