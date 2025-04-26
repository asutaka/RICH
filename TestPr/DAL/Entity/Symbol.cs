using MongoDB.Bson.Serialization.Attributes;

namespace TestPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Symbol : BaseDTO
    {
        public string s { get; set; }
        public int ex { get; set; }//EExchange
        public int ty { get; set; }//OrderSide  -1: all
        public int rank { get; set; }
    }
}
