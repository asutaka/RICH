using MongoDB.Bson.Serialization.Attributes;

namespace StockPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class PreEntry : BaseDTO
    {
        public string s { get; set; }
        public int d { get; set; }
        public bool isPrePressure { get; set; }
        public bool isPreNN1 { get; set; }
    }
}
