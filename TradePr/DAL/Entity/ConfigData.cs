using MongoDB.Bson.Serialization.Attributes;

namespace TradePr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class ConfigData : BaseDTO
    {
        public int t { get; set; }//time
        public int ex { get; set; }//Exchange
        public int op { get; set; }//Option
        public int status { get; set; }
    }
}
