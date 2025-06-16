using MongoDB.Bson.Serialization.Attributes;

namespace StockPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class F319Account : BaseDTO
    {
        public string name { get; set; }
        public string url { get; set; }
        public string des { get; set; }
        public int rank { get; set; }//Thứ tự
        public int status { get; set; }//0, -1
    }
}
