using MongoDB.Bson.Serialization.Attributes;

namespace TradePr.DAL.Entity
{

    [BsonIgnoreExtraElements]
    public class ErrorPartner : BaseDTO
    {
        public string s { get; set; }
        public int time { get; set; }
        public string mes { get; set; }
        public string des { get; set; }
        public int ty { get; set; }//loại hình báo lỗi
        public int action { get; set; }//phương thức báo lỗi
    }
}
