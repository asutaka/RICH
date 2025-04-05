using TradePr.DAL.Entity;

namespace TradePr.DAL
{
    public interface ISymbolRepo : IBaseRepo<Symbol>
    {
    }

    public class SymbolRepo : BaseRepo<Symbol>, ISymbolRepo
    {
        public SymbolRepo()
        {
        }
    }
}
