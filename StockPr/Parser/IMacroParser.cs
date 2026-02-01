using StockPr.Model;
using StockPr.Model.BCPT;

namespace StockPr.Parser
{
    public interface IMacroParser
    {
        List<Pig333_Clean> ParsePigPrice(string json);
        List<TradingEconomics_Data> ParseCommodities(string html, List<string> lCode);
        (string Authorize, string Cookie) ParseMacroMicroAuth(string html, string setCookieHeader);
        MacroMicro_Key ParseMacroMicroData(string json, string key);
        List<Metal_Detail> ParseMetalPhoto(string json);
        List<string> ParseTongCucThongKeLinks(string html, int year);
        string ParseTongCucThongKeExcelLink(string html);
    }
}
