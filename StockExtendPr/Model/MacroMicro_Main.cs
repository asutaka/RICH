using Newtonsoft.Json;

namespace StockExtendPr.Model
{
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
}
