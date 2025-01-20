using Skender.Stock.Indicators;

namespace CoinPr.Model
{
    public class TopBotModel
    {
        public DateTime Date { get; set; }
        public bool IsTop { get; set; }
        public bool IsBot { get; set; }
        public decimal Value { get; set; }
        public Quote Item { get; set; }
    }

    public class TradingResponse
    {
        public string s { get; set; }
        public DateTime Date { get; set; }
        public int Type { get; set; }//OrderBlock or Liquid
        public Binance.Net.Enums.OrderSide Side { get; set; }//Buy Or Sell
        public decimal Entry { get; set; }//Only OrderBlock
        public decimal SL { get; set; }//Only OrderBlock
        public decimal TP { get; set; }//Only OrderBlock
        public decimal Focus { get; set; }
        public int Status { get; set; }//Only Liquid
    }
}
