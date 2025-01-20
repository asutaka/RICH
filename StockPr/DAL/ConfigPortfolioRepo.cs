using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IConfigPortfolioRepo : IBaseRepo<ConfigPortfolio>
    {
    }

    public class ConfigPortfolioRepo : BaseRepo<ConfigPortfolio>, IConfigPortfolioRepo
    {
        public ConfigPortfolioRepo()
        {
        }
    }
}
