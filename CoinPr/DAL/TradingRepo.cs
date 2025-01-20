using CoinPr.DAL.Entity;

namespace CoinPr.DAL
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
