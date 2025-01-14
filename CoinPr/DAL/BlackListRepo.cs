using CoinPr.DAL.Entity;

namespace CoinPr.DAL
{
    public interface IBlackListRepo : IBaseRepo<BlackList>
    {
    }

    public class BlackListRepo : BaseRepo<BlackList>, IBlackListRepo
    {
        public BlackListRepo()
        {
        }
    }
}
