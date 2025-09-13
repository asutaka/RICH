using Skender.Stock.Indicators;

namespace CoinUtilsPr.DAL.Entity
{
    public class QuoteEx : Quote
    {
        public decimal Rate_TP { get; set; }
        public double? MA20Vol { get; set; }
        public double? MA20 { get; set; }
    }
}
