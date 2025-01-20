using MongoDB.Bson.Serialization.Attributes;

namespace StockPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class ConfigPortfolio : BaseDTO
    {
        public int ty { get; set; }
        public string key { get; set; }//id hoặc dấu hiệu nhận biết cuả bài post
    }
}
