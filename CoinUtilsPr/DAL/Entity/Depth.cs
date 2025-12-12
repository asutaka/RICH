using MongoDB.Bson.Serialization.Attributes;

namespace CoinUtilsPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Depth : BaseDTO
    {
        public string s { get; set; }
        public int t { get; set; }
        public double buySellRatio { get; set; }//Tỉ lệ vol mua bán
        public double tilebidask { get; set; }// Tỉ lệ tổng vị thế bids/ ask(giá x số lượng vị thế)
    }
}
