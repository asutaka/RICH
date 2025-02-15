using MongoDB.Bson.Serialization.Attributes;

namespace TestPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class TokenUnlock : BaseDTO
    {
        public string s { get; set; }
        public int time { get; set; }
        public int status { get; set; }//0: insert, 1: đã thực hiện
    }
}
