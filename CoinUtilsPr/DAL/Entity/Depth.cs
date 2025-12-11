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
        public double priceBidsTop { get; set; }//Giá cao nhất trong các lệnh mua (bids)
        public double priceBidsBot { get; set; }//Giá thấp nhất trong các lệnh mua (bids)
        public double priceAsksTop { get; set; }//Giá cao nhất trong các lệnh bán (asks)
        public double priceAsksBot { get; set; }//Giá thấp nhất trong các lệnh bán (asks)
        public double posMaxBidsRatio { get; set; }//vị thế lớn nhất tại Bids chia cho trung bình vị thế tại Bids
        public double posMaxAskRatio { get; set; }//vị thế lớn nhất tại Asks chia cho trung bình vị thế tại Asks
        public double priceAtMaxBids { get; set; }//giá tại vị thế lớn nhất Bids
        public double priceAtMaxAsks { get; set; }//giá tại vị thế lớn nhất Asks
    }
}
