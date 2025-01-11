using StockPr.DAL.Entity;

namespace StockPr.Model
{
    public class HSXTudoanhModel
    {
        public List<HSXRowModel> rows { get; set; }
    }

    public class HSXRowModel
    {
        public List<string> cell { get; set; }
    }

    public class TudoanhPDF
    {
        public int no { get; set; }
        public long d { get; set; }
        public string s { get; set; }
        public int bvo { get; set; }//kl mua
        public int svo { get; set; }//kl ban
        public decimal bva { get; set; }//gia tri mua
        public decimal sva { get; set; }//gia tri ban
        public int bvo_pt { get; set; }//kl mua thỏa thuận
        public int svo_pt { get; set; }//kl ban thỏa thuận
        public decimal bva_pt { get; set; }//gia tri mua thỏa thuận
        public decimal sva_pt { get; set; }//gia tri ban thỏa thuận
        public long t { get; set; }
    }

    public class TudoanhAPI
    {
        public TuDoanhAPI_Data data { get; set; }
        public int status { get; set; }
    }

    public class TuDoanhAPI_Data
    {
        public List<TuDoanhAPI_DataDetail> data { get; set; }
        public string from_date { get; set; }
        public string to_date { get; set; }
    }

    public class TuDoanhAPI_DataDetail
    {
        public string symbol { get; set; }
        public decimal prop_net_deal { get; set; }
        public decimal prop_net_pt { get; set; }
        public decimal prop_net { get; set; }
    }

    public class TuDoanh
    {
        public int no { get; set; }
        public long d { get; set; }
        public string s { get; set; }
        public decimal net_deal { get; set; }//gia tri mua bán khớp lệnh
        public decimal net_pt { get; set; }//gia tri mua bán thỏa thuận
        public decimal net { get; set; }//gia tri mua bán
        public long t { get; set; }
    }
}
