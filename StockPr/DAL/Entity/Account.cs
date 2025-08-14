using MongoDB.Bson.Serialization.Attributes;

namespace StockPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Account : BaseDTO
    {
        public long u { get; set; }//userid
        public string name { get; set; }
        public int status { get; set; }//type
    }
}
