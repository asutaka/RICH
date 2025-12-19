using MongoDB.Driver;
using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface ISymbolRepo : IBaseRepo<Symbol>
    {
    }

    public class SymbolRepo : BaseRepo<Symbol>, ISymbolRepo
    {
        public SymbolRepo(IMongoDatabase database, ILogger<BaseRepo<Symbol>> logger) : base(database, logger) { }
    }
}
