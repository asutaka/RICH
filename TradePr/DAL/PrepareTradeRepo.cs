using TradePr.DAL.Entity;

namespace TradePr.DAL
{
    public interface IPrepareTradeRepo : IBaseRepo<PrepareTrade>
    {
    }

    public class PrepareTradeRepo : BaseRepo<PrepareTrade>, IPrepareTradeRepo
    {
        public PrepareTradeRepo()
        {
        }
    }
}
