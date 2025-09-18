using CoinUtilsPr.DAL.Entity;

namespace CoinUtilsPr.DAL
{
    public interface ISignalSOSRepo : IBaseRepo<SignalSOS>
    {
    }

    public class SignalSOSRepo : BaseRepo<SignalSOS>, ISignalSOSRepo
    {
        public SignalSOSRepo()
        {
        }
    }
}
