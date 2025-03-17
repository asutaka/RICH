using MongoDB.Bson.Serialization.Attributes;

namespace TradePr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class PrepareTrade : BaseDTO
    {
        public string s { get; set; }
        public int Side { get; set; }//Buy Or Sell
        public int ty { get; set; }//Liquid or RSI
        public int detectTime { get; set; }//Date
        public DateTime detectDate { get; set; }
        public int entryTime { get; set; }
        public DateTime entryDate { get; set; }
        public int stopTime { get; set; }
        public DateTime stopDate { get; set; }
        public double Entry { get; set; }
        public double Entry_Real { get; set; }
        public double SL_Real { get; set; }
        public int Status { get; set; }
    }
}
