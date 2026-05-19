using InvestLab.Business.Interfaces;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Integrations.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Favorite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvestLab.Business.Services
{
    public class FavoriteService : IFavoriteService
    {
        private readonly IExternalProvider _externalProvider;
        private readonly LimitsOptions _limits;
        private readonly IFavoriteRepository _repo;
        private readonly IAssetRepository _assetRepo;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<FavoriteService> _logger;

        public FavoriteService(IFavoriteRepository repo, IAssetRepository assetRepo, IUnitOfWork uow, ILogger<FavoriteService> logger, IOptions<LimitsOptions> options, IExternalProvider externalProvider)
        {
            _repo = repo;
            _assetRepo = assetRepo;
            _uow = uow;
            _logger = logger;
            _limits = options.Value;
            _externalProvider = externalProvider;
        }

        public async Task<Response> GetAsync(int userId, FavoriteFilterDto filter)
        {
            var (list, total) = await _repo.GetPagedAsync(userId, filter.Page, filter.PageSize);

            var symbols = list.Select(x => x.Asset.Symbol).ToList();

            var marketData = await _externalProvider.GetPricesAsync(symbols);

            var result = list.Select(fav =>
            {
                var market = marketData.FirstOrDefault(x => x.Symbol == fav.Asset.Symbol);

                return new
                {
                    Id = fav.Id,
                    Symbol = fav.Asset.Symbol,
                    Price = market?.Price ?? 0,
                    VariationPercent = market?.VariationPercent ?? 0
                };
            });

            return Response.Ok(new
            {
                data = result,
                total
            });
        }

        public async Task<Response> AddAsync(int userId, AddFavoriteDto dto)
        {
            var asset = await _assetRepo.GetBySymbolAsync(dto.Symbol);

            if (asset == null)
                return Response.Fail("Activo no encontrado");

            var count = await _repo.CountAsync(userId);

            if (count >= _limits.MaxFavorites)
                return Response.Fail("Límite de favoritos alcanzado");

            var exists = await _repo.ExistsAsync(userId, asset.Id);

            if (exists)
                return Response.Fail("El activo ya está en favoritos");

            await _repo.AddAsync(new Favorite
            {
                UserId = userId,
                AssetId = asset.Id
            });

            await _uow.SaveChangesAsync();

            _logger.LogInformation("Favorito agregado {Symbol} para usuario {UserId}", dto.Symbol, userId);

            return Response.Ok(null);
        }

        public async Task<Response> RemoveAsync(int userId, string symbol)
        {
            var asset = await _assetRepo.GetBySymbolAsync(symbol);

            if (asset == null)
                return Response.Fail("Activo no encontrado");

            var fav = await _repo.GetByUserAndAssetAsync(userId, asset.Id);

            if (fav == null)
                return Response.Fail("Favorito no encontrado");

            _repo.Remove(fav);

            await _uow.SaveChangesAsync();

            _logger.LogInformation("Favorito eliminado {Symbol} para usuario {UserId}", symbol, userId);

            return Response.Ok(null);
        }

        public async Task<Response> GetCountAsync(int userId)
        {
            var count = await _repo.CountAsync(userId);

            return Response.Ok(new { count });
        }
    }
}
