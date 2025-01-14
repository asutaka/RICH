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
}
