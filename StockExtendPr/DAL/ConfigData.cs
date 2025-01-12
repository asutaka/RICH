using MongoDB.Bson.Serialization.Attributes;
using StockExtendPr.DAL.Entity;

namespace StockExtendPr.DAL
{
    [BsonIgnoreExtraElements]
    public class ConfigData : BaseDTO
    {
        public int ty { get; set; }//type
        public long t { get; set; }//time
    }
}
