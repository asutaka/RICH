namespace StockPr.Model.BCPT
{
    public class F319Raw
    {
        public string TemplateHtml { get; set; }
    }

    public class F319Model
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Content { get; set; }
        public int TimePost { get; set; }
        
        public string Message { get; set; }//Last
    }
}
