using StockPr.DAL.Entity;
using StockPr.Utils;

namespace StockPr.Model
{
    public class EntryModel
    {
        public EEntry Response { get; set; }
        public QuoteT quote { get; set; }
        public PreEntry pre { get; set; }
        public EPreEntryAction preAction { get; set; }
    }
}
