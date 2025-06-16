using MongoDB.Bson.Serialization.Attributes;

namespace CoinUtilsPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Symbol : BaseDTO
    {
        public string s { get; set; }
        public int ex { get; set; }//EExchange
        public int ty { get; set; }//OrderSide  -1: all
        public int rank { get; set; }
        public int status { get; set; }//0: bình thường, -1: bỏ
    }
}
