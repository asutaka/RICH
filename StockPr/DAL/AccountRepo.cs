using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IAccountRepo : IBaseRepo<Account>
    {
    }
    public class AccountRepo : BaseRepo<Account>, IAccountRepo
    {
        public AccountRepo() { }
    }
}
