using System;

namespace StockPr.Model
{
    public class Vietstock_GICSProportionResponse
    {
        public int IndustryCode { get; set; }
        public string IndustryName { get; set; }
        public DateTime? TradingDate { get; set; }
        public decimal Change { get; set; }
        public int Type { get; set; }
    }
}
