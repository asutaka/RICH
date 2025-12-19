using MongoDB.Driver;
using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IConfigF319Repo : IBaseRepo<ConfigF319>
    {
    }

    public class ConfigF319Repo : BaseRepo<ConfigF319>, IConfigF319Repo
    {
        public ConfigF319Repo(IMongoDatabase database, ILogger<BaseRepo<ConfigF319>> logger) : base(database, logger) { }
    }
}
