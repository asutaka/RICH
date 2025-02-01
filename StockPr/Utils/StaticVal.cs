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
        public static string _VietStock_Cookie = "ASP.NET_SessionId=arn5rig5dxbmmjoizukepdtp; __RequestVerificationToken=lsueRAFEInzD1hnxKUdgW2KwoJG9BTcIbmeErlT-ZbsnP-s9bR_xm25X1mXPVAj19x9vzoY6uLnwZW889qjTsK-YscWe_4RZQo5ErQDjJBE1";
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
    }
}
