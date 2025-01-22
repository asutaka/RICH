using TestPr.DAL.Entity;

namespace TestPr.DAL
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
