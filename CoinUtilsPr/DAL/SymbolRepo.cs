using CoinUtilsPr.DAL.Entity;

namespace CoinUtilsPr.DAL
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
