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
            "HEIUSDT",
            "OMUSDT",
            "CATIUSDT",
            "GRIFFAINUSDT",
            "GPSUSDT",
            "DUSKUSDT",
            "NEIROUSDT",
            "PONKEUSDT",
            "LINKUSDT",
            "MORPHOUSDT",
            "AVAAIUSDT",
            "VICUSDT",
            "KASUSDT",
            "DYDXUSDT",
            "BADGERUSDT",
            "BAKEUSDT",
            "CAKEUSDT",
            "ONDOUSDT",
            "OMNIUSDT",
            "1MBABYDOGEUSDT",
            "SAFEUSDT",
            "1000000MOGUSDT",
            "RUNEUSDT",
            "BERAUSDT",
            "MANTAUSDT",
            "AGLDUSDT",
            "CRVUSDT",
            "TRUUSDT",
            "TWTUSDT",
            "DEXEUSDT",
            "IPUSDT",
            "CHESSUSDT",
            "PROMUSDT",
            "AXSUSDT",
            "ANKRUSDT",
            "CTSIUSDT",
            "ACHUSDT",
            "EDUUSDT",
            "ETHWUSDT",
            "AIUSDT",
            "XAIUSDT",
            "FIOUSDT",
            "MOODENGUSDT",
            "QTUMUSDT",
            "ALPHAUSDT",
            "APEUSDT",
            "ORDIUSDT",
            "LSKUSDT",
            "LISTAUSDT",
            "EIGENUSDT",
            "MOVEUSDT",
        };

        public static List<string> _lRsiLong_Binance = new List<string>//Chuẩn
        {
            "MUBARAKUSDT",
            "1000WHYUSDT",
            "B3USDT",
            "LUMIAUSDT",
            "1000BONKUSDT",
            "EPICUSDT",
            "INJUSDT",
            "GRTUSDT",
            "ORDIUSDT",
            "ALTUSDT",
            "ARKMUSDT",
            "QTUMUSDT",
            "PLUMEUSDT",
            "VINEUSDT",
            "PHBUSDT",
            "SAFEUSDT",
            "ZRXUSDT",
            "CETUSUSDT",
            "ATAUSDT",
            "THEUSDT",
            "DODOXUSDT",
            "ADAUSDT",
            "DFUSDT",
            "TOKENUSDT",
            "KSMUSDT",
            "PROMUSDT",
            "BSWUSDT",
            "MTLUSDT",
            "ONDOUSDT",
            "1000FLOKIUSDT",
            "ETHFIUSDT",
            "SEIUSDT",
            "RAREUSDT",
            "AXLUSDT",
            "VIRTUALUSDT",
            "PHAUSDT",
            "JOEUSDT",
            "BTCUSDT",
            "RAYSOLUSDT",
            "VTHOUSDT",
            "OMUSDT",
            "DEGENUSDT",
            "ZILUSDT",
            "SANDUSDT",
            "MASKUSDT",
            "BEAMXUSDT",
            "RENDERUSDT",
            "IOTAUSDT",
            "ANKRUSDT",
            "BIOUSDT",
            "ETCUSDT",
            "CAKEUSDT",
            "ALICEUSDT",
            "CATIUSDT"
        };

        public static List<string> _lRsiShort_Bybit = new List<string>
        {
            "NILUSDT",
            "HEIUSDT",
            "JUSDT",
            "CATIUSDT",
            "OLUSDT",
            "MORPHOUSDT",
            "NCUSDT",
            "OMUSDT",
            "IDEXUSDT",
            "XVGUSDT",
            "XVSUSDT",
            "ZENUSDT",
            "FWOGUSDT",
            "KOMAUSDT",
            "CAKEUSDT",
            "FLUXUSDT",
            "MANTAUSDT",
            "VICUSDT",
            "CPOOLUSDT",
            "PENGUUSDT",
            "DYDXUSDT",
            "OMNIUSDT",
            "TROYUSDT",
            "XCHUSDT",
            "PERPUSDT",
            "ACHUSDT",
            "ENSUSDT",
            "SPECUSDT",
            "XEMUSDT",
            "KASUSDT",
            "MAVIAUSDT",
            "MOODENGUSDT",
            "SPELLUSDT",
            "BLASTUSDT",
            "STGUSDT",
            "ZBCNUSDT",
            "AIOZUSDT",
            "ALUUSDT",
            "AXLUSDT",
            "BLURUSDT",
            "LSKUSDT",
            "MOVRUSDT",
            "SERAPHUSDT",
            "TUSDT",
            "VTHOUSDT",
            "MEMEUSDT",
            "ANKRUSDT",
            "IPUSDT",
            "LISTAUSDT",
        };

        public static List<string> _lRsiLong_Bybit = new List<string>//Chuẩn
        {
            "DGBUSDT",
            "AERGOUSDT",
            "GMTUSDT",
            "GPSUSDT",
            "MEMEUSDT",
            "XCHUSDT",
            "RAYDIUMUSDT",
            "VIRTUALUSDT",
            "PENDLEUSDT",
            "BEAMUSDT",
            "PHBUSDT",
            "ZBCNUSDT",
            "SLFUSDT",
            "GOATUSDT",
            "MANEKIUSDT",
            "PYTHUSDT",
            "BIGTIMEUSDT",
            "BNBUSDT",
            "CFXUSDT",
            "FIDAUSDT",
            "NEOUSDT",
            "LQTYUSDT",
            "RAREUSDT",
            "SANDUSDT",
            "WUSDT",
            "ZILUSDT",
            "ADAUSDT",
            "ALICEUSDT",
            "ALTUSDT",
            "ARKMUSDT",
            "FLUXUSDT",
            "PLUMEUSDT",
            "BTCUSDT",
            "COMPUSDT",
            "PROMUSDT",
            "DATAUSDT",
            "ENJUSDT",
            "ETHFIUSDT",
            "GLMRUSDT",
            "RLCUSDT",
            "SOLOUSDT",
            "VRUSDT",
        };

        public static List<string> _lCoinRecheck = new List<string>
        {
            "ZBCNUSDT"
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
