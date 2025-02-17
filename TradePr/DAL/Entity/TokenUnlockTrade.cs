using MongoDB.Bson.Serialization.Attributes;

namespace TradePr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class TokenUnlockTrade : BaseDTO
    {
        public string s { get; set; }
        public int time { get; set; }
        public double entry { get; set; }
        public double priceSell { get; set; }
        public double rate { get; set; }
        public double vithe { get; set; }
        public double lailo { get; set; }
        public bool isTP { get; set; }
    }
}
