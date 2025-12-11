using CoinUtilsPr.DAL.Entity;
namespace CoinUtilsPr.DAL
{
    public interface IDepthRepo : IBaseRepo<Depth>
    {
    }

    public class DepthRepo : BaseRepo<Depth>, IDepthRepo
    {
        public DepthRepo()
        {
        }
    }
}
