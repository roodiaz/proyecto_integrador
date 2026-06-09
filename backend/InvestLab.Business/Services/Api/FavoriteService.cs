using InvestLab.Business.Interfaces.Api;
using InvestLab.Data;
using InvestLab.Data.Interfaces;
using InvestLab.Models;
using InvestLab.Models.DTOs.Favorite;
using InvestLab.Models.DTOs.Market;
using InvestLab.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static InvestLab.Models.MessageCodes;

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

            var favorites = await _favoriteRepo.GetFilteredAsync(userId, filter);

            var symbols = favorites.Select(x => x.Asset.Symbol).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            var marketPricesResponse = symbols.Count == 0 ? new MarketPricesResponseDto() : await _marketPriceCacheService.GetPricesAsync(symbols);
            var pricesBySymbol = marketPricesResponse.Prices.ToDictionary(x => x.Symbol, x => x, StringComparer.OrdinalIgnoreCase);

            var items = favorites.Select(fav =>
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
            }).ToList();

            var desc = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            items = filter.SortBy switch
            {
                "symbol" => desc ? items.OrderByDescending(x => x.Symbol).ToList() : items.OrderBy(x => x.Symbol).ToList(),

                "name" => desc ? items.OrderByDescending(x => x.Name).ToList() : items.OrderBy(x => x.Name).ToList(),

                "price" => desc ? items.OrderByDescending(x => x.Price).ToList() : items.OrderBy(x => x.Price).ToList(),

                "variationPercent" => desc ? items.OrderByDescending(x => x.VariationPercent).ToList() : items.OrderBy(x => x.VariationPercent).ToList(),

                _ => items.OrderBy(x => x.Symbol).ToList()
            };

            var total = items.Count;

            var paged = items.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();

            return Response.Ok(new
            {
                Items = paged,
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
                return Response.Fail("Configuración de usuario no encontrada", USER_SETTINGS_NOT_FOUND);

            var asset = await _assetService.GetOrCreateAsync(dto.Symbol);
            if (asset == null)
                return Response.Fail("Activo no encontrado", ASSET_NOT_FOUND);

            if (settings.FavoritesUsed >= _limits.MaxFavorites)
                return Response.Fail("Límite de favoritos alcanzado", MAX_FAVORITES_REACHED);

            var exists = await _favoriteRepo.ExistsAsync(userId, asset.Id);
            if (exists)
                return Response.Fail("El activo ya está en favoritos", FAVORITE_ALREADY_EXISTS);

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
                return Response.Fail("Configuración de usuario no encontrada", USER_SETTINGS_NOT_FOUND);

            var asset = await _assetService.GetOrCreateAsync(symbol);
            if (asset == null)
                return Response.Fail("Activo no encontrado", ASSET_NOT_FOUND);

            var fav = await _favoriteRepo.GetByUserAndAssetAsync(userId, asset.Id);
            if (fav == null)
                return Response.Fail("Favorito no encontrado", FAVORITE_NOT_FOUND);

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
                return Response.Fail("Debe indicar un símbolo válido", INVALID_SYMBOL);

            var asset = await _assetService.GetOrCreateAsync(symbol);
            if (asset == null)
                return Response.Fail("Activo no encontrado", ASSET_NOT_FOUND);

            var exists = await _favoriteRepo.ExistsAsync(userId, asset.Id);

            return Response.Ok(exists, "Consulta realizada correctamente");
        }
    }
}
