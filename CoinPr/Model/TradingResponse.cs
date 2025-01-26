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
        //For Test
        public decimal AvgPrice { get; set; }
        public decimal PriceAtLiquid { get; set; }
        public decimal CurrentPrice { get; set; }
        public int Mode { get; set; }
        public decimal Rsi_5 { get; set; }//5p
        public decimal Top_1 { get; set; }
        public decimal Top_2 { get; set; }
        public decimal Bot_1 { get; set; }
        public decimal Bot_2 { get; set; }
        public decimal TP_2 { get; set; }
        public decimal TP_3 { get; set; }
        public decimal SL_2 { get; set; }
    }
}
