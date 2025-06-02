using MongoDB.Bson.Serialization.Attributes;

namespace TradePr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Prepare : BaseDTO
    {
        public int Index { get; set; }
        public string s { get; set; }
        public int ex { get; set; }
        public int side { get; set; }
        public int op { get; set; }

        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
    }
}
