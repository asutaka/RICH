using CoinPr.DAL.Entity;

namespace CoinPr.DAL
{
    public interface ITokenUnlockRepo : IBaseRepo<TokenUnlock>
    {
    }

    public class TokenUnlockRepo : BaseRepo<TokenUnlock>, ITokenUnlockRepo
    {
        public TokenUnlockRepo()
        {
        }
    }
}
