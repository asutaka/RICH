using StockPr.DAL.Entity;
namespace StockPr.DAL
{
    public interface ICategoryRepo : IBaseRepo<Category>
    {
    }

    public class CategoryRepo : BaseRepo<Category>, ICategoryRepo
    {
        public CategoryRepo()
        {
        }
    }
}
