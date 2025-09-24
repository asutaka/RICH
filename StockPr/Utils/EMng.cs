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
        TuDoanh_today = 13,
        TuDoanh_week = 14,
        ThongKeNhomNganh_today = 20,
        ThongKeNhomNganh_week = 21,
        ThongKeNhomNganh_month = 22,
        ChiBaoKyThuat = 30,
        TongCucThongKeThang = 41,
        CurrentTime = 50,
        TraceGia = 60,
        CheckVietStockToken = 61,
        MacroMicro = 62,
        EPS = 63,
        ThongKeKhopLenh = 64,
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

    public enum EOrderBlockMode
    {
        TopPinbar = 1,
        TopInsideBar = 2,
        BotPinbar = 3,
        BotInsideBar = 4
    }

    public enum EKeyTongCucThongKe
    {
        None = 0,
        [Display(Name = "Chỉ số giá tiêu dùng(%)")]
        CPI_GiaTieuDung = 1,
        [Display(Name = "Giá Vàng(%)")]
        CPI_GiaVang = 2,
        [Display(Name = "USD(%)")]
        CPI_DoLa = 3,
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
        KBS = 13,
        VinaCapital = 14,

        VCBF = 20,
        DC = 21,
        PynElite = 22,
        [Display(Name = "veof")]
        VinaCapital_VEOF = 23,
        [Display(Name = "vesaf")]
        VinaCapital_VESAF = 24,
        [Display(Name = "vmeef")]
        VinaCapital_VMEEF = 25,
        SGI = 26,

    }

    public enum EMoney24hTimeType
    {
        [Display(Name = "today")]
        today = 1,
        [Display(Name = "week")]
        week = 2,
        [Display(Name = "month")]
        month = 3
    }

    public enum EExchange
    {
        [Display(Name = "10")]
        HSX = 1,
        [Display(Name = "02")]
        HNX = 2,
        [Display(Name = "03")]
        UPCOM = 3
    }

    public enum EPrice
    {
        [Display(Name = "Crude Oil")]
        Crude_Oil = 1,//Dầu thô
        [Display(Name = "Natural gas")]
        Natural_gas = 2,//Khí thiên nhiên
        [Display(Name = "Coal")]
        Coal = 3,//Than
        [Display(Name = "Rubber")]
        Rubber = 4,//Cao su
        [Display(Name = "Steel")]
        Steel = 5,//Thép
        [Display(Name = "HRC Steel")]
        HRC_Steel = 6,//Thép HRC
        [Display(Name = "Gold")]
        Gold = 99,//Vàng
        [Display(Name = "Coffee")]
        Coffee = 32,//Cà phê
        [Display(Name = "Rice")]
        Rice = 33,//Gạo
        [Display(Name = "Sugar")]
        Sugar = 40,//Đường
        [Display(Name = "Urea")]
        Urea = 11,//U rê
        [Display(Name = "polyvinyl")]
        polyvinyl = 19,//Ống nhựa PVC
        [Display(Name = "Nickel")]
        Nickel = 13,//Niken
        [Display(Name = "WCI")]
        WCI = 14,//World Container Index: chỉ số vận tải container
        [Display(Name = "YellowPhotpho")]
        YellowPhotpho = 15,//Phốt pho vàng
        [Display(Name = "BDTI")]
        BDTI = 16,//Cước vận tải dầu
        [Display(Name = "milk")]
        milk = 17,//Giá sữa
        [Display(Name = "DXY")]
        DXY = 18,//Chỉ số đô la
        [Display(Name = "kraft-pulp")]
        kraftpulp = 98,
        [Display(Name = "Cotton")]
        Cotton = 97,//Giá bông
        [Display(Name = "di-ammonium")]
        DAP = 96
    }

    public enum EReportNormId
    {
        [Display(Name = "Doanh thu")]
        DoanhThu = 2206,
        [Display(Name = "Giá vốn")]
        GiaVon = 2207,
        [Display(Name = "Lợi nhuận sau thuế")]
        LNST = 2212,
        [Display(Name = "Lợi nhuận gộp")]
        LNGop = 2217,
        [Display(Name = "Lợi nhuận ròng")]
        LNRong = 2208,

        [Display(Name = "Vay và nợ thuê tài chính ngắn hạn")]
        VayNganHan = 3077,
        [Display(Name = "Vay và nợ thuê tài chính dài hạn")]
        VayDaiHan = 3078,

        [Display(Name = "Vốn chủ sở hữu")]
        VonChuSoHuu = 2998,
        [Display(Name = "Tồn kho")]
        TonKho = 3006,
        [Display(Name = "Tồn kho")]
        TonKhoThep = 3027,
        [Display(Name = "Người mua trả tiền trước")]
        NguoiMuaTraTienTruoc = 3049,


        [Display(Name = "Tiền gửi tại NHNN")]
        TienGuiNHNN = 4311,
        [Display(Name = "Tiền, vàng gửi tại các TCTD khác")]
        TienGuiTCTD = 4344,
        [Display(Name = "Cho vay các TCTD khác")]
        ChoVayTCTD = 4326,
        [Display(Name = "Chứng khoán kinh doanh")]
        ChungKhoanKD = 4346,
        [Display(Name = "Cho vay khách hàng")]
        ChoVayKH = 4348,
        [Display(Name = "Trích lập dự phòng")]
        TrichLap = 4349,
        [Display(Name = "Chứng khoán đầu tư sẵn sàng để bán")]
        ChungKhoanDauTu = 4350,
        [Display(Name = "Chứng khoán giữ đến đáo hạn")]
        ChungKhoanDaoHan = 4351,

        [Display(Name = "LNST Ngân Hàng")]
        LNSTNH = 4378,
        [Display(Name = "Chi phí lãi và các chi phí tương tự")]
        ChiPhiLai = 4396,
        [Display(Name = "Thu nhập từ dịch vụ")]
        ThuNhapTuDichVu = 4397,
        [Display(Name = "Thu nhập từ lãi")]
        ThuNhapLai = 4399,


        [Display(Name = "Thu nhập lãi thuần")]
        ThuNhapLaiThuan = 4385,

        [Display(Name = "Doanh thu thuần")]
        DoanhThuThuan = 4590,
        [Display(Name = "Lợi nhuận kế toán sau thuế")]
        LNKTST = 4585,
        [Display(Name = "Lãi từ tự doanh FVTPL")]
        LaiFVTPL = 5434,
        [Display(Name = "Lãi từ tự doanh HTM")]
        LaiHTM = 5435,
        [Display(Name = "Lãi từ tự doanh AFS")]
        LaiAFS = 5437,
        [Display(Name = "Lãi từ hoạt động cho vay")]
        LaiChoVay = 5436,
        [Display(Name = "Doanh thu môi giới")]
        DoanhThuMoiGioi = 4599,

        [Display(Name = "Lỗ từ tự doanh FVTPL")]
        LoFVTPL = 5439,
        [Display(Name = "Lỗ từ tự doanh HTM")]
        LoHTM = 5440,
        [Display(Name = "Lỗ từ tự doanh AFS")]
        LoAFS = 5442,
        [Display(Name = "Chi phí môi giới")]
        ChiPhiMoiGioi = 5445,

        [Display(Name = "Tài sản FVTPL")]
        TaiSanFVTPL = 5372,
        [Display(Name = "Tài sản HTM")]
        TaiSanHTM = 5398,
        [Display(Name = "Tài sản AFS")]
        TaiSanAFS = 5374,
        [Display(Name = "Tài sản cho vay")]
        TaiSanChoVay = 5373,
        [Display(Name = "Vốn chủ sở hữu")]
        VonChuSoHuuCK = 4478,

        [Display(Name = "Nợ nhóm 1")]
        NoNhom1 = 747,
        [Display(Name = "Nợ nhóm 2")]
        NoNhom2 = 748,
        [Display(Name = "Nợ nhóm 3")]
        NoNhom3 = 749,
        [Display(Name = "Nợ nhóm 4")]
        NoNhom4 = 750,
        [Display(Name = "Nợ nhóm 5")]
        NoNhom5 = 751,
        [Display(Name = "Tiền gửi khách hàng")]
        TienGuiKhachHang = 1056,
        [Display(Name = "Tiền gửi không kỳ hạn")]
        TienGuiKhongKyHan = 1057
    }

    public enum EFinanceIndex
    {
        [Display(Name = "Chỉ số beta")]
        Beta = 61,
        [Display(Name = "Tỷ lệ chi phí hoạt động/Tổng thu nhập HĐKD trước dự phòng (CIR)")]
        CIR = 109
    }

    public enum ECDKTType
    {
        Normal = 1,
        BDS = 2,
        ChungKhoan = 3
    }

    public enum EOrderType
    {
        NONE = -1,
        BUY = 0,
        SELL = 1
    }
    #endregion
    #region Response
    #endregion
}
