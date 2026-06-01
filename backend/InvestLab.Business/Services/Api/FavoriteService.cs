using InvestLab.Business.Interfaces.Api;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Favorite;
using InvestLab.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvestLab.Business.Services.Api
{
    public class FavoriteService : IFavoriteService
    {
        private readonly ILogger<FavoriteService> _logger;
        private readonly LimitsOptions _limits;
        private readonly IExternalProvider _externalProvider;
        private readonly IAssetService _assetService;

        private readonly IUserSettingRepository _userSettingRepository;
        private readonly IFavoriteRepository _favoriteRepo;
        private readonly IAssetRepository _assetRepo;
        private readonly IUnitOfWork _uow;

        public FavoriteService(IFavoriteRepository repo, IAssetRepository assetRepo, IUnitOfWork uow, ILogger<FavoriteService> logger, IOptions<LimitsOptions> options, IExternalProvider externalProvider, IAssetService assetService, IUserSettingRepository userSettingRepository)
        {
            _favoriteRepo = repo;
            _assetRepo = assetRepo;
            _uow = uow;
            _logger = logger;
            _limits = options.Value;
            _externalProvider = externalProvider;
            _assetService = assetService;
            _userSettingRepository = userSettingRepository;
        }

        public async Task<Response> GetAsync(int userId, FavoriteFilterDto filter)
        {
            var settings = await _userSettingRepository.GetByUserIdAsync(userId);

            var (list, total) = await _favoriteRepo.GetPagedAsync(userId, filter);

            var symbols = list.Select(x => x.Asset.Symbol).ToList();
            var marketData = await _externalProvider.GetPricesAsync(symbols);

            var result = list.Select(fav =>
            {
                var market = marketData.FirstOrDefault(x => x.Symbol == fav.Asset.Symbol);

                return new
                {
                    Id = fav.Id,
                    Symbol = fav.Asset.Symbol,
                    Name = fav.Asset.Name,
                    Price = market?.Price ?? 0,
                    VariationPercent = market?.VariationPercent ?? 0,

                };
            });

            return Response.Ok(new
            {
                Items = result,
                Total = total,
                Page = filter.Page,
                PageSize = filter.PageSize,
                CurrentFavorites = settings?.FavoritesUsed ?? 0,
                MaxFavorites = _limits.MaxFavorites
            });
        }

        public async Task<Response> AddAsync(int userId, AddFavoriteDto dto)
        {
            var settings = await _userSettingRepository.GetByUserIdAsync(userId);
            if (settings == null)
                return Response.Fail("Configuración de usuario no encontrada");

            var asset = await _assetService.GetOrCreateAsync(dto.Symbol);
            if (asset == null)
                return Response.Fail("Activo no encontrado");

            if (settings.FavoritesUsed >= _limits.MaxFavorites)
                return Response.Fail("Límite de favoritos alcanzado");

            var exists = await _favoriteRepo.ExistsAsync(userId, asset.Id);
            if (exists)
                return Response.Fail("El activo ya está en favoritos");

            await _favoriteRepo.AddAsync(new Favorite
            {
                UserId = userId,
                AssetId = asset.Id
            });

            settings.FavoritesUsed++;

            await _uow.SaveChangesAsync();

            _logger.LogInformation("Favorito agregado {Symbol} para usuario {UserId}", dto.Symbol, userId);

            return Response.Ok(null);
        }

        public async Task<Response> RemoveAsync(int userId, string symbol)
        {
            symbol = symbol.Trim().ToUpper();

            var settings = await _userSettingRepository.GetByUserIdAsync(userId);
            if (settings == null)
                return Response.Fail("Configuración de usuario no encontrada");

            var asset = await _assetRepo.GetBySymbolAsync(symbol);
            if (asset == null)
                return Response.Fail("Activo no encontrado");

            var fav = await _favoriteRepo.GetByUserAndAssetAsync(userId, asset.Id);
            if (fav == null)
                return Response.Fail("Favorito no encontrado");

            _favoriteRepo.Remove(fav);

            if (settings.FavoritesUsed > 0)
                settings.FavoritesUsed--;

            await _uow.SaveChangesAsync();

            _logger.LogInformation($"Favorito eliminado {symbol} para usuario {userId}");

            return Response.Ok(null);
        }
    }
}
