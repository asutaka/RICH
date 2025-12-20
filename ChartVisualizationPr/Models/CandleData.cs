namespace ChartVisualizationPr.Models
{
    public class CandleData
    {
        public long Time { get; set; }  // Unix timestamp in seconds
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
    }
}
