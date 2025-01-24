using MongoDB.Bson.Serialization.Attributes;

namespace TradePr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class ActionTrade : BaseDTO
    {
        public string key { get; set; }
        public string s { get; set; }
        public int Mode { get; set; }
        public int dbuy { get; set; }//Date
        public double Entry { get; set; }
        public double TP { get; set; }
        public double SL { get; set; }
        public bool IsFinish { get; set; }
        public int IsWin { get; set; }
        public int dsell { get; set; }
    }
}
