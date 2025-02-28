using Skender.Stock.Indicators;
using StockPr.DAL.Entity;
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

    public class Money24h_ForeignAPIResponse
    {
        public Money24h_ForeignAPI_DataResponse data { get; set; }
        public int status { get; set; }
    }

    public class Money24h_ForeignAPI_DataResponse
    {
        public List<Money24h_ForeignAPI_DataDetailResponse> data { get; set; }
        public string from_date { get; set; }
        public string to_date { get; set; }
    }

    public class Money24h_ForeignAPI_DataDetailResponse
    {
        public string symbol { get; set; }
        public decimal sell_qtty { get; set; }
        public decimal sell_val { get; set; }
        public decimal buy_qtty { get; set; }
        public decimal buy_val { get; set; }
        public decimal net_val { get; set; }
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

    public class SSI_DataFinanceResponse
    {
        public IEnumerable<SSI_DataFinanceDetailResponse> data { get; set; }
    }

    public class SSI_DataFinanceDetailResponse
    {
        public decimal eps { get; set; }
    }

    public class SSI_ShareholderResponse
    {
        public IEnumerable<SSI_ShareholderDetailResponse> data { get; set; }
    }

    public class SSI_ShareholderDetailResponse
    {
        public decimal percentage { get; set; }
    }

    public class ReportPTKT
    {
        public string s { get; set; }
        public int rank { get; set; }
        public bool isCrossMa20Up { get; set; }// cắt lên và nến xanh
        public bool isGEMA20 { get; set; }//Nằm trên MA20
        public bool isIchi { get; set; }//giá vượt trên ichi
        public bool isPriceUp { get; set; }//cp tăng giá hay ko
        public OrderBlock ob { get; set; }
    }

    public class TopBotModel
    {
        public DateTime Date { get; set; }
        public bool IsTop { get; set; }
        public bool IsBot { get; set; }
        public decimal Value { get; set; }
        public Quote Item { get; set; }
    }
}
