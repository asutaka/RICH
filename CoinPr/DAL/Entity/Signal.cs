using MongoDB.Bson.Serialization.Attributes;

namespace CoinPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Signal : BaseDTO
    {
        public DateTime Date { get; set; }
        public double Channel { get; set; }
        public string Content { get; set; }
    }
}
