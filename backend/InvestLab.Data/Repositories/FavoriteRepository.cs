using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using InvestLab.Models.DTOs.Favorite;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data.Repositories
{
    public class FavoriteRepository : IFavoriteRepository
    {
        private readonly InvestLabDbContext _context;

        /// <summary>
        /// Inicializa una nueva instancia del repositorio de favoritos.
        /// </summary>
        /// <param name="context">Contexto de base de datos de InvestLab.</param>
        public FavoriteRepository(InvestLabDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene la lista de favoritos de un usuario, incluyendo los datos del activo asociado.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <returns>Lista de favoritos pertenecientes al usuario.</returns>
        public async Task<List<Favorite>> GetByUserAsync(int userId)
        {
            return await _context.Favorites
                .Include(x => x.Asset)
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        /// <summary>
        /// Cuenta la cantidad de favoritos que tiene un usuario.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <returns>Cantidad total de favoritos del usuario.</returns>
        public async Task<int> CountAsync(int userId)
        {
            return await _context.Favorites
                .CountAsync(x => x.UserId == userId);
        }

        /// <summary>
        /// Verifica si existe un favorito para la combinación de usuario y activo indicados.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <param name="assetId">Identificador del activo.</param>
        /// <returns>true si el favorito existe; en caso contrario, false.</returns>
        public async Task<bool> ExistsAsync(int userId, int assetId)
        {
            return await _context.Favorites
                .AnyAsync(x => x.UserId == userId && x.AssetId == assetId);
        }

        /// <summary>
        /// Agrega un nuevo favorito al contexto de base de datos.
        /// </summary>
        /// <param name="favorite">Entidad de favorito a agregar.</param>
        public async Task AddAsync(Favorite favorite)
        {
            await _context.Favorites.AddAsync(favorite);
        }

        /// <summary>
        /// Obtiene el favorito correspondiente a un usuario y un activo específicos.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <param name="assetId">Identificador del activo.</param>
        /// <returns>El favorito encontrado, o null si no existe.</returns>
        public async Task<Favorite?> GetByUserAndAssetAsync(int userId, int assetId)
        {
            return await _context.Favorites
                .FirstOrDefaultAsync(x => x.UserId == userId && x.AssetId == assetId);
        }

        /// <summary>
        /// Elimina un favorito del contexto de base de datos.
        /// </summary>
        /// <param name="favorite">Entidad de favorito a eliminar.</param>
        public void Remove(Favorite favorite)
        {
            _context.Favorites.Remove(favorite);
        }

        /// <summary>
        /// Obtiene los favoritos de un usuario que cumplen el filtro de búsqueda, sin paginar ni ordenar
        /// (el precio actual y la variación dependen del cache de mercado, por lo que el ordenamiento
        /// y la paginación finales se resuelven en el servicio).
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <param name="filter">Datos de filtrado a aplicar sobre los favoritos.</param>
        /// <returns>La lista de favoritos que cumplen el filtro de búsqueda.</returns>
        public async Task<List<Favorite>> GetFilteredAsync(int userId, FavoriteFilterDto filter)
        {
            var query = _context.Favorites.Include(x => x.Asset).Where(x => x.UserId == userId);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim().ToUpper();

                query = query.Where(x =>
                    x.Asset.Symbol.Contains(search) ||
                    x.Asset.Name.Contains(search));
            }

            return await query.ToListAsync();
        }

        /// <summary>
        /// Elimina todos los favoritos asociados a un usuario.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        public async Task DeleteByUserIdAsync(int userId)
        {
            await _context.Favorites.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        }
    }
}
