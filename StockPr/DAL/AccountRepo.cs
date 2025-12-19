using MongoDB.Driver;
using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IAccountRepo : IBaseRepo<Account>
    {
    }
    public class AccountRepo : BaseRepo<Account>, IAccountRepo
    {
        public AccountRepo(IMongoDatabase database, ILogger<BaseRepo<Account>> logger) : base(database, logger) { }
    }
}
