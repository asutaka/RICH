using Skender.Stock.Indicators;

namespace TestPr.Model
{
    public class QuoteEx : Quote
    {
        public int TotalRateMa20_UP { get; set; } //0,1
        public int TotalRate1_3_UP { get; set; }//0,1
        public int TotalRate1_3_DOWN { get; set; }
    }
}
