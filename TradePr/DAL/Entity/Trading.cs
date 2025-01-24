using MongoDB.Bson.Serialization.Attributes;

namespace TradePr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Trading : BaseDTO
    {
        public string key { get; set; }
        public string s { get; set; }
        public int d { get; set; }//Date
        public int Side { get; set; }//Buy Or Sell
        public double Entry { get; set; }//Only OrderBlock
        public double SL { get; set; }//Only OrderBlock
        public double TP { get; set; }//Only OrderBlock
        public double Focus { get; set; }
        public int Status { get; set; }//Only Liquid

        public double AvgPrice { get; set; }
        public double PriceAtLiquid { get; set; }
        public double CurrentPrice { get; set; }
        public int Mode { get; set; }
        public DateTime Date { get; set; }


        public double Rsi_5 { get; set; }//5p
        public double Rsi_15 { get; set; }//15
        public double Top_1 { get; set; }
        public double Top_2 { get; set; }
        public double Bot_1 { get; set; }
        public double Bot_2 { get; set; }
        public int Case { get; set; }
    }
}
