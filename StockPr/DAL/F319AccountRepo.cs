using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IF319AccountRepo : IBaseRepo<F319Account>
    {
    }

    public class F319AccountRepo : BaseRepo<F319Account>, IF319AccountRepo
    {
        public F319AccountRepo()
        {
        }
    }
}
