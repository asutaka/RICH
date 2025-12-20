namespace ChartVisualizationPr.Models
{
    public class MarkerData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Symbol { get; set; } = string.Empty;
        public long Time { get; set; }  // Unix timestamp in seconds
        public string Position { get; set; } = "aboveBar";  // "aboveBar" or "belowBar"
        public string Color { get; set; } = "#2196F3";
        public string Shape { get; set; } = "circle";  // "circle", "square", "arrowUp", "arrowDown"
        public string Text { get; set; } = string.Empty;
    }
}
