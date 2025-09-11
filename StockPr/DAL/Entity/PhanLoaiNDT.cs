using MongoDB.Bson.Serialization.Attributes;

namespace StockPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class PhanLoaiNDT : BaseDTO
    {
        public string s { get; set; }
        public List<double> Date { get; set; }
        public List<double> Foreign { get; set; }
        public List<double> TuDoanh { get; set; }
        public List<double> Individual { get; set; }
        public List<double> Group { get; set; }

    }
}
