using MongoDB.Driver;
using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IConfigPortfolioRepo : IBaseRepo<ConfigPortfolio>
    {
    }

    public class ConfigPortfolioRepo : BaseRepo<ConfigPortfolio>, IConfigPortfolioRepo
    {
        public ConfigPortfolioRepo(IMongoDatabase database, ILogger<BaseRepo<ConfigPortfolio>> logger) : base(database, logger) { }
    }
}
