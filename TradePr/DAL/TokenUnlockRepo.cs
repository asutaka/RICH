using TradePr.DAL.Entity;

namespace TradePr.DAL
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
