using MongoDB.Bson.Serialization.Attributes;

namespace CoinUtilsPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Trading : BaseDTO
    {
        public string s { get; set; }
        public int d { get; set; }//Date
        public DateTime Date { get; set; }
        public int Side { get; set; }//Buy Or Sell 
        public double Price { get; set; }
        public double Liquid { get; set; }
        public int Status { get; set; }
    }
}
