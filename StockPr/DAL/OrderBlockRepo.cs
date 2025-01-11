using StockPr.DAL.Entity;
namespace StockPr.DAL
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
