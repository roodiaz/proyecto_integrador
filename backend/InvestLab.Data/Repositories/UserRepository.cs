using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly InvestLabDbContext _context;

        /// <summary>
        /// Inicializa una nueva instancia del repositorio de usuarios.
        /// </summary>
        /// <param name="context">Contexto de base de datos de InvestLab.</param>
        public UserRepository(InvestLabDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene un usuario a partir de su correo electrónico.
        /// </summary>
        /// <param name="email">Correo electrónico del usuario a buscar.</param>
        /// <returns>El usuario encontrado, o null si no existe.</returns>
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.Email == email);
        }

        /// <summary>
        /// Agrega un nuevo usuario al contexto de base de datos.
        /// </summary>
        /// <param name="user">Entidad de usuario a agregar.</param>
        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        /// <summary>
        /// Obtiene un usuario por su identificador, incluyendo su configuración asociada.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <returns>El usuario encontrado con su configuración, o null si no existe.</returns>
        public async Task<User?> GetByIdWithSettingsAsync(int userId)
        {
            return await _context.Users
                .Include(x => x.UserSetting)
                .FirstOrDefaultAsync(x => x.Id == userId);
        }

        /// <summary>
        /// Obtiene un usuario a partir de su identificador.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <returns>El usuario encontrado, o null si no existe.</returns>
        public async Task<User?> GetByIdAsync(int userId)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        }

        /// <summary>
        /// Marca un usuario existente como modificado en el contexto de base de datos.
        /// </summary>
        /// <param name="user">Entidad de usuario a actualizar.</param>
        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
        }

        /// <summary>
        /// Obtiene todos los usuarios activos.
        /// </summary>
        /// <returns>Lista de usuarios activos.</returns>
        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users.Where(x => x.IsActive).ToListAsync();
        }

        /// <summary>
        /// Elimina un usuario del contexto de base de datos.
        /// </summary>
        /// <param name="user">Entidad de usuario a eliminar.</param>
        public async Task DeleteAsync(User user)
        {
            _context.Users.Remove(user);
            await Task.CompletedTask;
        }
    }
}
