using MongoDB.Bson.Serialization.Attributes;

namespace StockPr.DAL.Entity
{
    [BsonIgnoreExtraElements]
    public class Financial : BaseDTO
    {
        public string s { get; set; }//Mã CK
        public int d { get; set; }//Năm - Quý
        public int t { get; set; }//Thời gian cập nhật
        public int pl { get; set; }//Ngày public BCTC
        public double rv { get; set; }//Doanh thu
        public double pf { get; set; }//Lợi nhuận
        public double pfg { get; set; }//Lợi nhuận Gộp 
        public double pfn { get; set; }//Lợi nhuận Ròng
        public double inv { get; set; }//Tồn Kho
        public double debt { get; set; }//Nợ tài chính - Đối với mã chứng khoán: Các khoản cho vay(phần rất lớn là tăng trưởng margin)(CDKT) - Đối với ngành ngân hàng: Tổng nợ
        public double eq { get; set; }//Vốn chủ sở hữu
        public double bp { get; set; }//Người mua trả tiền trước
        //Các property cho chứng khoán
        public double broker { get; set; } //Doanh thu môi giới
        public double bcost { get; set; } //Chi phí môi giới
        public double idebt { get; set; } //Lãi từ các khoản cho vay
        public double trade { get; set; } //Doanh thu tự doanh
        public double itrade { get; set; } //Tài sản tự doanh
        //Các property cho ngành ngân hàng
        public double? risk { get; set; }//Trích lập dự phòng
        public double? nim_r { get; set; }//Lãi thuần / Tổng tài sản sinh lãi
        public double? casa_r { get; set; }//Tiền gửi KH không kỳ hạn/ Tổng tiền gửi KH
        public double? cir_r { get; set; }//Tổng chi phí hoạt động/ Tổng thu nhập hoạt động 
        public double? credit_r { get; set; }//Tăng trưởng tín dụng
        public double? cost_r { get; set; }//Giảm chi phí vốn(chi phí lãi và các chi phí tương tự)
        public double debt1 { get; set; }//Nợ mức 1
        public double debt2 { get; set; }//Nợ mức 2
        public double debt3 { get; set; }//Nợ mức 3
        public double debt4 { get; set; }//Nợ mức 4
        public double debt5 { get; set; }//Nợ mức 5
        public double? cover_r { get; set; }//Tỉ lệ bao phủ nợ xấu
        public double? room { get; set; }//Room tín dụng
    }
}
