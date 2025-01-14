using MongoDB.Bson.Serialization.Attributes;

namespace CoinPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Coin : BaseDTO
    {
        public string symbol { get; set; }
        public string FromAsset { get; set; }
        public string ToAsset { get; set; }
        public string ContractKey { get; set; }
        public string Description { get; set; }
    }
}
