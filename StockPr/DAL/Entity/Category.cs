using MongoDB.Bson.Serialization.Attributes;

namespace StockPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Category : BaseDTO
    {
        public string code { get; set; }
        public string name { get; set; }
    }
}
