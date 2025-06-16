using CoinUtilsPr.DAL.Entity;

namespace CoinUtilsPr.DAL
{
    public interface IPrepareRepo : IBaseRepo<Prepare>
    {
    }

    public class PrepareRepo : BaseRepo<Prepare>, IPrepareRepo
    {
        public PrepareRepo()
        {
        }
    }
}
