using InvestLab.Business.Interfaces.Api;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Favorite;
using InvestLab.Models.DTOs.Market;
using InvestLab.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvestLab.Business.Services.Api
{
    public class FavoriteService : IFavoriteService
    {
        private readonly ILogger<FavoriteService> _logger;
        private readonly LimitsOptions _limits;
        private readonly IAssetService _assetService;
        private readonly IMarketPriceCacheService _marketPriceCacheService;

        private readonly IUserSettingRepository _userSettingRepository;
        private readonly IFavoriteRepository _favoriteRepo;
        private readonly IAssetRepository _assetRepo;
        private readonly IUnitOfWork _uow;

        /// <summary>
        /// Inicializa una nueva instancia del servicio de favoritos.
        /// </summary>
        /// <param name="repo">Repositorio de favoritos.</param>
        /// <param name="assetRepo">Repositorio de activos.</param>
        /// <param name="uow">Unidad de trabajo para confirmar los cambios en la base de datos.</param>
        /// <param name="logger">Registrador de eventos del servicio.</param>
        /// <param name="options">Opciones de configuración con los límites del sistema.</param>
        /// <param name="assetService">Servicio de gestión de activos.</param>
        /// <param name="userSettingRepository">Repositorio de configuraciones de usuario.</param>
        /// <param name="marketPriceCacheService">Servicio de cache de precios de mercado para datos informativos.</param>
        public FavoriteService(IFavoriteRepository repo, IAssetRepository assetRepo, IUnitOfWork uow, ILogger<FavoriteService> logger, IOptions<LimitsOptions> options, IAssetService assetService, IUserSettingRepository userSettingRepository, IMarketPriceCacheService marketPriceCacheService)
        {
            _favoriteRepo = repo;
            _assetRepo = assetRepo;
            _uow = uow;
            _logger = logger;
            _limits = options.Value;
            _assetService = assetService;
            _userSettingRepository = userSettingRepository;
            _marketPriceCacheService = marketPriceCacheService;
        }

        /// <summary>
        /// Obtiene el listado paginado de activos favoritos de un usuario, incluyendo precios
        /// y variaciones obtenidas del cache de precios de mercado.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <param name="filter">Filtros de paginación a aplicar sobre el listado de favoritos.</param>
        /// <returns>Respuesta con los favoritos del usuario, el total de registros y la información de límites configurados.</returns>
        public async Task<Response> GetAsync(int userId, FavoriteFilterDto filter)
        {
            var settings = await _userSettingRepository.GetByUserIdAsync(userId);

            var (list, total) = await _favoriteRepo.GetPagedAsync(userId, filter);

            var symbols = list.Select(x => x.Asset.Symbol).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            var marketPricesResponse = symbols.Count == 0 ? new MarketPricesResponseDto() : await _marketPriceCacheService.GetPricesAsync(symbols);
            var pricesBySymbol = marketPricesResponse.Prices.ToDictionary(x => x.Symbol, x => x, StringComparer.OrdinalIgnoreCase);

            var result = list.Select(fav =>
            {
                pricesBySymbol.TryGetValue(fav.Asset.Symbol, out var market);

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

        /// <summary>
        /// Agrega un activo a la lista de favoritos del usuario, validando la configuración del usuario,
        /// la existencia del activo, el límite máximo de favoritos y que no esté ya agregado.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <param name="dto">Datos del favorito a agregar, incluyendo el símbolo del activo.</param>
        /// <returns>Respuesta indicando si la operación fue exitosa o el motivo del error.</returns>
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

        /// <summary>
        /// Elimina un activo de la lista de favoritos del usuario, validando la configuración del usuario,
        /// la existencia del activo y que el favorito esté registrado.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <param name="symbol">Símbolo del activo a eliminar de favoritos.</param>
        /// <returns>Respuesta indicando si la operación fue exitosa o el motivo del error.</returns>
        public async Task<Response> RemoveAsync(int userId, string symbol)
        {
            symbol = symbol.Trim().ToUpper();

            var settings = await _userSettingRepository.GetByUserIdAsync(userId);
            if (settings == null)
                return Response.Fail("Configuración de usuario no encontrada");

            var asset = await _assetService.GetOrCreateAsync(symbol);
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

        /// <summary>
        /// Verifica si un activo identificado por su símbolo ya se encuentra entre los favoritos del usuario.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <param name="symbol">Símbolo del activo a verificar.</param>
        /// <returns>Respuesta indicando si el activo ya está marcado como favorito.</returns>
        public async Task<Response> ExistsAsync(int userId, string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return Response.Fail("Debe indicar un símbolo válido");

            var asset = await _assetService.GetOrCreateAsync(symbol);
            if (asset == null)
                return Response.Fail("Activo no encontrado");

            var exists = await _favoriteRepo.ExistsAsync(userId, asset.Id);

            return Response.Ok(exists, "Consulta realizada correctamente");
        }
    }
}
