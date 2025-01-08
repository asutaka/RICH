namespace StockPr.DAL.Entity
{
    public class BaseFinancialDTO : BaseDTO
    {
        public int d { get; set; }//Năm - Quý
        public string s { get; set; }//Mã CK
        public int pl { get; set; }//Ngày public BCTC
        public int t { get; set; }
        public double rv { get; set; }//Doanh thu
        public double pf { get; set; }//Lợi nhuận
        public double pfg { get; set; }//Lợi nhuận Gộp 
        public double pfn { get; set; }//Lợi nhuận Ròng
    }
}
