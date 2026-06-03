using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InvestLab.Models.Documents
{
    public class MarketMetadata
    {
        [BsonId]
        public ObjectId Id { get; set; }

        /// <summary>
        /// Fecha correspondiente al último cierre de mercado
        /// utilizado para generar los precios históricos.
        /// </summary>
        [BsonElement("last_market_close_date")]
        public DateTime LastMarketCloseDate { get; set; }

        /// <summary>
        /// Fecha en que se ejecutó exitosamente
        /// la sincronización de precios.
        /// </summary>
        [BsonElement("last_sync_date")]
        public DateTime LastSyncDate { get; set; }
    }
}