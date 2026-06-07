using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data.Repositories
{
    public class UserSettingRepository : IUserSettingRepository
    {
        private readonly InvestLabDbContext _context;

        /// <summary>
        /// Inicializa una nueva instancia del repositorio de configuraciones de usuario.
        /// </summary>
        /// <param name="context">Contexto de base de datos de InvestLab.</param>
        public UserSettingRepository(InvestLabDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Agrega una nueva configuración de usuario al contexto de base de datos.
        /// </summary>
        /// <param name="setting">Configuración de usuario a agregar.</param>
        public async Task AddAsync(UserSetting setting)
        {
            await _context.UserSettings.AddAsync(setting);
        }

        /// <summary>
        /// Obtiene la configuración asociada a un usuario según su identificador.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <returns>La configuración del usuario encontrada o <c>null</c> si no existe.</returns>
        public async Task<UserSetting?> GetByUserIdAsync(int userId)
        {
            return await _context.UserSettings.FirstOrDefaultAsync(x => x.UserId == userId);
        }

        /// <summary>
        /// Restablece a cero los límites diarios de búsquedas y operaciones de todos los usuarios.
        /// </summary>
        public async Task ResetDailyLimitsAsync()
        {
            await _context.UserSettings
                .ExecuteUpdateAsync(setters =>
                    setters
                        .SetProperty(x => x.SearchesUsedToday, 0)
                        .SetProperty(x => x.OperationsUsedToday, 0));

           await  _context.SaveChangesAsync();
        }

        /// <summary>
        /// Restablece a cero la cantidad de operaciones utilizadas hoy por un usuario específico.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        public async Task ResetOperationsUsedTodayAsync(int userId)
        {
            var settings = await _context.UserSettings.FirstOrDefaultAsync(x => x.UserId == userId);

            if (settings == null)
                return;

            settings.OperationsUsedToday = 0;
        }

        /// <summary>
        /// Elimina la configuración de usuario asociada a un identificador de usuario.
        /// </summary>
        /// <param name="userId">Identificador del usuario cuya configuración se eliminará.</param>
        public async Task DeleteByUserIdAsync(int userId)
        {
            await _context.UserSettings.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        }
    }
}
