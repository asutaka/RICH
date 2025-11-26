using MongoDB.Bson.Serialization.Attributes;
using Skender.Stock.Indicators;

namespace CoinUtilsPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Pro : BaseDTO
    {
        public Quote entity { get; set; }
        public string s { get; set; }
        public decimal sl_price { get; set; }//Giá StopLoss
        public decimal sl_rate { get; set; }//Điểm StopLoss
        public string mes { get; set; }
        public decimal ratio { get; set; }//Phần trăm vào lệnh
        public int interval { get; set; }
        public int status { get; set; }//insert = 0, không quan tâm = -1
        public bool is_tp1 { get; set; }//điểm chốt lãi tại ma20
        public bool is_tp2 { get; set; }//điểm chốt lãi tại upperband
        public bool is_tp3 { get; set; }//điểm chốt lãi tại rsi cross ma9
        public bool is_sl { get; set; }
    }
}
