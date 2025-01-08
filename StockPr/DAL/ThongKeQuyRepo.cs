using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IThongKeQuyRepo : IBaseRepo<ThongKe>
    {
    }

    public class ThongKeQuyRepo : BaseRepo<ThongKe>, IThongKeQuyRepo
    {
        public ThongKeQuyRepo()
        {
        }
    }
}
