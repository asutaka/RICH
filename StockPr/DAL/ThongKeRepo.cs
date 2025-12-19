using MongoDB.Driver;
using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IThongKeRepo : IBaseRepo<ThongKe>
    {
    }

    public class ThongKeRepo : BaseRepo<ThongKe>, IThongKeRepo
    {
        public ThongKeRepo(IMongoDatabase database, ILogger<BaseRepo<ThongKe>> logger) : base(database, logger) { }
    }
}
