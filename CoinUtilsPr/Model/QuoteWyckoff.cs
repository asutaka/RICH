using Skender.Stock.Indicators;

namespace CoinUtilsPr.Model
{
    public class QuoteWyckoff : Quote
    {
        public decimal Ma20 { get; set; }
        public decimal Ma20Vol { get; set; }
        public decimal PrevVol { get; set; }
        public decimal Rsi { get; set; }
    }
}
