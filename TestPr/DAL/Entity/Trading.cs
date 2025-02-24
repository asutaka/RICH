using MongoDB.Bson.Serialization.Attributes;

namespace TestPr.DAL.Entity
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
        public DateTime Date { get; set; }
        //For Test
        public double CurrentPrice { get; set; }
        public double AvgPrice { get; set; }
        public double Liquid { get; set; }
        public double Rsi { get; set; }//5p
        public double TopFirst { get; set; }
        public double TopNext { get; set; }
        public double BotFirst { get; set; }
        public double BotNext { get; set; }
        public double RateVol { get; set; }
        public double TP_2 { get; set; }
        public double TP_3 { get; set; }
        public double TP25 { get; set; }
        public double SL_2 { get; set; }
        public double SL25 { get; set; }
    }
}
