using CoinUtilsPr.DAL.Entity;

namespace CoinUtilsPr.DAL
{
    public interface ILast_SOSRepo : IBaseRepo<SOSDTO>
    {
    }

    public class Last_SOSRepo : BaseRepo<SOSDTO>, ILast_SOSRepo
    {
        public Last_SOSRepo()
        {
        }
    }
}
