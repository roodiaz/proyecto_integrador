using InvestLab.Data.Interfaces;
using InvestLab.Models.Documents;
using MongoDB.Driver;


namespace InvestLab.Data.Repositories
{
    public class MarketMetadataRepository: IMarketMetadataRepository
    {
        private readonly IMongoCollection<MarketMetadata> _collection;

        public MarketMetadataRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<MarketMetadata>("market_metadata");
        }

        public async Task<MarketMetadata?> GetAsync()
        {
            return await _collection
                .Find(_ => true)
                .FirstOrDefaultAsync();
        }

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
