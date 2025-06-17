namespace StockPr.Model
{
    public class News_KinhTeChungKhoan
    {
        public News_KinhTeChungKhoan_Data data { get; set; }
    }
    public class News_KinhTeChungKhoan_Data
    {
        public List<News_KinhTeChungKhoan_Articles> articles { get; set; }
    }
    public class News_KinhTeChungKhoan_Articles
    {
        public int PublisherId { get; set; }
        public string Title { get; set; }
        public string LinktoMe2 { get; set; }
    }
}
