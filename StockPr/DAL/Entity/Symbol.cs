using MongoDB.Bson.Serialization.Attributes;

namespace StockPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Symbol : BaseDTO
    {
        public string s { get; set; }
        public int rank { get; set; }
    }
}
