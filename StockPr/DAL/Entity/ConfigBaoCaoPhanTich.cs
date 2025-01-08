using MongoDB.Bson.Serialization.Attributes;

namespace StockPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class ConfigBaoCaoPhanTich : BaseDTO
    {
        public int d { get; set; }
        public int ty { get; set; }
        public string key { get; set; }//id hoặc dấu hiệu nhận biết cuả bài post
    }
}
