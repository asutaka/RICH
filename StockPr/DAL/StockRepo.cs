using MongoDB.Driver;
using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IStockRepo : IBaseRepo<Stock>
    {
    }

    public class StockRepo : BaseRepo<Stock>, IStockRepo
    {
        public StockRepo(IMongoDatabase database, ILogger<BaseRepo<Stock>> logger) 
            : base(database, logger)
        {
        }
    }
}
