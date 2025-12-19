using MongoDB.Driver;
using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IConfigDataRepo : IBaseRepo<ConfigData>
    {
    }

    public class ConfigDataRepo : BaseRepo<ConfigData>, IConfigDataRepo
    {
        public ConfigDataRepo(IMongoDatabase database, ILogger<BaseRepo<ConfigData>> logger) : base(database, logger) { }
    }
}
