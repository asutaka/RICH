namespace ChartVisualizationPr.Models
{
    public class IndicatorData
    {
        public long Time { get; set; }
        public decimal? Ma20 { get; set; }
        public decimal? UpperBand { get; set; }
        public decimal? LowerBand { get; set; }
        public decimal? Rsi { get; set; }
        public decimal? RsiMa9 { get; set; }
        public decimal? RsiWma45 { get; set; }
        public decimal Volume { get; set; }
    }
}
