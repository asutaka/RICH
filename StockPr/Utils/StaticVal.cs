using StockPr.DAL.Entity;

namespace StockPr.Utils
{
    public static class StaticVal
    {
        public static List<Stock> _lStock = new List<Stock>();
        public static int _MaxRate = 500;
        public static int _TAKE = 15;
        public static (long, long, long, long) _currentTime;//yearquarter + year + quarter + flag
        public static string _VietStock_Cookie = "_cc_id=e51d0de85494001334059d580b3b30ee; dable_uid=78795168.1741068525978; cto_bundle=rHmRlV9lSElpWE5IWlhuNHdaQ3JPRHpoWFBSNkglMkI1TUZBUGFhY3glMkZRMVI4b0ltS1c3NEdlQnolMkYlMkJXRWZidlZSaEFQcGtmenFwSDZKUWpYQjZ5JTJGJTJCYkVvViUyQmUlMkZqJTJGWXVNJTJGV3U1QVF6TG4zTG1rNThNN085c1ZLeXhLWWlqQ05mbE1NNkdyQWRGTWJDMTVsNWlvblcwekJWYWV6b2oxWTFiTktzY2kzenc5b2xVb21xTmxzYVFmSWd2NjR5anQ5SDZiTlQlMkJaZzl5MWRXaWslMkJKNE51SmVqYm1BdGhBJTNEJTNE; language=vi-VN; ASP.NET_SessionId=34yetaypvgz4p3nwvz3tktfr; __RequestVerificationToken=hdjGcUlyb6RNr7X4R0ruVkY2W2B9L1qzsuOu4N3IKAaEaxBDGWBsvO9ZkjIs4wZTXHlQAZnR-nGQ2inut8K18kYELJyr7NMvjMEeSEyA2kk1; __qca=I0-1210709677-1754636995953; AnonymousNotification=; Theme=Light; _gid=GA1.2.66909454.1760321855; isShowLogin=true; panoramaId_expiry=1761012118591; panoramaId=933492e46c8542c7c7a59ac7671016d539381d25aa646a6afa0b945b3fe9a49e; panoramaIdType=panoIndiv; __gads=ID=0abcf0b903165ac3:T=1741139637:RT=1760672576:S=ALNI_MaNgChwrdkhXYaA4Lt6rKowzLa0vg; __gpi=UID=0000105519e34a39:T=1741139637:RT=1760672576:S=ALNI_MbN3X8Cpn-2hl4D8Ya9oRcDMrfyhw; __eoi=ID=b056c6ba79e28eb9:T=1756865789:RT=1760672576:S=AA-AfjZmgOpqDQ-UfSdlAQCmDG9F; finance_viewedstock=DCM,; CookieLogin=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoibmd1eWVucGh1MTMxMkBnbWFpbC5jb20iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9oYXNoIjoic2stVG40V1ptQzhpZl8tOXk1N3NBQWdpQSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWVpZGVudGlmaWVyIjoibmd1eWVucGh1MTMxMkBnbWFpbC5jb20iLCJleHAiOjE3NjMyNjQ2MzIsImlzcyI6Ii52aWV0c3RvY2sudm4iLCJhdWQiOiIudmlldHN0b2NrLnZuIn0.bLx-jJkfEBaLl3P486h1U3jtXEwRFIgLs2rTaW7Q2dA; vst_usr_lg_token=8rLjnCXzTUmW5f4/ZV4Naw==; cto_bidid=NuUSWF9EM1lOVG1DU3ZhV1cyUGx3dnJ2UHpqbk1SaGolMkJuVHZUN3AzNzJGWHRibUViZTJ4VGVRaFBOWGRXQ04lMkJYSjlicUtETmRJc0ZjVEhSMVh0R3BpV3RrSDFkU2hpNXMzVzhORFFLNmllMktCREklM0Q; _ga=GA1.2.1634064005.1724032252; FCNEC=%5B%5B%22AKsRol8Xl2vynxurnn2Im-Pf9PZP2bw3XZzkeoa1d-21z1H0Cj2ZyZCWUgoLG3DQKlG82bF3Xhf3pSZ4BWvRYcGb7_kHmqG9MaL8GlmH4lRb52tt0YVgOVROWtb0AJiTp9TKsIlAMeyytd48yEsiuCL5cEm0M3bCuw%3D%3D%22%5D%5D; cto_bundle=MkNb6V9lSElpWE5IWlhuNHdaQ3JPRHpoWFBkOGQ0SWpwdTJXd2RFNWdqUHN3QnNSNjREMnN3TnBDWHc3aEdVTkFjNXF4ZkpXNWdoUDRDV25WJTJGd2FXWUNudzZwQnJYdFFoc2E1bTRsJTJCNDhVY3J1QUJkNnJ5N3FCZExaOGFOOFRyV3VhSVo2eXA5MHJUT21UZlVyclJsQUlZcGd2aUZWMyUyQjVtRE5pOWtvSU1HdmtLNiUyQllCb2xVNmZERkMlMkZzeGIlMkJvZm43OERuNzUlMkJIZ2hQYXEwN0doakFrdXlCQTRVbVZMdUI4bzVFZ2dLZHdQNTVrRW1kSUFDUkhjWVFZS2ZhV1VBclh1WW8lMkZnQUNDQlFjY3NSWVM1cWZyNVBCOGclM0QlM0Q; cto_bundle=MkNb6V9lSElpWE5IWlhuNHdaQ3JPRHpoWFBkOGQ0SWpwdTJXd2RFNWdqUHN3QnNSNjREMnN3TnBDWHc3aEdVTkFjNXF4ZkpXNWdoUDRDV25WJTJGd2FXWUNudzZwQnJYdFFoc2E1bTRsJTJCNDhVY3J1QUJkNnJ5N3FCZExaOGFOOFRyV3VhSVo2eXA5MHJUT21UZlVyclJsQUlZcGd2aUZWMyUyQjVtRE5pOWtvSU1HdmtLNiUyQllCb2xVNmZERkMlMkZzeGIlMkJvZm43OERuNzUlMkJIZ2hQYXEwN0doakFrdXlCQTRVbVZMdUI4bzVFZ2dLZHdQNTVrRW1kSUFDUkhjWVFZS2ZhV1VBclh1WW8lMkZnQUNDQlFjY3NSWVM1cWZyNVBCOGclM0QlM0Q; _ga_EXMM0DKVEX=GS2.1.s1760672576$o336$g1$t1760672641$j60$l0$h0";
        public static string _VietStock_Token = "gi6LsAX1ZhkDn7MpHWyWfCQooSfb9-eJgOLThir5syfeIqeRElPmUE_mPs_btcjlY4YQsHdE-vL7pccYSFe6BlqQLpHov1ieuTrbVIU32WeJMRCc4nIFHh0jUzg9qjiMWUfxg1afBA1h5Ai_LOZNSA2";

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
    }
}
