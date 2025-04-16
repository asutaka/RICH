using MongoDB.Bson.Serialization.Attributes;

namespace StockPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class ConfigF319 : BaseDTO
    {
        public int d { get; set; }
        public string user { get; set; }
    }
}
