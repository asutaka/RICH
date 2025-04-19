using Binance.Net.Clients;
using Bybit.Net.Clients;
using CryptoExchange.Net.Authentication;

namespace TradePr.Utils
{
    public static class StaticVal
    {
        private static BinanceRestClient _binance;
        private static BinanceSocketClient _binanceSocket;

        private static BybitRestClient _bybit;

        public static string _binance_key;
        public static string _binance_secret;

        public static string _bybit_key;
        public static string _bybit_secret;

        public static BinanceRestClient BinanceInstance()
        {
            if (_binance == null)
            {
                BinanceRestClient.SetDefaultOptions(options =>
                {
                    options.ApiCredentials = new ApiCredentials(_binance_key, _binance_secret);
                });
                _binance = new BinanceRestClient();
            }
            return _binance;
        }

        public static BinanceSocketClient BinanceSocketInstance()
        {
            if (_binanceSocket == null)
            {
                _binanceSocket = new BinanceSocketClient();
            }
            return _binanceSocket;
        }

        public static BybitRestClient ByBitInstance()
        {
            if (_bybit == null)
            {
                BybitRestClient.SetDefaultOptions(options =>
                {
                    options.ApiCredentials = new ApiCredentials(_bybit_key, _bybit_secret);
                });
                _bybit = new BybitRestClient();
            }
            return _bybit;
        }

        public static List<string> _lRsiShort_Binance = new List<string>
        {
            "BROCCOLI714USDT",
            "NILUSDT",
            "1000XUSDT",
            "HEIUSDT",
            "OMUSDT",
            "CATIUSDT",
            "GRIFFAINUSDT",
            "GPSUSDT",
            "DUSKUSDT",
            "LTCUSDT",
            "BIOUSDT",
            "NEIROUSDT",
            "PONKEUSDT",
            "LINKUSDT",
            "MORPHOUSDT",
            "AVAAIUSDT",
            "VICUSDT",
            "KASUSDT",
            "DYDXUSDT",
            "HOOKUSDT",
            "XVGUSDT",
            "PENGUUSDT",
            "BADGERUSDT",
            "BAKEUSDT",
            "CAKEUSDT",
            "ONDOUSDT",
            "OMNIUSDT",
            "1MBABYDOGEUSDT",
            "SAFEUSDT",
            "VETUSDT",
            "API3USDT",
            "1000000MOGUSDT",
            "RUNEUSDT",
            "BERAUSDT",
            "AGLDUSDT",
            "CRVUSDT",
            "TRUUSDT",
            "TWTUSDT",
            "DEXEUSDT",
            "IPUSDT",
            "CHESSUSDT",
            "PROMUSDT",
            "CTSIUSDT",
            "EDUUSDT",
            "ETHWUSDT",
            "FIOUSDT",
            "MOODENGUSDT",
            "QTUMUSDT",
            "ALPHAUSDT",
            "APEUSDT",
            "ORDIUSDT",
            "LISTAUSDT",
        };

        public static List<string> _lRsiLong_Binance = new List<string>//Chuẩn
        {
            "MUBARAKUSDT",
            "1000WHYUSDT",
            "1000BONKUSDT",
            "B3USDT",
            "SAFEUSDT",
            "LUMIAUSDT",
            "PLUMEUSDT",
            "RAREUSDT",
            "VIRTUALUSDT",
            "VTHOUSDT",
            "INJUSDT",
            "QTUMUSDT",
            "ARKMUSDT",
            "ORDIUSDT",
            "ALTUSDT",
            "ADAUSDT",
            "CETUSUSDT",
            "PHBUSDT",
            "DODOXUSDT",
            "GRTUSDT",
            "BADGERUSDT",
            "NFPUSDT",
            "BSWUSDT",
            "RAYSOLUSDT",
            "1000CHEEMSUSDT",
            "THEUSDT",
            "BIOUSDT",
            "SEIUSDT",
            "ZRXUSDT",
            "ONDOUSDT",
            "LPTUSDT",
            "CFXUSDT",
            "BRETTUSDT",
            "CATIUSDT",
            "EPICUSDT",
            "PHAUSDT",
            "IOTAUSDT",
            "IOSTUSDT",
            "ALGOUSDT",
            "KSMUSDT",
            "GLMUSDT",
            "BTCUSDT",
            "STORJUSDT",
            "1000FLOKIUSDT",
            "LQTYUSDT",
            "TOKENUSDT",
            "ACXUSDT",
            "ETCUSDT",
            "SANDUSDT",
            "ANKRUSDT",
            "MTLUSDT",
            "MAGICUSDT",
            "BBUSDT",
            "RENDERUSDT",
            "PROMUSDT",
        };

        public static List<string> _lRsiShort_Bybit = new List<string>
        {
            "NILUSDT",
            "MAVIAUSDT",
            "HEIUSDT",
            "JUSDT",
            "CATIUSDT",
            "OLUSDT",
            "MORPHOUSDT",
            "NCUSDT",
            "IDEXUSDT",
            "XVSUSDT",
            "ZBCNUSDT",
            "AIOZUSDT",
            "CPOOLUSDT",
            "TROYUSDT",
            "ALUUSDT",
            "XVGUSDT",
            "MAJORUSDT",
            "XCHUSDT",
            "OMNIUSDT",
            "XNOUSDT",
            "OMUSDT",
            "ZENUSDT",
            "FWOGUSDT",
            "FLUXUSDT",
            "VICUSDT",
            "PENGUUSDT",
            "PERPUSDT",
            "EGLDUSDT",
            "RDNTUSDT",
            "FLOCKUSDT",
            "MEMEFIUSDT",
            "SHELLUSDT",
            "ENSUSDT",
            "SPECUSDT",
            "KASUSDT",
            "FOXYUSDT",
            "DYDXUSDT",
            "PORTALUSDT",
            "ROSEUSDT",
            "ZRCUSDT",
            "AEROUSDT",
            "ALICEUSDT",
            "GODSUSDT",
            "LINKUSDT",
            "XTZUSDT",
            "SPELLUSDT",
            "BLURUSDT",
            "VTHOUSDT",
            "ANKRUSDT",
            "LISTAUSDT",
        };

        public static List<string> _lRsiLong_Bybit = new List<string>//Chuẩn
        {
            "DGBUSDT",
            "GPSUSDT",
            "AUDIOUSDT",
            "ZBCNUSDT",
            "GMTUSDT",
            "MEMEUSDT",
            "SERAPHUSDT",
            "VIRTUALUSDT",
            "PENDLEUSDT",
            "AERGOUSDT",
            "RAREUSDT",
            "RAYDIUMUSDT",
            "PYTHUSDT",
            "FLRUSDT",
            "GOATUSDT",
            "DATAUSDT",
            "MANEKIUSDT",
            "MAXUSDT",
            "PHBUSDT",
            "AI16ZUSDT",
            "SLFUSDT",
            "XCHUSDT",
            "ALTUSDT",
            "GLMUSDT",
            "POPCATUSDT",
            "ZILUSDT",
            "PROMUSDT",
            "BIGTIMEUSDT",
            "FIDAUSDT",
            "BNBUSDT",
            "KNCUSDT",
            "LUMIAUSDT",
            "LUCEUSDT",
            "MOVRUSDT",
            "ANKRUSDT",
            "GLMRUSDT",
            "BSWUSDT",
            "BTCUSDT",
            "FLOCKUSDT",
            "LPTUSDT",
            "KAVAUSDT",
            "TLMUSDT",
            "MAVUSDT",
            "TOKENUSDT",
            "QTUMUSDT",
            "ACXUSDT",
            "ARKMUSDT",
            "FLUXUSDT",
            "INJUSDT",
            "LDOUSDT",
            "LQTYUSDT",
            "MTLUSDT",
            "PLUMEUSDT",
            "XVGUSDT",
            "SUNDOGUSDT",
        };

        public static List<string> _lCoinRecheck = new List<string>
        {
            "ZBCNUSDT"
        };

        public static List<string> _lCoinSpecial = new List<string>
        {
            "MUBARAKUSDT"
        };


        public static Dictionary<string, int> _dicBinanceMargin = new Dictionary<string, int>
        {
            { "1INCHUSDT", 75 },
            { "LINKUSDT", 75 },
            { "ZILUSDT", 50 },
            { "OPUSDT", 75 },
            { "HIGHUSDT", 25 },
            { "STGUSDT", 25 },
            { "XTZUSDT", 50 },
            { "ATOMUSDT", 75 },
            { "ARBUSDT", 25 },
            { "LINAUSDT", 7 },
            { "DOTUSDT", 75 },
            { "FILUSDT", 75 },
            { "ETCUSDT", 75 },
            { "DOGEUSDT", 75 },
            { "MAGICUSDT", 75 },
            { "APEUSDT", 75 },
            { "BTCUSDT", 125 },
            { "SUIUSDT", 75 },
            { "MINAUSDT", 50 },
            { "AVAXUSDT", 75 },
            { "AGLDUSDT", 25 },
            { "GRTUSDT", 50 },
            { "DYDXUSDT", 75 },
            { "NEOUSDT", 50 },
            { "IDUSDT", 75 },
            { "GTCUSDT", 75 },
            { "ETHUSDT", 125 },
            { "THETAUSDT", 50 },
            { "EOSUSDT", 75 },
            { "BCHUSDT", 75 },
            { "LTCUSDT", 75 },
            { "XRPUSDT", 75 },
            { "TRBUSDT", 26 },
            { "WLDUSDT", 75 },
            { "MKRUSDT", 75 },
            { "PIXELUSDT", 75 },
            { "APTUSDT", 75 },
            { "JOEUSDT", 75 },
            { "EGLDUSDT", 75 },
            { "C98USDT", 25 },
            { "CYBERUSDT", 20 },
            { "BELUSDT", 20 },
            { "ADAUSDT", 75 },
            { "OMUSDT", 5 }
        };
    }
}
