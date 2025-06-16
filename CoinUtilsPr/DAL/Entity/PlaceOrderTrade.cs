using MongoDB.Bson.Serialization.Attributes;

namespace CoinUtilsPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class PlaceOrderTrade : BaseDTO
    {
        public string s { get; set; }
        public int ex { get; set; }
        public DateTime time { get; set; }
    }
}
