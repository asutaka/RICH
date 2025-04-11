using Skender.Stock.Indicators;

namespace CoinPr.Model
{
    public class TradingResponse
    {
        public string s { get; set; }
        public DateTime Date { get; set; }
        public Binance.Net.Enums.OrderSide Side { get; set; }//Buy Or Sell
        public double Price { get; set; }
        public double Liquid { get; set; }
    }
}
