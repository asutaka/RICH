using StockPr.DAL.Entity;
using StockPr.Service;

namespace StockPr.Utils
{
    public static class StaticVal
    {
        public static List<Stock> _lStock = new List<Stock>();
        public static int _MaxRate = 500;
        public static int _TAKE = 15;
        public static (long, long, long, long) _currentTime;//yearquarter + year + quarter + flag


        public static List<DateTime> _lNghiLe = new List<DateTime>
        {
            new DateTime(2025,1,27),
            new DateTime(2025,1,28),
            new DateTime(2025,1,29),
            new DateTime(2025,1,30),
            new DateTime(2025,1,31),
            new DateTime(2025,4,7),
            new DateTime(2025,4,30),
            new DateTime(2025,5,1),
            new DateTime(2025,5,2),
            new DateTime(2025,9,1),
            new DateTime(2025,9,2),

             new DateTime(2026,1,1),
             new DateTime(2026,1,2),
             new DateTime(2026,2,16),
             new DateTime(2026,2,17),
             new DateTime(2026,2,18),
             new DateTime(2026,2,19),
             new DateTime(2026,2,20),
             new DateTime(2026,4,27),
             new DateTime(2026,4,30),
             new DateTime(2026,5,1),
             new DateTime(2025,8,31),
             new DateTime(2025,9,1),
             new DateTime(2025,9,2),
        };

        public static Dictionary<string, int> _dicMa = new Dictionary<string, int>
        {
            {"CTD",2 },
            {"SLS",2 },
            {"KTS",2 },
            {"IDV",1 },
            {"CAP",1 },
            {"TIX",1 },
            {"SFC",1 },
            {"HSG",1 },
            {"FIR",1 },//
            {"SJ1",4 },
        };

        public static List<string> _lBankTP = new List<string>
        {
            "ACB",
            "STB",
            "BID",
            "HDB",
            "MBB",
            "VCB",
            "TPB",
            "OCB",
            "NAB",
            "MSB",
            "EIB",
            "TCB",
            "LPB",
            "VIB",
            "CTG",
            "SHB",
            "VPB"
        };

        public static List<string> _lChungKhoanTP = new List<string>
        {
            "AGR",
            "BSI",
            "CTS",
            "DSC",
            "DSE",
            "EVF",
            "FTS",
            "HCM",
            "MBS",
            "ORS",
            "SSI",
            "TVB",
            "TVS",
            "VCI",
            "VDS",
            "VIX",
            "VND",
            "TCX",
            "VCK"
        };

        public static List<string> _lThepTP = new List<string>
        {
            "HPG",
            "NKG",
            "HSG",
            "SMC",
            "TLH"
        };

        public static List<string> _lDTC_TP = new List<string>
        {
            "VCG",
            "HHV",
            "CTI",
            "HT1",
            "DHA",
            "CTD",
            "FCN",
            "CTR",
        };

        public static List<string> _lBDS_TP = new List<string>
        {
            "HDC",
            "HDG",
            "DPG",
            "DXG",
            "DXS",
            "TCH",
            "HHS",
            "NHA",
            "TAL",
            "AGG",
            "NLG",
            "KDH",
            "PDR",
            "DIG",
            "DC4",
            "CII",
            "EVG",
            "NVL",
            "IJC"
        };

        public static List<string> _lKCNTP = new List<string>
        {
            "KBC",
            "BCM",
            "IDC",
            "VGC",
            "SZC",
            "SIP",
            "PHR",
            "DPR"
        };


        public static List<string> _lThuysanTP = new List<string>
        {
            "VHC",
            "ANV",
            "PAN",
            "DBC",
            "BAF",
            "HAG",
            "ASM",
            "FMC"
        };

        public static List<string> _lDaukhiTP = new List<string>
        {
            "PVS",
            "PVB",
            "PVT",
            "PVD",
            "PVP",
            "BSR",
            "GAS",
            "PLX"
        };

        public static List<string> _lDetmayTP = new List<string>
        {
            "GIL",
            "TCM",
            "MSH",
            "STK",
            "HTG",
            "PPH"
        };

        public static List<string> _lPhanbonTP = new List<string>
        {
            "DCM",
            "DPM",
            "BFC",
            "DGC",
            "LIX"
        };

        public static List<string> _lBanleTP = new List<string>
        {
            "MWG",
            "FRT",
            "DGW",
            "PET",
            "VNM",
            "VRE",
            "MSN",
        };

        public static List<string> _lKhoangsanTP = new List<string>
        {
        };

        public static List<string> _lDienTP = new List<string>
        {
            "REE",
            "GEX",
            "GEE",
            "NT2",
            "TV2",
            "POW",
            "PC1"
        };

        public static List<string> _lKhacTP = new List<string>
        {
            "GMD",
            "VOS",
            "TRC",
            "CSM",
            "DRC",
            "FPT",
            "VTP",
            "VTP",
            "CMG",
            "BVH",
            "MIG",
        };

        public static Dictionary<string, string> _dicSectorIndex = new Dictionary<string, string>
        {
            {"Ngân hàng", "4010"},
            {"Chứng khoán", "4020"},
            {"BDS tập đoàn", "60201040"},
            {"BDS nhà ở", "60201010"},
            {"Khu công nghiệp", "60201030"},
            {"Thép,cao su, phân bón", "1510"},
            {"Bảo hiểm", "4030"},
            {"Điện nước", "5510"},
            {"Bán lẻ", "2550"},
            {"Dầu khí", "1010"},
            {"Xây dựng", "2010"},
            {"Thực phẩm", "3020"},
            {"Dệt may", "2520"},
            {"Cảng, Hàng không", "2030"},
            {"Dược phẩm", "3520"},
            {"Công nghệ", "4510"},
            {"Viễn thông", "5010"},
        };

        public static Dictionary<string, string> _dicSectorChartIndex = new Dictionary<string, string>
        {
            {"Ngân hàng", "4010"},
            {"Chứng khoán", "4020"},
            {"Bất động sản", "6020"},
            {"Nguyên liệu", "1510"},
            {"Bảo hiểm", "4030"},
            {"Điện nước", "5510"},
            {"Bán lẻ", "2550"},
            {"Dầu khí", "1010"},
            {"Xây dựng", "2010"},
            {"Thực phẩm", "3020"},
            {"Dệt may", "2520"},
            {"Vận tải", "2030"},
            {"Dược phẩm", "3520"},
            {"Công nghệ", "4510"},
            {"Viễn thông", "5010"},
        };

        public static Dictionary<string, string> _dicSectorDescription = new Dictionary<string, string>
        {
            {"Nhà ở và Khu công nghiệp", "6020"},
            {"Thép, Cao su, Khoáng sản, Phân bón", "1510"},
            {"Xây dựng, Thủy điện, Gex, Nhựa", "2010"},
            {"Cảng, Hàng không", "2030"},
            {"Thủy sản, Chăn nuôi, Đường", "3020"},
            {"GAS,REE,POW,HDG,NT2,GEG,BWE", "5510"},
            {"Viettel, FOX", "5010"},
        };
    }
}
