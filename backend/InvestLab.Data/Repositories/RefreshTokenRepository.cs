using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data.Repositories
{
    public class RefreshTokenRepository: IRefreshTokenRepository
    {
        private readonly InvestLabDbContext _context;

        /// <summary>
        /// Inicializa una nueva instancia del repositorio de tokens de actualización.
        /// </summary>
        /// <param name="context">Contexto de base de datos de InvestLab.</param>
        public RefreshTokenRepository(InvestLabDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene un token de actualización a partir de su valor, incluyendo los datos del usuario asociado.
        /// </summary>
        /// <param name="token">Valor del token de actualización a buscar.</param>
        /// <returns>El token de actualización encontrado, o null si no existe.</returns>
        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == token);
        }

        /// <summary>
        /// Agrega un nuevo token de actualización al contexto de base de datos.
        /// </summary>
        /// <param name="token">Entidad de token de actualización a agregar.</param>
        public async Task AddAsync(RefreshToken token)
        {
            await _context.RefreshTokens.AddAsync(token);
        }

        /// <summary>
        /// Elimina todos los tokens de actualización asociados a un usuario.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        public async Task DeleteByUserIdAsync(int userId)
        {
            await _context.RefreshTokens.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        }
    }
}
