using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPr.Model
{
    public class Money24h_PTKT_LV1Response
    {
        public int status { get; set; }
        public Money24h_PTKT_LV2Response data { get; set; }
    }

    public class Money24h_PTKT_LV2Response
    {
        public List<Money24h_PTKTResponse> data { get; set; }
        public int total_symbol { get; set; }
    }
    public class Money24h_PTKTResponse
    {
        public string symbol { get; set; }
        public decimal match_price { get; set; }//Giá hiện tại
        public decimal basic_price { get; set; }//Giá tham chiếu
        public decimal accumylated_vol { get; set; }//Vol
        public decimal change_vol_percent_5 { get; set; }//Thay đổi so với 5 phiên trước
    }

    public class Money24h_NhomNganhResponse
    {
        public Money24h_NhomNganh_DataResponse data { get; set; }
        public int status { get; set; }
    }

    public class Money24h_NhomNganh_DataResponse
    {
        public List<Money24h_NhomNganh_GroupResponse> groups { get; set; }
        public long last_update { get; set; }
    }

    public class Money24h_NhomNganh_GroupResponse
    {
        public string icb_code { get; set; }
        public string icb_name { get; set; }
        public int icb_level { get; set; }
        public List<Money24h_NhomNganh_GroupResponse> child { get; set; }
        public int total_stock { get; set; }
        public int total_stock_increase { get; set; }
        public int total_stock_nochange { get; set; }
        public int toal_stock_decrease { get; set; }
        public decimal avg_change_percent { get; set; }
        public decimal total_val { get; set; }
        public decimal toal_val_increase { get; set; }
        public decimal total_val_nochange { get; set; }
        public decimal total_val_decrease { get; set; }
    }

    public class Money24h_ForeignResponse
    {
        public int no { get; set; }
        public long d { get; set; }
        public string s { get; set; }
        public decimal sell_qtty { get; set; }
        public decimal sell_val { get; set; }
        public decimal buy_qtty { get; set; }
        public decimal buy_val { get; set; }
        public decimal net_val { get; set; }
        public long t { get; set; }
    }

    public class MaTheoPTKT24H_MA20
    {
        public string s { get; set; }
        public int rank { get; set; }
        public bool isIchi { get; set; }
        public bool isVol { get; set; }
    }

    public class SSI_DataTradingResponse
    {
        public SSI_DataTradingDetailResponse data { get; set; }
    }

    public class SSI_DataTradingDetailResponse
    {
        public IEnumerable<decimal> t { get; set; }
        public IEnumerable<decimal> c { get; set; }
        public IEnumerable<decimal> o { get; set; }
        public IEnumerable<decimal> h { get; set; }
        public IEnumerable<decimal> l { get; set; }
        public IEnumerable<decimal> v { get; set; }
    }
}
