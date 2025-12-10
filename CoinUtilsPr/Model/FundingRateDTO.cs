namespace CoinUtilsPr.Model
{
    public class FundingRateDTO
    {
        public decimal fundingRate { get; set; }
        public decimal fundingTime { get; set; }
        public decimal markPrice { get; set; }
    }

    public class  TakerVolumneBuySellDTO
    {
        public decimal buySellRatio { get; set; }
        public decimal timestamp { get; set; }
    }

    public class LongShortRatioDTO
    {
        public decimal longShortRatio { get; set; }
        public decimal timestamp { get; set; }
    }
}
