using Newtonsoft.Json;

namespace StockPr.Model.BCPT
{
    public class TradingEconomics_Data
    {
        public string Code { get; set; }
        public decimal Price { get; set; }
        public decimal Weekly { get; set; }
        public decimal Monthly { get; set; }
        public decimal YTD { get; set; }
        public decimal YoY { get; set; }
    }

    public class MacroMicro_Main
    {
        public MacroMicro_Data data { get; set; }
    }

    public class MacroMicro_Data
    {
        [JsonProperty("c:44756")]
        public MacroMicro_Key key { get; set; }
        [JsonProperty("c:946")]
        public MacroMicro_Key key2 { get; set; }
    }

    public class MacroMicro_Key
    {
        public List<List<List<string>>> series { get; set; }
    }

    public class MacroMicro_CleanData
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
    }

    public class Metal_Main
    {
        public Metal_Data data { get; set; }
    }

    public class Metal_Data
    {
        public List<Metal_Detail> priceListList { get; set; }
    }

    public class Metal_Detail
    {
        public Metal_Price metalsPrice { get; set; }
    }

    public class Metal_Price
    {
        public decimal average { get; set; }
        public string renewDate { get; set; }
        public DateTime Date { get; set; }
    }

    public class Pig333_Main
    {
        public List<string> resultat { get; set; }
    }

    public class Pig333_Clean
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
    }

    public class TraceGiaModel
    {
        public string content { get; set; }
        public decimal weekly { get; set; }
        public decimal monthly { get; set; }
        public decimal yearly { get; set; }
        public decimal YTD { get; set; }
        public decimal price { get; set; }
        public string description { get; set; }
        public string link { get; set; }
    }
}
