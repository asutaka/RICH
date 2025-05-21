using MongoDB.Bson.Serialization.Attributes;
using Skender.Stock.Indicators;

namespace TradePr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class SignalBase : BaseDTO
    {
        public string s { get; set; }
        public int ex { get; set; }
        public int Side { get; set; }
        public int timeFlag { get; set; }
        public DateTime dateFlag { get; set; }
        public int timeStoploss { get; set; }
        public int timeClose { get; set; }
        public double priceEntry { get; set; }
        public double priceStoploss { get; set; }
        public double priceClose { get; set; }
        public double rate { get; set; }
        public Quote quote { get; set; }
        public int rank { get; set; }
        public int status { get; set; }
    }
}
