using Skender.Stock.Indicators;

namespace CoinUtilsPr.Model
{
    public class ProModel 
    {
        public Quote entity { get; set; }
        public int Strength { get; set; }
        public decimal riskPercent { get; set; }
        public decimal risk { get; set; }
        public decimal sl { get; set; }
        public string mes { get; set; }
    }
}
