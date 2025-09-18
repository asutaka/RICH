using MongoDB.Bson.Serialization.Attributes;
using Skender.Stock.Indicators;

namespace CoinUtilsPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class EntrySOS : BaseDTO
    {
        public string s { get; set; }
        public int d { get; set; }
        public Quote sos { get; set; }
        public Quote signal { get; set; }
        public int status { get; set; }
        public int ty { get; set; }//1: Fast 2: Low
        public int side { get; set; }

        public double entry { get; set; }
        public double tp { get; set; }
        public double sl { get; set; }
        public double distance_unit { get; set; }
        public double rate { get; set; }
        public bool allow_sell { get; set; }

    }
}
