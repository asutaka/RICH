using MongoDB.Bson.Serialization.Attributes;
namespace CoinPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Trading : BaseDTO
    {
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
    }
}
