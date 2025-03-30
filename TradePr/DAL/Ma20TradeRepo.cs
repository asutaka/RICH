using TradePr.DAL.Entity;

namespace TradePr.DAL
{
    public interface IMa20TradeRepo : IBaseRepo<Ma20Trade>
    {
    }

    public class Ma20TradeRepo : BaseRepo<Ma20Trade>, IMa20TradeRepo
    {
        public Ma20TradeRepo()
        {
        }
    }
}
