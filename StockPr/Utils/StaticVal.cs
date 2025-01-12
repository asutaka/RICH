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
        public static string _VietStock_Cookie = "ASP.NET_SessionId=nlehv103ci5raqevopu2cysl; __RequestVerificationToken=Mmg6CGomYCXe4BZkIkoJRLvUmhZ99CUb4coeUeaymZCN9xYHumQFNeWQBeu2TlJriagiaA-3vecniiRfEVRWQBIsYA-Gi-PysFMLQ8FjpXY1; language=vi-VN; _ga=GA1.1.1634064005.1724032252; _ga_EXMM0DKVEX=GS1.1.1724032251.1.0.1724032251.60.0.0; AnonymousNotification=; Theme=Light; vts_usr_lg=8E7FC9B57A7F485E1BF6E003F8700B04A197873D535172F8B72CBF3E943E96E892AE7919BFE55F31E327735F1C715D801A1EEA9F5DEE728F9642A841504B610E1D6B04D958309DAEDE842A620B055DDD0480800FD63C2D047B2CB31F794DC93F9B7E7116E87AFEDB3CF0C90934AC0B47950BC5585E235CFF35E59885BEFCDCCE";
        public static string _VietStock_Token = "wVIyNRGpnFhrFclsY80ON85OurU8C1z0U53Yhn8uPuHtKkP2RNX7XMWZQXaP3xTANcAMaFUCAcCkgD5lAxbLRJ6t89Ui-MFsrh90SL6z57ygdjrSXm9sxaLvFZCYx0im0";

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
