using TradePr.DAL.Entity;

namespace TradePr.DAL
{
    public interface ITokenUnlockTradeRepo : IBaseRepo<TokenUnlockTrade>
    {
    }

    public class TokenUnlockTradeRepo : BaseRepo<TokenUnlockTrade>, ITokenUnlockTradeRepo
    {
        public TokenUnlockTradeRepo()
        {
        }
    }
}
