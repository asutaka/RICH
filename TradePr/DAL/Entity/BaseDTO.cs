using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace TradePr.DAL.Entity
{
    public abstract class BaseDTO
    {
        private string _id;
        protected string CollectionName { get; set; }

        protected BaseDTO() => _id = UniqueIdentifier.New;

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonPropertyName("objectId")]
        public string ObjectId
        {
            get => _id;
            set => _id = string.IsNullOrEmpty(value) ? UniqueIdentifier.New : value;
        }
    }
    public static class UniqueIdentifier
    {
        public static string New => ObjectId.GenerateNewId().ToString();
    }
}
