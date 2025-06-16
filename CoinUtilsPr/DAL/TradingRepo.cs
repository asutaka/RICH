using CoinUtilsPr.DAL.Entity;

namespace CoinUtilsPr.DAL
{
    public interface ITradingRepo : IBaseRepo<Trading>
    {
    }

    public class TradingRepo : BaseRepo<Trading>, ITradingRepo
    {
        public TradingRepo()
        {
        }
    }
}
