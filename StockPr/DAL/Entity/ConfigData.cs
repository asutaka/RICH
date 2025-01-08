using MongoDB.Bson.Serialization.Attributes;

namespace StockPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class ConfigData : BaseDTO
    {
        public int ty { get; set; }//type
        public long t { get; set; }//time
    }
}
