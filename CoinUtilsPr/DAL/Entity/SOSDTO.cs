using MongoDB.Bson.Serialization.Attributes;
using Skender.Stock.Indicators;

namespace CoinUtilsPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class SOSDTO : BaseDTO
    {
        public Quote sos { get; set; }//Nến SOS
        public Quote sos_real { get; set; }//Nến TOP-BOT sau SOS
        public Quote signal { get; set; }//Nến tín hiệu cắt xuống MA20
        public int ty { get; set; }//1: Fast 2: Low
        public decimal distance_unit { get; set; }
        public decimal sl { get; set; }
        public decimal tp { get; set; }
        public bool allowSell { get; set; }
    }
}
