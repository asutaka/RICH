using TradePr.DAL.Entity;

namespace TradePr.DAL
{

    public interface IThreeSignalTradeRepo : IBaseRepo<ThreeSignalTrade>
    {
    }

    public class ThreeSignalTradeRepo : BaseRepo<ThreeSignalTrade>, IThreeSignalTradeRepo
    {
        public ThreeSignalTradeRepo()
        {
        }
    }
}
