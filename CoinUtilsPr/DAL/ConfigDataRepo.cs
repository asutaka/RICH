using CoinUtilsPr.DAL.Entity;

namespace CoinUtilsPr.DAL
{
    public interface IConfigDataRepo : IBaseRepo<ConfigData>
    {
    }

    public class ConfigDataRepo : BaseRepo<ConfigData>, IConfigDataRepo
    {
        public ConfigDataRepo()
        {
        }
    }
}
