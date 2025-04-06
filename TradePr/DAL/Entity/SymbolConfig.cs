using MongoDB.Bson.Serialization.Attributes;

namespace TradePr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class SymbolConfig : BaseDTO
    {
        public string s { get; set; }
        public int amount { get; set; }//So luong
        public int price { get; set; }//Gia
    }
}
