using TradePr.DAL.Entity;

namespace TradePr.DAL
{
    public interface IActionTradeRepo : IBaseRepo<ActionTrade>
    {
    }

    public class ActionTradeRepo : BaseRepo<ActionTrade>, IActionTradeRepo
    {
        public ActionTradeRepo()
        {
        }
    }
}
