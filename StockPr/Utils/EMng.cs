using System.ComponentModel.DataAnnotations;

namespace StockPr.Utils
{
    #region Common
    public enum EUserMessageType
    {
        StockPr = 0,
        CoinPr = 1
    }
    public enum EConfigDataType
    {
        TuDoanhHNX = 1,
        TuDoanhUpcom = 2,
        TuDoanhHose = 3,
        GDNN_today = 10,
        GDNN_week = 11,
        GDNN_month = 12,
        ThongKeNhomNganh_today = 20,
        ThongKeNhomNganh_week = 21,
        ThongKeNhomNganh_month = 22,
        ChiBaoKyThuat = 30,
        TongCucThongKeThang = 41,
        TongCucThongKeQuy = 42,
        TongCucHaiQuan_XK = 43,
        TongCucHaiQuan_NK = 44,
        CurrentTime = 50,
        TraceGia = 60,
        CheckVietStockToken = 61,
    }

    public enum EStockType
    {
        [Display(Name = "Bán lẻ")]
        BanLe = 1,
        [Display(Name = "Bất động sản")]
        BDS = 2,
        [Display(Name = "Cảng biển")]
        CangBien = 3,
        [Display(Name = "Cao su")]
        CaoSu = 4,
        [Display(Name = "Chứng khoán")]
        ChungKhoan = 5,
        [Display(Name = "Dầu khí")]
        DauKhi = 6,
        [Display(Name = "Dệt may")]
        DetMay = 7,
        [Display(Name = "Điện gió")]
        DienGio = 8,
        [Display(Name = "Điện khí")]
        DienKhi = 9,
        [Display(Name = "Điện mặt trời")]
        DienMatTroi = 10,
        [Display(Name = "Nhiệt điện")]
        DienThan = 11,
        [Display(Name = "Thủy điện")]
        ThuyDien = 12,
        [Display(Name = "Vay nước ngoài")]
        Forex = 13,
        [Display(Name = "Gỗ")]
        Go = 14,
        [Display(Name = "Hàng không")]
        HangKhong = 15,
        [Display(Name = "Khu công nghiệp")]
        KCN = 16,
        [Display(Name = "Logistic")]
        Logistic = 17,
        [Display(Name = "Ngân hàng")]
        NganHang = 18,
        [Display(Name = "Nhựa")]
        Nhua = 19,
        [Display(Name = "Ô tô dưới 9 chỗ")]
        Oto = 20,
        [Display(Name = "Phân bón")]
        PhanBon = 21,
        [Display(Name = "Than")]
        Than = 22,
        [Display(Name = "Thép")]
        Thep = 23,
        [Display(Name = "Thủy sản")]
        ThuySan = 24,
        [Display(Name = "Ô tô tải")]
        OtoTai = 25,
        [Display(Name = "Xây dựng")]
        XayDung = 26,
        [Display(Name = "Xi măng")]
        XiMang = 27,
        [Display(Name = "Vận tải Biển")]
        VanTaiBien = 28,
        [Display(Name = "Chăn nuôi")]
        ChanNuoi = 29,
        [Display(Name = "Nông nghiệp")]
        NongNghiep = 30,
        [Display(Name = "Hóa chất")]
        HoaChat = 31,
        [Display(Name = "Cà phê")]
        CaPhe = 32,
        [Display(Name = "Gạo")]
        Gao = 33,
        [Display(Name = "Dược phẩm")]
        Duoc = 34,
        [Display(Name = "Dịch vụ y tế")]
        DichVuYTe = 35,
        [Display(Name = "Bảo hiểm")]
        BaoHiem = 36,
        [Display(Name = "Công nghệ thông tin")]
        CNTT = 37,
        [Display(Name = "Đầu tư công")]
        DauTuCong = 38,
        [Display(Name = "Thiết bị điện")]
        ThietBiDien = 39,
        [Display(Name = "Đường")]
        Duong = 40,
        [Display(Name = "Bia")]
        Bia = 41,
        [Display(Name = "Sản phẩm nông nghiệp")]
        SPNongNghiepKhac = 42,
        [Display(Name = "Nước ngọt")]
        NuocNgot = 43,
        [Display(Name = "Sữa")]
        Sua = 44,
        [Display(Name = "Xuất khẩu")]
        XuatKhau = 45,
        [Display(Name = "Năng lượng")]
        NangLuong = 46,
        [Display(Name = "Khác")]
        Khac = 99
    }

    public enum EKeyTongCucThongKe
    {
        None = 0,
        [Display(Name = "Chỉ số giá tiêu dùng(%)")]
        CPI_GiaTieuDung = 1,
        [Display(Name = "Lạm phát(%)")]
        CPI_LamPhat = 4,
        [Display(Name = "Bán lẻ")]
        BanLe = 5,
        [Display(Name = "FDI")]
        FDI = 6,
        [Display(Name = "Đầu tư công")]
        DauTuCong = 7,
        [Display(Name = "IIP - Điện")]
        IIP_Dien = 8,
        [Display(Name = "IIP - Than")]
        IIP_Than = 9,
        [Display(Name = "IIP - Chế biến gỗ")]
        IIP_CheBienGo = 12,
        [Display(Name = "SPCN - Thủy sản")]
        SPCN_ThuySan = 18,
        [Display(Name = "SPCN - Đường")]
        SPCN_Duong = 19,
        [Display(Name = "SPCN - Bia")]
        SPCN_Bia = 20,
        [Display(Name = "SPCN - Ure")]
        SPCN_Ure = 21,
        [Display(Name = "SPCN - NPK")]
        SPCN_NPK = 22,
        [Display(Name = "Hành khách - hàng không")]
        HanhKhach_HangKhong = 26,
        [Display(Name = "Vận tải đường Biển")]
        VanTai_DuongBien = 27,
        [Display(Name = "Vận tải đường Bộ")]
        VanTai_DuongBo = 28,
        [Display(Name = "Vận tải Hàng không")]
        VanTai_HangKhong = 29,
        [Display(Name = "Thuỷ sản")]
        XK_ThuySan = 32,
        [Display(Name = "Cà phê")]
        XK_CaPhe = 33,
        [Display(Name = "Gạo")]
        XK_Gao = 34,
        [Display(Name = "Xi măng")]
        XK_Ximang = 35,
        [Display(Name = "Hóa chất")]
        XK_HoaChat = 36,
        [Display(Name = "SP Chất dẻo")]
        XK_SPChatDeo = 39,
        [Display(Name = "Cao su")]
        XK_CaoSu = 40,
        [Display(Name = "Gỗ")]
        XK_Go = 41,
        [Display(Name = "Dệt may")]
        XK_DetMay = 42,
        [Display(Name = "Sắt thép")]
        XK_SatThep = 43,
        [Display(Name = "Dây điện")]
        XK_DayDien = 45,

        [Display(Name = "Giá VT - Hàng không")]
        QUY_GiaVT_HangKhong = 51,
        [Display(Name = "Giá VT - Bưu chính, chuyển phát")]
        QUY_GiaVT_BuuChinh = 53,
        [Display(Name = "Giá NVL - giá điện")]
        QUY_GiaNVL_Dien = 54,
        [Display(Name = "Giá XK - Thủy Sản")]
        QUY_GiaXK_ThuySan = 55,
        [Display(Name = "Giá XK - Phân bón")]
        QUY_GiaXK_PhanBon = 62,
        [Display(Name = "Giá XK - SP Chất dẻo")]
        QUY_GiaXK_SPChatDeo = 63,
        [Display(Name = "Giá XK - Gỗ")]
        QUY_GiaXK_Go = 64,
        [Display(Name = "Giá XK - Dệt may")]
        QUY_GiaXK_DetMay = 65,
        [Display(Name = "Giá XK - Dây cáp điện")]
        QUY_GiaXK_CapDien = 67,
        [Display(Name = "GDP y tế")]
        QUY_GDP_YTE = 68,
        [Display(Name = "GDP ngân hàng - bảo hiểm")]
        QUY_GDP_NganHangBaoHiem = 69,
    }

    public enum ESource
    {
        DSC = 1,
        VNDirect = 2,
        MigrateAsset = 3,
        Agribank = 4,
        SSI = 5,
        BSC = 6,
        MBS = 7,
        PSI = 8,
        CafeF = 9,
        VCBS = 10,
        FPTS = 11,
        VCI = 12,
        KBS = 13
    }
    #endregion
    #region Response
    #endregion
}
