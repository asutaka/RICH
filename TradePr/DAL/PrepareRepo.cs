using TradePr.DAL.Entity;

namespace TradePr.DAL
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
