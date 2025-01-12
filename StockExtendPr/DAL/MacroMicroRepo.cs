using StockExtendPr.DAL.Entity;

namespace StockExtendPr.DAL
{
    public interface IMacroMicroRepo : IBaseRepo<MacroMicro>
    {
    }

    public class MacroMicroRepo : BaseRepo<MacroMicro>, IMacroMicroRepo
    {
        public MacroMicroRepo()
        {
        }
    }
}
