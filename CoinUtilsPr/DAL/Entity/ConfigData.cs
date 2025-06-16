using MongoDB.Bson.Serialization.Attributes;

namespace CoinUtilsPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class ConfigData : BaseDTO
    {
        public int t { get; set; }//time
        public int ex { get; set; }//Exchange
        public int op { get; set; }//Option
        public double value { get; set; }
        public int status { get; set; }
    }
}
