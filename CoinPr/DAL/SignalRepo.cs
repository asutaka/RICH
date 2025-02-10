using CoinPr.DAL.Entity;

namespace CoinPr.DAL
{
    public interface ISignalRepo : IBaseRepo<Signal>
    {
    }

    public class SignalRepo : BaseRepo<Signal>, ISignalRepo
    {
        public SignalRepo()
        {
        }
    }
}
