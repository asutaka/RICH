using TradePr.DAL.Entity;

namespace TradePr.DAL
{
    public interface ISymbolConfigRepo : IBaseRepo<SymbolConfig>
    {
    }

    public class SymbolConfigRepo : BaseRepo<SymbolConfig>, ISymbolConfigRepo
    {
        public SymbolConfigRepo()
        {
        }
    }
}
