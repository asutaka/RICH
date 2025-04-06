using TestPr.DAL;
using TestPr.DAL.Entity;

namespace TestPr.DAL
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
