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
        public Binance.Net.Enums.OrderSide Side { get; set; }//Buy Or Sell
        public decimal Entry { get; set; }
        public decimal SL { get; set; }
        public decimal TP { get; set; }
        public decimal Focus { get; set; }
        //For Test
        public decimal CurrentPrice { get; set; }
        public decimal AvgPrice { get; set; }
        public decimal Liquid { get; set; }
        public decimal Rsi { get; set; }//5p
        public decimal TopFirst { get; set; }
        public decimal TopNext { get; set; }
        public decimal BotFirst { get; set; }
        public decimal BotNext { get; set; }
        public decimal RateVol { get; set; }
        public decimal TP_2 { get; set; }
        public decimal TP_3 { get; set; }
        public decimal TP25 { get; set; }
        public decimal SL_2 { get; set; }
        public decimal SL25 { get; set; }
    }
}
