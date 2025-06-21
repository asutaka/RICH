using MongoDB.Bson.Serialization.Attributes;

namespace CoinUtilsPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Trading : BaseDTO
    {
        public int ex { get; set; }
        public string s { get; set; }
        public int d { get; set; }//Date
        public DateTime Date { get; set; }
        public int Side { get; set; }//Buy Or Sell 
        public int Op { get; set; }
        public double Entry { get; set; }
        public double SL { get; set; }//Giá StopLoss
        public double RateTP { get; set; }//Rate TakeProfit
        public double RateClose { get; set; }//Rate đóng lệnh
        public int Status { get; set; }//0: Available; 1: Close
    }
}
