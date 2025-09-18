using CoinUtilsPr.DAL.Entity;

namespace CoinUtilsPr.DAL
{
    public interface IEntrySOSRepo : IBaseRepo<EntrySOS>
    {
    }

    public class EntrySOSRepo : BaseRepo<EntrySOS>, IEntrySOSRepo
    {
        public EntrySOSRepo()
        {
        }
    }
}
