using MongoDB.Driver;
using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IUserMessageRepo : IBaseRepo<UserMessage>
    {
    }
    public class UserMessageRepo : BaseRepo<UserMessage>, IUserMessageRepo
    {
        public UserMessageRepo(IMongoDatabase database, ILogger<BaseRepo<UserMessage>> logger) : base(database, logger) { }
    }
}
