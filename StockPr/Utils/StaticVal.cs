using StockPr.DAL.Entity;

namespace StockPr.Utils
{
    public static class StaticVal
    {
        public static List<Stock> _lStock = new List<Stock>();
        public static int _MaxRate = 500;
        public static int _TAKE = 15;
        public static int _curQuarter = 20244;
        public static (long, long, long, long) _currentTime;//yearquarter + year + quarter + flag
        public static string _VietStock_Cookie = "ASP.NET_SessionId=arn5rig5dxbmmjoizukepdtp; __RequestVerificationToken=lsueRAFEInzD1hnxKUdgW2KwoJG9BTcIbmeErlT-ZbsnP-s9bR_xm25X1mXPVAj19x9vzoY6uLnwZW889qjTsK-YscWe_4RZQo5ErQDjJBE1; _ga_EXMM0DKVEX=GS1.1.1726851987.1.0.1726851987.60.0.0; _ga=GA1.1.1789690764.1726851987; fileDownload=true; language=vi-VN; AnonymousNotification=; finance_viewedstock=ACB,; Theme=Light; vts_usr_lg=7B048DE8E732DB21C0DC64D90F12D4431D3B81657307DAE046866EEE85040B7B0C01EB62FF3BDBA3852AD3771082B78E6D42E0EBA4790E4651DCAC54E3D210BA53B79E1550AE93177C36F0756EBC6B89225880E7B23A37C58616194B2D3AB6F812CA80FF0565FF93D8CE807499B3CC0F4D0E0E4698C770FD4FD8A1B719F50C27; vst_usr_lg_token=XIHQ1mz1jE+YDnHSvak5uA==";
        public static string _VietStock_Token = "7JGS5cE1JOnO0ahmaHORE-TfLL2cwOSn8PW2HOlAi9hrGZcTIx1bHmNKAh5cCTNjz6Aaz3BLDHOQwdZ6JAqACPuAC_TfSi3tVisBTv_JkYF6juHbiKElDlPSkt5lrDzZ0";

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
    }
}
