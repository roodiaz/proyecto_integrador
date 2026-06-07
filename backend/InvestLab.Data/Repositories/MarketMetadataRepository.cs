using InvestLab.Data.Interfaces;
using InvestLab.Models.Documents;
using MongoDB.Driver;


namespace InvestLab.Data.Repositories
{
    public class MarketMetadataRepository: IMarketMetadataRepository
    {
        private readonly IMongoCollection<MarketMetadata> _collection;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="MarketMetadataRepository"/> obteniendo la colección de metadatos de mercado de la base de datos.
        /// </summary>
        /// <param name="database">Instancia de la base de datos MongoDB desde donde se obtiene la colección.</param>
        public MarketMetadataRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<MarketMetadata>("market_metadata");
        }

        /// <summary>
        /// Obtiene de forma asincrónica el primer documento de metadatos de mercado disponible en la colección.
        /// </summary>
        /// <returns>Una tarea que representa la operación asincrónica, cuyo resultado contiene los metadatos de mercado encontrados o <c>null</c> si no existen.</returns>
        public async Task<MarketMetadata?> GetAsync()
        {
            return await _collection
                .Find(_ => true)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Actualiza de forma asincrónica la fecha del último cierre de mercado y la fecha de última sincronización, creando el documento si no existe.
        /// </summary>
        /// <param name="marketCloseDate">Fecha del último cierre de mercado a registrar.</param>
        /// <returns>Una tarea que representa la operación asincrónica de actualización.</returns>
        public async Task UpdateLastCloseAsync(DateTime marketCloseDate)
        {
            var update =
                Builders<MarketMetadata>
                    .Update
                    .Set(
                        x => x.LastMarketCloseDate,
                        marketCloseDate)
                    .Set(
                        x => x.LastSyncDate,
                        DateTime.UtcNow);

            await _collection.UpdateOneAsync(_ => true, update,
                new UpdateOptions
                {
                    IsUpsert = true
                });
        }
    }
}
