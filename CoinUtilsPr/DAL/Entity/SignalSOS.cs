using MongoDB.Bson.Serialization.Attributes;
using Skender.Stock.Indicators;

namespace CoinUtilsPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class SignalSOS : BaseDTO
    {
        public string s { get; set; }
        public int d { get; set; }
        public Quote sos { get; set; }
        public int status { get; set; }
        public int ty { get; set; }//1: Fast 2: Low
    }
}
