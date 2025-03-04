using TradePr.DAL.Entity;

namespace TradePr.DAL
{
    public interface ISignalTradeRepo : IBaseRepo<SignalTrade>
    {
    }

    public class SignalTradeRepo : BaseRepo<SignalTrade>, ISignalTradeRepo
    {
        public SignalTradeRepo()
        {
        }
    }
}
