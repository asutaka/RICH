using Skender.Stock.Indicators;

namespace CoinUtilsPr.Model
{
    public class ProModel 
    {
        public Quote entity { get; set; }
        public decimal sl { get; set; }
        public string mes { get; set; }
        public decimal ratio { get; set; }//Phần trăm vào lệnh
    }
}
