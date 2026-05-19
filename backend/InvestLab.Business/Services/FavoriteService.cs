using InvestLab.Business.Interfaces;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Favorite;
using Microsoft.Extensions.Logging;

namespace InvestLab.Business.Services
{
    public class FavoriteService : IFavoriteService
    {
        private readonly LimitsOptions _limits;
        private readonly IFavoriteRepository _repo;
        private readonly IAssetRepository _assetRepo;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<FavoriteService> _logger;

        public FavoriteService(IFavoriteRepository repo, IAssetRepository assetRepo, IUnitOfWork uow, ILogger<FavoriteService> logger, LimitsOptions limits)
        {
            _repo = repo;
            _assetRepo = assetRepo;
            _uow = uow;
            _logger = logger;
            _limits = limits;
        }

        public async Task<Response> GetAsync(int userId)
        {
            var list = await _repo.GetByUserAsync(userId);

            var result = list.Select(x => new FavoriteDto
            {
                Id = x.Id,
                Symbol = x.Asset.Symbol
            });

            return Response.Ok(result);
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
