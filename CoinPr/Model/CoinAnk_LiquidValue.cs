namespace CoinPr.Model
{
    public class CoinAnk_LiquidValue
    {
        public CoinAnk_LiquidValueDetail data { get; set; }
    }

    public class CoinAnk_LiquidValueDetail
    {
        public CoinAnk_LiquidHeatmap liqHeatMap { get; set; }
    }

    public class CoinAnk_LiquidHeatmap
    {
        public List<List<decimal>> data { get; set; }
        public List<decimal> priceArray { get; set; }
        public decimal maxLiqValue { get; set; }
    }

    public class LiquidResponse
    {
        public List<LiquidDetailResponse> data { get; set; }
    }
    public class LiquidDetailResponse
    {
        public string exchangeName { get; set; }
        public string baseCoin { get; set; }
        public string side { get; set; }
        public string contractType { get; set; }
        public string contractCode { get; set; }
        public string posSide { get; set; }
        public decimal amount { get; set; }
        public decimal price { get; set; }
        public decimal tradeTurnover { get; set; }
        public long ts { get; set; }
    }
}
