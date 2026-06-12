using InvestLab.Data.Interfaces;
using InvestLab.Models.Documents;
using MongoDB.Bson;
using MongoDB.Driver;

namespace InvestLab.Data.Repositories
{
    internal class PortfolioHistoryRepository : IPortfolioHistoryRepository
    {
        private readonly IMongoCollection<PortfolioHistory> _collection;
        private readonly IMongoCollection<BsonDocument> _rawCollection;

        /// <summary>
        /// Inicializa una nueva instancia del repositorio de historial de portafolio, obteniendo la colección de Mongo correspondiente.
        /// </summary>
        /// <param name="database">Base de datos de MongoDB de la cual se obtiene la colección.</param>
        public PortfolioHistoryRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<PortfolioHistory>("portfolio_history");
            _rawCollection = database.GetCollection<BsonDocument>("portfolio_history");

            var indexKeys = Builders<PortfolioHistory>.IndexKeys
                .Ascending(x => x.PortfolioId)
                .Ascending(x => x.Date);

            _collection.Indexes.CreateOne(new CreateIndexModel<PortfolioHistory>(indexKeys));
        }

        /// <summary>
        /// Obtiene el historial de portafolio a partir de una fecha determinada, ordenado por fecha ascendente.
        /// </summary>
        /// <param name="portfolioId">Identificador del portfolio.</param>
        /// <param name="fromDate">Fecha mínima a partir de la cual se incluyen los registros.</param>
        /// <returns>Lista de registros de historial de portafolio que cumplen la condición.</returns>
        public async Task<List<PortfolioHistory>> GetByPortfolioAndDateAsync(int portfolioId, DateTime fromDate)
        {
            return await _collection
                .Find(x => x.PortfolioId == portfolioId && x.Date >= fromDate)
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
        /// Obtiene el registro de historial de portafolio más reciente de un portfolio.
        /// </summary>
        /// <param name="portfolioId">Identificador del portfolio.</param>
        /// <returns>El registro más reciente del historial, o <c>null</c> si no existe ninguno.</returns>
        public async Task<PortfolioHistory?> GetLatestAsync(int portfolioId)
        {
            return await _collection
                .Find(x => x.PortfolioId == portfolioId)
                .SortByDescending(x => x.Date)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Obtiene el penúltimo registro de historial de portafolio de un portfolio (el anterior al más reciente).
        /// </summary>
        /// <param name="portfolioId">Identificador del portfolio.</param>
        /// <returns>El registro anterior al más reciente del historial, o <c>null</c> si no existe.</returns>
        public async Task<PortfolioHistory?> GetPreviousAsync(int portfolioId)
        {
            return await _collection
                .Find(x => x.PortfolioId == portfolioId)
                .SortByDescending(x => x.Date)
                .Skip(1)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Verifica si existe un registro de historial de portafolio para un portfolio en una fecha específica (sin considerar la hora).
        /// </summary>
        /// <param name="portfolioId">Identificador del portfolio.</param>
        /// <param name="date">Fecha a verificar (se considera el rango de todo ese día).</param>
        /// <returns><c>true</c> si existe al menos un registro para esa fecha; en caso contrario, <c>false</c>.</returns>
        public async Task<bool> ExistsByDateAsync(int portfolioId, DateTime date)
        {
            var start = date.Date;
            var end = start.AddDays(1);

            return await _collection.Find(
                    x =>
                        x.PortfolioId == portfolioId &&
                        x.Date >= start &&
                        x.Date < end)
                .Limit(1)
                .FirstOrDefaultAsync() != null;
        }

        /// <summary>
        /// Obtiene el snapshot más reciente del historial de portafolio en o antes de la fecha indicada.
        /// Si existe un registro exacto para esa fecha lo retorna; de lo contrario retorna el más reciente anterior.
        /// </summary>
        /// <param name="portfolioId">Identificador del portfolio.</param>
        /// <param name="date">Fecha de referencia (inclusive).</param>
        /// <returns>El snapshot más reciente en o antes de la fecha dada, o <c>null</c> si no existe ninguno.</returns>
        public async Task<PortfolioHistory?> GetOnOrBeforeAsync(int portfolioId, DateTime date)
        {
            var end = date.Date.AddDays(1);

            return await _collection
                .Find(x => x.PortfolioId == portfolioId && x.Date < end)
                .SortByDescending(x => x.Date)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Elimina todos los registros de historial de portafolio asociados a un portfolio.
        /// </summary>
        /// <param name="portfolioId">Identificador del portfolio cuyo historial se eliminará.</param>
        public async Task DeleteByPortfolioIdAsync(int portfolioId)
        {
            var filter = Builders<PortfolioHistory>.Filter.Eq(x => x.PortfolioId, portfolioId);
            await _collection.DeleteManyAsync(filter);
        }

        /// <inheritdoc />
        public async Task BackfillPortfolioIdsAsync(IReadOnlyDictionary<int, int> portfolioIdByUserId)
        {
            foreach (var (userId, portfolioId) in portfolioIdByUserId)
            {
                var filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("userId", userId),
                    Builders<BsonDocument>.Filter.Exists("portfolioId", false));

                var update = Builders<BsonDocument>.Update.Set("portfolioId", portfolioId);

                await _rawCollection.UpdateManyAsync(filter, update);
            }
        }
    }
}
