using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InvestLab.Models.Documents
{
    public class PortfolioHistory
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("userId")]
        public int UserId { get; set; }

        [BsonElement("portfolioId")]
        public int PortfolioId { get; set; }

        [BsonElement("date")]
        public DateTime Date { get; set; }

        [BsonElement("availableBalance")]
        [BsonIgnoreIfNull]
        public decimal? AvailableBalance { get; set; }

        [BsonElement("investedValue")]
        [BsonIgnoreIfNull]
        public decimal? InvestedValue { get; set; }

        [BsonElement("totalValue")]
        public decimal TotalValue { get; set; }
    }
}
