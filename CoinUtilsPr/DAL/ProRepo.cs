using CoinUtilsPr.DAL.Entity;

namespace CoinUtilsPr.DAL
{
    public interface IProRepo : IBaseRepo<Pro>
    {
    }

    public class ProRepo : BaseRepo<Pro>, IProRepo
    {
        public ProRepo()
        {
        }
    }
}
