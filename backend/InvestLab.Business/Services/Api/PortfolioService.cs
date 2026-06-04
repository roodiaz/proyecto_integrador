using InvestLab.Business.Interfaces;
using InvestLab.Business.Interfaces.Api;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Data.Repositories;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models;
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
    private readonly IExternalProvider _externalProvider;
    private readonly IMarketPriceService _marketPriceService;
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

    public PortfolioService(IUserRepository userRepository, IUserSettingRepository userSettingRepository, IAssetRepository assetRepository, IPortfolioRepository portfolioRepository, ITransactionRepository transactionRepository, IExternalProvider externalProvider, IUnitOfWork unitOfWork, IPortfolioHistoryRepository portfolioHistoryRepository, IOptions<LimitsOptions> limits, ILogger<PortfolioService> logger, IMarketPriceService marketPriceService, IMarketMetadataRepository marketMetadataRepository, IAssetService assetService)
    {
        _userRepository = userRepository;
        _userSettingRepository = userSettingRepository;
        _assetRepository = assetRepository;
        _portfolioRepository = portfolioRepository;
        _transactionRepository = transactionRepository;
        _externalProvider = externalProvider;
        _unitOfWork = unitOfWork;
        _portfolioHistoryRepository = portfolioHistoryRepository;
        _limits = limits.Value;
        _logger = logger;
        _marketPriceService = marketPriceService;
        _marketMetadataRepository = marketMetadataRepository;
        _assetService = assetService;
    }

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

            var market = await _externalProvider.GetPriceAsync(asset.Symbol);
            if (market == null)
            {
                _logger.LogWarning("No se pudo obtener precio: {Symbol}", asset.Symbol);
                return Response.Fail("No se pudo obtener el precio");
            }

            var total = dto.Quantity * market.Price;

            if (user.Balance < total)
            {
                _logger.LogWarning("Saldo insuficiente: {UserId}", userId);
                return Response.Fail("Saldo insuficiente");
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

            var transaction = new Transaction
            {
                UserId = userId,
                AssetId = asset.Id,
                Type = TransactionType.Buy,
                Quantity = dto.Quantity,
                Price = market.Price,
                Total = total,
                CreatedAt = DateTime.UtcNow
            };

            await _transactionRepository.InsertAsync(transaction);

            user.Balance -= total;
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

            var asset = await _assetRepository.GetBySymbolAsync(dto.Symbol);
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

            var market = await _externalProvider.GetPriceAsync(asset.Symbol);
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

            var transaction = new Transaction
            {
                UserId = userId,
                AssetId = asset.Id,
                Type = TransactionType.Sell,
                Quantity = dto.Quantity,
                Price = market.Price,
                Total = total,
                CreatedAt = DateTime.UtcNow
            };

            await _transactionRepository.InsertAsync(transaction);

            user.Balance += total;
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

    public async Task<Response> GetPositionForSellAsync(int userId, string symbol)
    {
        try
        {
            var asset = await _assetRepository.GetBySymbolAsync(symbol);
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

            var market = await _externalProvider.GetPriceAsync(asset.Symbol);
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

    public async Task<Response> GetPriceAsync(string symbol)
    {
        try
        {
            var asset = await _assetRepository.GetBySymbolAsync(symbol);
            if (asset == null)
            {
                _logger.LogWarning("Activo no encontrado: {Symbol}", symbol);
                return Response.Fail("Activo no encontrado");
            }

            var market = await _externalProvider.GetPriceAsync(asset.Symbol);
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

            // Obtiene precios en tiempo real desde Yahoo
            var marketPrices = await _externalProvider.GetPricesAsync(symbols);
            var pricesBySymbol = marketPrices.ToDictionary(x => x.Symbol, x => x.Price);

            decimal holdingsValue = 0;
            foreach (var item in portfolio)
            {
                if (!pricesBySymbol.TryGetValue(item.Asset.Symbol, out var currentPrice))
                {
                    _logger.LogWarning("Sin precio disponible para {Symbol}", item.Asset.Symbol);
                    continue;
                }

                holdingsValue += item.Quantity * currentPrice;
            }

            var marketMetadata = await _marketMetadataRepository.GetAsync();

            var currentBalance = user.Balance + holdingsValue;
            var profitLoss = currentBalance - _limits.InitialBalance;
            var profitPercent = _limits.InitialBalance == 0 ? 0 : (profitLoss / _limits.InitialBalance) * 100;

            var response = new PortfolioBalanceCardsDto
            {
                InitialBalance = _limits.InitialBalance,
                CurrentBalance = Math.Round(currentBalance, 2),
                ProfitLoss = Math.Round(profitLoss, 2),
                ProfitLossPercent = Math.Round(profitPercent, 2),
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

            var marketPrices = await _externalProvider.GetPricesAsync(symbols);

            var pricesBySymbol = marketPrices.ToDictionary(x => x.Symbol, x => x.Price);

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

    public async Task<Response> GetOpenPositionsAsync(int userId, PortfolioOpenPositionsFilterDto filter)
    {
        try
        {
            var portfolio = await _portfolioRepository.GetPagedByUserAsync(userId);

            var symbols = portfolio.Select(x => x.Asset.Symbol).Distinct().ToList();

            var marketPrices = await _externalProvider.GetPricesAsync(symbols);

            var pricesBySymbol = marketPrices.ToDictionary(x => x.Symbol, x => x.Price);

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

            if (filter.Status == "gain")
                positions = positions.Where(x => x.ProfitLoss > 0).ToList();
            else if (filter.Status == "loss")
                positions = positions.Where(x => x.ProfitLoss < 0).ToList();

            var desc = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            positions = filter.SortBy switch
            {
                "symbol" => desc ? positions.OrderByDescending(x => x.Symbol).ToList() : positions.OrderBy(x => x.Symbol).ToList(),

                "quantity" => desc ? positions.OrderByDescending(x => x.Quantity).ToList() : positions.OrderBy(x => x.Quantity).ToList(),

                "currentValue" => desc ? positions.OrderByDescending(x => x.CurrentPrice * x.Quantity).ToList() : positions.OrderBy(x => x.CurrentPrice * x.Quantity).ToList(),

                "variation" => desc ? positions.OrderByDescending(x => x.VariationPercent).ToList() : positions.OrderBy(x => x.VariationPercent).ToList(),

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
}