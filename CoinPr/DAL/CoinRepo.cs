using CoinPr.DAL.Entity;

namespace CoinPr.DAL
{
    public interface ICoinRepo : IBaseRepo<Coin>
    {
    }

    public class CoinRepo : BaseRepo<Coin>, ICoinRepo
    {
        public CoinRepo()
        {
        }
    }
}
