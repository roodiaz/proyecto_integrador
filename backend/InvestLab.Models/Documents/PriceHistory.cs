using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class PriceHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonElement("symbol")]
    public string Symbol { get; set; } = default!;

    [BsonElement("data")]
    public List<PriceEntry> Data { get; set; } = [];

    public class PriceEntry
    {
        [BsonElement("date")]
        public DateTime Date { get; set; }

        [BsonElement("open")]
        public decimal Open { get; set; }

        [BsonElement("high")]
        public decimal High { get; set; }

        [BsonElement("low")]
        public decimal Low { get; set; }

        [BsonElement("close")]
        public decimal Close { get; set; }

        [BsonElement("volume")]
        public long Volume { get; set; }
    }
}