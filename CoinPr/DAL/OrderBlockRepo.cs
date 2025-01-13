using CoinPr.DAL.Entity;

namespace CoinPr.DAL
{
    public interface IOrderBlockRepo : IBaseRepo<OrderBlock>
    {
    }

    public class OrderBlockRepo : BaseRepo<OrderBlock>, IOrderBlockRepo
    {
        public OrderBlockRepo()
        {
        }
    }
}
