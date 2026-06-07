using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data.Repositories
{
    public class UserTempCredentialRepository : IUserTempCredentialRepository
    {
        private readonly InvestLabDbContext _context;

        /// <summary>
        /// Inicializa una nueva instancia del repositorio de credenciales temporales de usuario.
        /// </summary>
        /// <param name="context">Contexto de base de datos de InvestLab.</param>
        public UserTempCredentialRepository(InvestLabDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Agrega una nueva credencial temporal al contexto de base de datos.
        /// </summary>
        /// <param name="credential">Entidad de credencial temporal a agregar.</param>
        public async Task AddAsync(UserTempCredential credential)
        {
            await _context.UserTempCredentials.AddAsync(credential);
        }

        /// <summary>
        /// Obtiene la credencial temporal asociada a un usuario.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <returns>La credencial temporal encontrada, o null si no existe.</returns>
        public async Task<UserTempCredential?> GetByUserIdAsync(int userId)
        {
            return await _context.UserTempCredentials
                .Where(x => x.UserId == userId)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Obtiene la credencial temporal más reciente y no utilizada de un usuario.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <returns>La credencial temporal más reciente sin usar, o null si no existe.</returns>
        public async Task<UserTempCredential?> GetLatestAsync(int userId)
        {
            return await _context.UserTempCredentials
                .Where(x => x.UserId == userId && !x.IsUsed)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Elimina todas las credenciales temporales asociadas a un usuario.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        public async Task DeleteByUserIdAsync(int userId)
        {
            await _context.UserTempCredentials.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        }
    }
}
