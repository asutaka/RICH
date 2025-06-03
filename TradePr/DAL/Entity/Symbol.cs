using MongoDB.Bson.Serialization.Attributes;

namespace TradePr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Symbol : BaseDTO
    {
        public string s { get; set; }
        public int ex { get; set; }//EExchange
        public int ty { get; set; }//OrderSide  -1: all
        public int op { get; set; }//Sell 0, Sell 1, Sell 2, Buy 0, Buy 1, Buy 2 : 0: entry 2.5, 1: entry 1.5, 2: entry 1
        public int rank { get; set; }
        public int status { get; set; }//0: bình thường, -1: bỏ
    }
}
