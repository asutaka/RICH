using TradePr.DAL.Entity;

namespace TradePr.DAL
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
