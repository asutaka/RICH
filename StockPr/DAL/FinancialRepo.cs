using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IFinancialRepo : IBaseRepo<Financial>
    {
    }

    public class FinancialRepo : BaseRepo<Financial>, IFinancialRepo
    {
        public FinancialRepo()
        {
        }
    }
}
