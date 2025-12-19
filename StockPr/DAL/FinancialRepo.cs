using MongoDB.Driver;
using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IFinancialRepo : IBaseRepo<Financial>
    {
    }

    public class FinancialRepo : BaseRepo<Financial>, IFinancialRepo
    {
        public FinancialRepo(IMongoDatabase database, ILogger<BaseRepo<Financial>> logger) : base(database, logger) { }
    }
}
