using CoinPr.DAL.Entity;

namespace CoinPr.DAL
{
    public interface IUserMessageRepo : IBaseRepo<UserMessage>
    {
    }
    public class UserMessageRepo : BaseRepo<UserMessage>, IUserMessageRepo
    {
        public UserMessageRepo() { }
    }
}
