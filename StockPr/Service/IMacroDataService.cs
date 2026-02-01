using StockPr.Model;
using StockPr.Model.BCPT;

namespace StockPr.Service
{
    public interface IMacroDataService
    {
        Task<List<Pig333_Clean>> Pig333_GetPigPrice();
        Task<List<TradingEconomics_Data>> Tradingeconimic_Commodities();
        Task<MacroMicro_Key> MacroMicro_WCI(string key);
        Task<List<Metal_Detail>> Metal_GetYellowPhotpho();
        Task<string> TongCucThongKeGetUrl();
        Task<Stream> TongCucThongKeGetFile(string url);
    }
}
