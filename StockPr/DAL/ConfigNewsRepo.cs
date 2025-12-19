using MongoDB.Driver;
using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IConfigNewsRepo : IBaseRepo<ConfigNews>
    {
    }

    public class ConfigNewsRepo : BaseRepo<ConfigNews>, IConfigNewsRepo
    {
        public ConfigNewsRepo(IMongoDatabase database, ILogger<BaseRepo<ConfigNews>> logger) : base(database, logger) { }
    }
}
