using MongoDB.Bson.Serialization.Attributes;

namespace TestPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Signal : BaseDTO
    {
        public DateTime Date { get; set; }
        public double Channel { get; set; }
        public string Content { get; set; }
    }
}
