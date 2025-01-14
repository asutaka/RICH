using MongoDB.Bson.Serialization.Attributes;

namespace CoinPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class BlackList : BaseDTO
    {
        public string ContractKey { get; set; }
    }
}
