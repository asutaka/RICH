using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IPhanLoaiNDTRepo : IBaseRepo<PhanLoaiNDT>
    {
    }

    public class PhanLoaiNDTRepo : BaseRepo<PhanLoaiNDT>, IPhanLoaiNDTRepo
    {
        public PhanLoaiNDTRepo()
        {
        }
    }
}
