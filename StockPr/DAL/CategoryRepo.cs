using MongoDB.Driver;
using StockPr.DAL.Entity;
namespace StockPr.DAL
{
    public interface ICategoryRepo : IBaseRepo<Category>
    {
    }

    public class CategoryRepo : BaseRepo<Category>, ICategoryRepo
    {
        public CategoryRepo(IMongoDatabase database, ILogger<BaseRepo<Category>> logger) : base(database, logger) { }
    }
}
