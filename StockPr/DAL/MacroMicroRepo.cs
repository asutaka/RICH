using MongoDB.Driver;
using StockPr.DAL.Entity;

namespace StockPr.DAL
{
    public interface IMacroMicroRepo : IBaseRepo<MacroMicro>
    {
    }

    public class MacroMicroRepo : BaseRepo<MacroMicro>, IMacroMicroRepo
    {
        public MacroMicroRepo(IMongoDatabase database, ILogger<BaseRepo<MacroMicro>> logger) : base(database, logger) { }
    }
}
