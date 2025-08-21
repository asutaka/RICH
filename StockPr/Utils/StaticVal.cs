using StockPr.DAL.Entity;

namespace StockPr.Utils
{
    public static class StaticVal
    {
        public static List<Stock> _lStock = new List<Stock>();
        public static int _MaxRate = 500;
        public static int _TAKE = 15;
        public static (long, long, long, long) _currentTime;//yearquarter + year + quarter + flag
        public static string _VietStock_Cookie = "dable_uid=91512941.1742123564449; _cc_id=c1aee6ce738dece3a92d2dba4208dc27; language=vi-VN; ASP.NET_SessionId=lywbwxoiy32xeqnm5ca3ms2g; __RequestVerificationToken=RZT3ygQ__y2h1oimLiBtZxD9VVD_W72MzpRQ4RrbHrLq-e1HDk7i8JbsPtKt_0f2PC3Tmmz3wtPXvbZM_cDIpw7I-bHjEBWYP5I4e6h6Nrs1; Theme=Light; __qca=I0-188042809-1752671388575; isShowLogin=true; __gads=ID=0ff784940ee3f238:T=1742489355:RT=1754107937:S=ALNI_MbqMsL50wrMYB6nqzEh1hqwjt21gg; __gpi=UID=0000106ba974e2bf:T=1742489355:RT=1754107937:S=ALNI_MbCo0MLN4S8vKlAHUsRqwV6e8cpCg; __eoi=ID=d8a4eb9640755cdf:T=1742489355:RT=1754107937:S=AA-AfjadUcOHsgRHqZZP0ikhyslg; finance_viewedstock=ACB,; _gid=GA1.2.649554355.1754107963; AnonymousNotification=; CookieLogin=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoibmd1eWVucGh1MTMxMkBnbWFpbC5jb20iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9oYXNoIjoic2stVG40V1ptQzhpZl8tOXk1N3NBQWdpQSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWVpZGVudGlmaWVyIjoibmd1eWVucGh1MTMxMkBnbWFpbC5jb20iLCJleHAiOjE3NTY3MDAyMTgsImlzcyI6Ii52aWV0c3RvY2sudm4iLCJhdWQiOiIudmlldHN0b2NrLnZuIn0.joK9RA8ZZmngKQKCwV0YYy9uRK_GCAsiFYs5JvklpZk; vst_usr_lg_token=N4WNXe5LO02OyeOeHHwRpQ==; _gat_UA-1460625-2=1; _ga=GA1.2.1789690764.1726851987; cto_bidid=VsREDV8lMkJBMllqc2l1Mjk0V3pkQzBhYjFjTm14aDBCMGM0V291elBVSmslMkZuTlJBJTJCNzZqdmdpMll5N0FJb0J2Y2RyMUE1a0VvQmMwYVhtUE1IcU9kRGljZVNBUE1EOFl6U0x4cUtmbVFxUjNYM2xDNCUzRA; FCNEC=%5B%5B%22AKsRol__p5qASO6qer4S-U-OYkDk7aAyHIlGYlqHVOEN3-VyL-9zINPZ6oauvChhmPenDcJnQ_qaMdy7LVdfekVVuULIKYdANd6VczEgOdRgWxz-Tyc4ltwSB-m6cg3r-U9vYtE9pmLI-CHDf6MSF7zOOCetDOqHDQ%3D%3D%22%5D%5D; cto_bundle=1FtCDF9iR2h4WTJWM29hdERrZjVnJTJCciUyQmdsdHpXNUNEJTJCcyUyRnFaR1FiTUk4SGNQWERvM3hlNFpaQWJjZjgxaWozTmNLNWk3JTJGdUszd09laWhvMmtDOG1jZDQzNTJreiUyQk5PWSUyRkglMkZRQ25QcEw2cnJrYlR3REFwRHg2dW1CVk8lMkZLYVBSJTJGS29Kc0hDQm0wa0lFS3glMkJ5RkpjcDAlMkJLNVFNWGlLQSUyRmQ5cHlrS0lTU1pzU2NkYmJYcXpLcDc5Sm11OWk4RG45V3NyeG9lcUQ4SFZZSyUyRnYwY0hwYkZ0NGloYVBrNjYzdWFUcVY5a0pmNUN5OFVSd1p0VWdtaEs5SzkzNHRmNFBUcThWYSUyRmpuUDN5OFR3dkIlMkJPdGN0V2gzd2dnJTNEJTNE; _ga_EXMM0DKVEX=GS2.1.s1754107931$o33$g1$t1754108240$j29$l0$h0";
        public static string _VietStock_Token = "EqlXt2zwPZ7oNC7XEsn1JwpMmJteAqdYIbHcxWgNWZoLYuqDQM5IGKlGfrYYpuf8BlWDNMru6ebFr45z6GBUZqUkMOjxyhBBIrJGhnB5I6_sV5hKu6UGkQQPdXmxNUa1XjYjHUK5sOhuopWiJxNDcg2";

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
            "VND"
        };
    }
}
