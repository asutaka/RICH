using MongoDB.Bson.Serialization.Attributes;

namespace TestPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Trading : BaseDTO
    {
        public string key { get; set; }
        public string s { get; set; }
        public int d { get; set; }//Date
        public DateTime Date { get; set; }
        public int Side { get; set; }//Buy Or Sell
        public double Entry { get; set; }//Only OrderBlock
        public double SL { get; set; }//Only OrderBlock
        public double TP { get; set; }//Only OrderBlock
        public double Liquid { get; set; }
        public double Rsi { get; set; }//5p
        public double RateVol { get; set; }
        public int Status { get; set; }//Only Liquid
    }
}
