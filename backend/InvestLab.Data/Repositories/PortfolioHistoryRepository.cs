using InvestLab.Data.Context;
using InvestLab.Data.Interfaces;
using InvestLab.Models.Documents;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace InvestLab.Data.Repositories
{
    internal class PortfolioHistoryRepository : IPortfolioHistoryRepository
    {
        private readonly IMongoCollection<PortfolioHistory> _collection;

        /// <summary>
        /// Inicializa una nueva instancia del repositorio de historial de portafolio, obteniendo la colección de Mongo correspondiente.
        /// </summary>
        /// <param name="database">Base de datos de MongoDB de la cual se obtiene la colección.</param>
        public PortfolioHistoryRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<PortfolioHistory>("portfolio_history");
        }

        /// <summary>
        /// Obtiene el historial de portafolio de un usuario a partir de una fecha determinada, ordenado por fecha ascendente.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <param name="fromDate">Fecha mínima a partir de la cual se incluyen los registros.</param>
        /// <returns>Lista de registros de historial de portafolio que cumplen la condición.</returns>
        public async Task<List<PortfolioHistory>> GetByUserAndDateAsync(int userId, DateTime fromDate)
        {
            return await _collection
                .Find(x => x.UserId == userId && x.Date >= fromDate)
                .SortBy(x => x.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Inserta un nuevo registro de historial de portafolio en la colección.
        /// </summary>
        /// <param name="history">Registro de historial de portafolio a insertar.</param>
        public async Task InsertAsync(PortfolioHistory history)
        {
            await _collection.InsertOneAsync(history);
        }

        /// <summary>
        /// Obtiene el registro de historial de portafolio más reciente de un usuario.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <returns>El registro más reciente del historial, o <c>null</c> si no existe ninguno.</returns>
        public async Task<PortfolioHistory?> GetLatestAsync(int userId)
        {
            return await _collection
                .Find(x => x.UserId == userId)
                .SortByDescending(x => x.Date)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Obtiene el penúltimo registro de historial de portafolio de un usuario (el anterior al más reciente).
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <returns>El registro anterior al más reciente del historial, o <c>null</c> si no existe.</returns>
        public async Task<PortfolioHistory?> GetPreviousAsync(int userId)
        {
            return await _collection
                .Find(x => x.UserId == userId)
                .SortByDescending(x => x.Date)
                .Skip(1)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Verifica si existe un registro de historial de portafolio para un usuario en una fecha específica (sin considerar la hora).
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <param name="date">Fecha a verificar (se considera el rango de todo ese día).</param>
        /// <returns><c>true</c> si existe al menos un registro para esa fecha; en caso contrario, <c>false</c>.</returns>
        public async Task<bool> ExistsByDateAsync(int userId,DateTime date)
        {
            var start = date.Date;
            var end = start.AddDays(1);

            return await _collection.Find(
                    x =>
                        x.UserId == userId &&
                        x.Date >= start &&
                        x.Date < end)
                .Limit(1)
                .FirstOrDefaultAsync() != null;
        }

        /// <summary>
        /// Elimina todos los registros de historial de portafolio asociados a un usuario.
        /// </summary>
        /// <param name="userId">Identificador del usuario cuyo historial se eliminará.</param>
        public async Task DeleteByUserIdAsync(int userId)
        {
            var filter = Builders<PortfolioHistory>.Filter.Eq(x => x.UserId, userId);
            await _collection.DeleteManyAsync(filter);
        }
    }
}
