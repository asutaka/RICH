using MongoDB.Bson.Serialization.Attributes;

namespace StockPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class ConfigNews : BaseDTO
    {
        public int d { get; set; }
        public string key { get; set; }
    }
}
