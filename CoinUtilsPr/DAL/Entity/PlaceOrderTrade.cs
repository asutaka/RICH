using MongoDB.Bson.Serialization.Attributes;

namespace CoinUtilsPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class PlaceOrderTrade : BaseDTO
    {
        public string s { get; set; }
        public int ex { get; set; }
        public DateTime time { get; set; }
    }
}
//public string s { get; set; }
//public int d { get; set; }//Date
//public DateTime Date { get; set; }
//public int Side { get; set; }//Buy Or Sell 
//public int Op { get; set; }
//public double Entry { get; set; }
//public double PriceClose { get; set; }//Giá đóng lệnh
//public double RateClose { get; set; }//Tỉ lệ đóng lệnh
//public double StopLoss { get; set; }//Giá StopLoss
//public double TakeProfit { get; set; }//Giá TakeProfit
//public int Status { get; set; }//0: Available; 1: Close