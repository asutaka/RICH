using MongoDB.Bson.Serialization.Attributes;
using Skender.Stock.Indicators;

namespace CoinUtilsPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Prepare : BaseDTO
    {
        public string s { get; set; }
        public int t { get; set; }
        public int ex { get; set; }
        public int side { get; set; }
        public int status { get; set; }// 1: active, 2: disable
        public Quote sos { get; set; }
        public Quote signal { get; set; }
    }
}
