﻿using StockPr.DAL.Entity;

namespace StockPr.Utils
{
    public static class StaticVal
    {
        public static List<Stock> _lStock = new List<Stock>();
        public static int _MaxRate = 500;
        public static int _TAKE = 15;
        public static (long, long, long, long) _currentTime;//yearquarter + year + quarter + flag
        public static string _VietStock_Cookie = "_cc_id=e51d0de85494001334059d580b3b30ee; dable_uid=78795168.1741068525978; cto_bundle=rHmRlV9lSElpWE5IWlhuNHdaQ3JPRHpoWFBSNkglMkI1TUZBUGFhY3glMkZRMVI4b0ltS1c3NEdlQnolMkYlMkJXRWZidlZSaEFQcGtmenFwSDZKUWpYQjZ5JTJGJTJCYkVvViUyQmUlMkZqJTJGWXVNJTJGV3U1QVF6TG4zTG1rNThNN085c1ZLeXhLWWlqQ05mbE1NNkdyQWRGTWJDMTVsNWlvblcwekJWYWV6b2oxWTFiTktzY2kzenc5b2xVb21xTmxzYVFmSWd2NjR5anQ5SDZiTlQlMkJaZzl5MWRXaWslMkJKNE51SmVqYm1BdGhBJTNEJTNE; AnonymousNotification=; Theme=Light; language=vi-VN; ASP.NET_SessionId=34yetaypvgz4p3nwvz3tktfr; __RequestVerificationToken=hdjGcUlyb6RNr7X4R0ruVkY2W2B9L1qzsuOu4N3IKAaEaxBDGWBsvO9ZkjIs4wZTXHlQAZnR-nGQ2inut8K18kYELJyr7NMvjMEeSEyA2kk1; _gid=GA1.2.1999726924.1746426429; panoramaId_expiry=1747031229414; panoramaId=69c3fd4fac5f01ef0528f8d8e42a16d53938c68c3e34690965818297c1824d25; panoramaIdType=panoIndiv; __gads=ID=0abcf0b903165ac3:T=1741139637:RT=1746598913:S=ALNI_MaNgChwrdkhXYaA4Lt6rKowzLa0vg; __gpi=UID=0000105519e34a39:T=1741139637:RT=1746598913:S=ALNI_MbN3X8Cpn-2hl4D8Ya9oRcDMrfyhw; __eoi=ID=cca6c1ecfceed66a:T=1741139637:RT=1746598913:S=AA-Afjad0WA-6OwxOw8DSj-TZVis; CookieLogin=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoibmd1eWVucGh1MTMxMkBnbWFpbC5jb20iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9oYXNoIjoic2stVG40V1ptQzhpZl8tOXk1N3NBQWdpQSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWVpZGVudGlmaWVyIjoibmd1eWVucGh1MTMxMkBnbWFpbC5jb20iLCJleHAiOjE3NDkxOTA5MTgsImlzcyI6Ii52aWV0c3RvY2sudm4iLCJhdWQiOiIudmlldHN0b2NrLnZuIn0.BZGZK78UiLpZBpP_4K6bpDo_3gxITrDSm_2nkcfBJ5U; finance_viewedstock=TCM,DRC,TTN,BMP,ACB,; cto_bidid=5L9veF9EM1lOVG1DU3ZhV1cyUGx3dnJ2UHpqbk1SaGolMkJuVHZUN3AzNzJGWHRibUViZTJ4VGVRaFBOWGRXQ04lMkJYSjlicUtETmRJc0ZjVEhSMVh0R3BpV3RrSDZ2eHZTYTdUcjBTbVk4UWxOaXFVQkElM0Q; _ga=GA1.2.1634064005.1724032252; _gat_UA-1460625-2=1; FCNEC=%5B%5B%22AKsRol8-iq3ZQ2q3kzKn85e-sqqhNICcZj_gW_eBiD6Di80qRv7FoTyIRb9DZA9wTUTNvcdNylSkLZx0RpDqKPkkHd2lmYI7462zqLzP3BjUQYIikMmaGi26Cv6K_ompNULuF4zw3gAH7xM1OswUMp2UDl6o0bAEyA%3D%3D%22%5D%5D; cto_bundle=_IxXQ19lSElpWE5IWlhuNHdaQ3JPRHpoWFBjcyUyRm80MXdxcGhyN2YlMkZOSnpldkVzUmFRQXBxRjUlMkZTaXBvcmJlVzdPVWl5RXNnY0pjQmpST0h2QWlRMmRxT1VhUDVqbm1SZ010VVdpMmNFTlRtRlNNYlBraElvSnlOMVBBVGZJOE5tRjI5aEVmN0luYUJwYXJ6cGZmJTJCR0JpNjhPNW9Pc29pRWViQXQlMkZTaWhKSk5KbllJM0hkNU5OaENqWE1lbGtxS3VuTWVXQVBtQVhuNWVYTFhHRVVJb2t5ZGlVVG94ZEpDUjRIMnlKNWtpdyUyRnU0V1VLeGw4a0pkRnRhcDFPNEVwMWF5MUp2MjFrcGs3aVVEREglMkJTV2VnZkN3SmJnJTNEJTNE; cto_bundle=_IxXQ19lSElpWE5IWlhuNHdaQ3JPRHpoWFBjcyUyRm80MXdxcGhyN2YlMkZOSnpldkVzUmFRQXBxRjUlMkZTaXBvcmJlVzdPVWl5RXNnY0pjQmpST0h2QWlRMmRxT1VhUDVqbm1SZ010VVdpMmNFTlRtRlNNYlBraElvSnlOMVBBVGZJOE5tRjI5aEVmN0luYUJwYXJ6cGZmJTJCR0JpNjhPNW9Pc29pRWViQXQlMkZTaWhKSk5KbllJM0hkNU5OaENqWE1lbGtxS3VuTWVXQVBtQVhuNWVYTFhHRVVJb2t5ZGlVVG94ZEpDUjRIMnlKNWtpdyUyRnU0V1VLeGw4a0pkRnRhcDFPNEVwMWF5MUp2MjFrcGs3aVVEREglMkJTV2VnZkN3SmJnJTNEJTNE; _ga_EXMM0DKVEX=GS2.1.s1746598913$o44$g1$t1746598947$j26$l0$h0";
        public static string _VietStock_Token = "D_av6u7vWB8lCXLYHhTCUD6rt68Zbwq2DAdZ_FIXiFaC5erIiwJyIyaqupjM_e5N_7nDFpS3Ikc_uIT9i8usxpmXriO5rTiqv3j0P-BAGrdZQW9HJzbAHsytD1xzBVrDX0FmRy7HUI6Gz7JZKUjncw2";

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
