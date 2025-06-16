using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IConfigNewsRepo : IBaseRepo<ConfigNews>
    {
    }

    public class ConfigNewsRepo : BaseRepo<ConfigNews>, IConfigNewsRepo
    {
        public ConfigNewsRepo()
        {
        }
    }
}
