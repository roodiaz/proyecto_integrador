using InvestLab.Business.Interfaces.Api;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
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
    private readonly IUserRepository _userRepository;
    private readonly IUserSettingRepository _userSettingRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IExternalProvider _externalProvider;
    private readonly IUnitOfWork _unitOfWork;

    public PortfolioService(IUserRepository userRepository, IUserSettingRepository userSettingRepository, IAssetRepository assetRepository, IPortfolioRepository portfolioRepository, ITransactionRepository transactionRepository, IExternalProvider externalProvider, IUnitOfWork unitOfWork, IOptions<LimitsOptions> limits, ILogger<PortfolioService> logger)
    {
        _userRepository = userRepository;
        _userSettingRepository = userSettingRepository;
        _assetRepository = assetRepository;
        _portfolioRepository = portfolioRepository;
        _transactionRepository = transactionRepository;
        _externalProvider = externalProvider;
        _unitOfWork = unitOfWork;
        _limits = limits.Value;
        _logger = logger;
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

            var asset = await _assetRepository.GetBySymbolAsync(dto.Symbol);

            if (asset == null)
            {
                _logger.LogWarning("Activo no encontrado: {AssetId}", dto.Symbol);
                return Response.Fail("Activo no encontrado");
            }

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
}