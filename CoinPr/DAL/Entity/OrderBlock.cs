using MongoDB.Bson.Serialization.Attributes;

namespace CoinPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class OrderBlock : BaseDTO
    {
        public DateTime Date { get; set; }
        public string s { get; set; }
        public int ex { get; set; }
        public int interval { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public int Mode { get; set; }
        public decimal Entry { get; set; }
        public decimal SL { get; set; }
        public decimal Focus { get; set; }
    }
}
