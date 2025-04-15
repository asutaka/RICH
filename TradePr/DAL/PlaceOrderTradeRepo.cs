using TradePr.DAL.Entity;

namespace TradePr.DAL
{
    public interface IPlaceOrderTradeRepo : IBaseRepo<PlaceOrderTrade>
    {
    }

    public class PlaceOrderTradeRepo : BaseRepo<PlaceOrderTrade>, IPlaceOrderTradeRepo
    {
        public PlaceOrderTradeRepo()
        {
        }
    }
}
