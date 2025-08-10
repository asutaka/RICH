using MongoDB.Bson.Serialization.Attributes;
using Skender.Stock.Indicators;

namespace CoinUtilsPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class SOSDTO : BaseDTO
    {
        public Quote sos { get; set; }//Nến SOS
        public Quote signal { get; set; }//Nến tín hiệu cắt xuống MA20
    }
}
