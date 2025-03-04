using MongoDB.Bson.Serialization.Attributes;

namespace TradePr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class ThreeSignalTrade : BaseDTO
    {
        public string s { get; set; }
        public int ex { get; set; }
        public int Side { get; set; }
        public int timeFlag { get; set; }
        public int timeStoploss { get; set; }
        public int timeClose { get; set; }
        public double priceEntry { get; set; }
        public double priceStoploss { get; set; }
        public double priceClose { get; set; }
        public double rate { get; set; }
    }
}
