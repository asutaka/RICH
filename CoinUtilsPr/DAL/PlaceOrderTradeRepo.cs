using CoinUtilsPr.DAL.Entity;

namespace CoinUtilsPr.DAL
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
