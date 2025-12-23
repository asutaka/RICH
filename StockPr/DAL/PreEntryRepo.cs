using MongoDB.Driver;
using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IPreEntryRepo : IBaseRepo<PreEntry>
    {
    }
    public class PreEntryRepo : BaseRepo<PreEntry>, IPreEntryRepo
    {
        public PreEntryRepo(IMongoDatabase database, ILogger<BaseRepo<PreEntry>> logger) : base(database, logger) { }
    }
}
