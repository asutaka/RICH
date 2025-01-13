using MongoDB.Bson.Serialization.Attributes;

namespace CoinPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class UserMessage : BaseDTO
    {
        public long u { get; set; }//userid
        public int ty { get; set; }//type
        public long t { get; set; }//time
    }
}
