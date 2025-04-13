using Binance.Net.Clients;
using Bybit.Net.Clients;
using CryptoExchange.Net.Authentication;
using TradePr.DAL.Entity;

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

        public static List<string> _lTokenUnlockBlackList = new List<string>
        {
           
            "LAZIO",
            "PORTO",
            "JUV",
            "SANTOS",
            "ATM",
            "ASR",

            "TORN",
            "CFX",
            "RAD",
            "OGN",
            "DAR",
            "CGPT",
            "VOXEL",
            "SEI",
        };

        public static List<string> _lRsiShort = new List<string>
        {
            "EIGENUSDT",
            "NILUSDT",
            "HEIUSDT",
            "NEIROUSDT",
            "DUSKUSDT",
            "KASUSDT",
            "BROCCOLI714USDT",
            "CATIUSDT",
            "GPSUSDT",
            "BIOUSDT",
            "HIPPOUSDT",
            "DYDXUSDT",
            "VICUSDT",
            "PONKEUSDT",
            "MANTAUSDT",
            "OMUSDT",
            "MOODENGUSDT",
            "BERAUSDT",
            "AGLDUSDT",
            "LUMIAUSDT",
            "AXSUSDT",
            "RSRUSDT",
            "ZENUSDT",
            "ALPHAUSDT",
            "LTCUSDT",
            "LINKUSDT",
            "QTUMUSDT",
            "RLCUSDT",
            "CRVUSDT",
            "GALAUSDT",
            "ARPAUSDT",
            "CTSIUSDT",
            "EDUUSDT",
            "AIUSDT",
            "BEAMXUSDT",
            "ETHWUSDT",
            "XVGUSDT",
            "TWTUSDT",
            "CAKEUSDT",
            "1MBABYDOGEUSDT",
            "BADGERUSDT",
            "CHESSUSDT",
            "PROMUSDT",
            "LOKAUSDT",
            "PENGUUSDT",
            "INJUSDT",
            "1000PEPEUSDT",
        };

        public static List<string> _lRsiLong = new List<string>//Chuẩn
        {
            "GRTUSDT",
            "B3USDT",
            "AERGOUSDT",
            "1000BONKUSDT",
            "ZRXUSDT",
            "EPICUSDT",
            "DODOXUSDT",
            "INJUSDT",
            "ALTUSDT",
            "MTLUSDT",
            "ETHFIUSDT",
            "DFUSDT",
            "PLUMEUSDT",
            "QTUMUSDT",
            "TOKENUSDT",
            "CETUSUSDT",
            "ONDOUSDT",
            "OMUSDT",
            "BTCUSDT",
            "ATAUSDT",
            "SEIUSDT",
            "DEGENUSDT",
            "THEUSDT",
            "PHBUSDT",
            "1000FLOKIUSDT",
            "KSMUSDT",
            "PROMUSDT",
            "JOEUSDT",
            "SWELLUSDT",
            "STORJUSDT",
            "STGUSDT",
            "RENDERUSDT",
            "IOTAUSDT",
            "ANKRUSDT",
            "NFPUSDT",
            "BOMEUSDT",
            "BIOUSDT",
            "LPTUSDT",
            "ETHWUSDT",
        };
        public static List<string> _lRsiShort_Bybit = new List<string>
        {
            //Tier 1
            "FWOGUSDT",
            "JUSDT",
            "XVSUSDT",
            "CATIUSDT",
            "HEIUSDT",
            "KOMAUSDT",
            "CAKEUSDT",
            "XEMUSDT",
            "NILUSDT",
            "OLUSDT",
            "DYDXUSDT",
            "FLUXUSDT",
            "IDEXUSDT",
            "KASUSDT",
            "XVGUSDT",
            "MAVIAUSDT",
            "MOODENGUSDT",
            "NCUSDT",
            "MAXUSDT",
            "MBLUSDT",
            "GUSDT",
            "OMNIUSDT",
            "SPELLUSDT",
            "STGUSDT",
            "VVVUSDT",
            "ZBCNUSDT",
            //Tier 2
            "AGLDUSDT",
            "AIOZUSDT",
            "AIUSDT",
            "ALEOUSDT",
            "ALUUSDT",
            "AVAILUSDT",
            "AXLUSDT",
            "BADGERUSDT",
            "BELUSDT",
            "BLASTUSDT",
            "BLURUSDT",
            "COOKUSDT",
            "CPOOLUSDT",
            "EGLDUSDT",
            "EIGENUSDT",
            "EOSUSDT",
            "FLRUSDT",
            "GPSUSDT",
            "LSKUSDT",
            "MBOXUSDT",
            "MDTUSDT",
            "MORPHOUSDT",
            "MOVRUSDT",
            "MVLUSDT",
            "NTRNUSDT",
            "OMUSDT",
            "PENGUUSDT",
            "ROSEUSDT",//
            "SERAPHUSDT",
            "TUSDT",
            "XTZUSDT",
            "ZENUSDT",
        };

        public static List<string> _lRsiLong_Bybit = new List<string>//Chuẩn
        {
            //Tier 1
            "VIDTUSDT",
            "EPICUSDT",
            "DGBUSDT",
            "MASAUSDT",
            "BRUSDT",//
            "BEAMUSDT",//
            "MEMEUSDT",//
            "PENDLEUSDT",
            "NEIROETHUSDT",
            "GMTUSDT",//
            "GPSUSDT",
            "CFXUSDT",
            "MAGICUSDT",
            "SLFUSDT",
            "SANDUSDT",
            "FIDAUSDT",
            "XCHUSDT",
            "VIRTUALUSDT",
            "BIGTIMEUSDT",
            "BNBUSDT",
            "ETHFIUSDT",
            "LUCEUSDT",
            "RAREUSDT",
            "AERGOUSDT",
            "ALICEUSDT",
            "PHBUSDT",
            //Tier 2
            "MASKUSDT",
            "NEOUSDT",
            "VRUSDT",
            "ENJUSDT",
            "SEIUSDT",
            "SNXUSDT",
            "PROMUSDT",
            "SOLOUSDT",
            "TOKENUSDT",
            "WUSDT",
            "ZBCNUSDT",
            "ZILUSDT",
            "ADAUSDT",
            "ALTUSDT",
            "ANKRUSDT",
            "ARKMUSDT",
            "BATUSDT",
            "BTCUSDT",
            "DOGSUSDT",
            "FILUSDT",
            "FLOCKUSDT",
            "FLUXUSDT",
            "GLMRUSDT",
            "GLMUSDT",
            "INJUSDT",
            "KAVAUSDT",
            "LQTYUSDT",
            "MEMEFIUSDT",
            "NCUSDT",
            "PLUMEUSDT",
            "PORTALUSDT",
            "QTUMUSDT",
            "RAYDIUMUSDT",
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

        public static List<PrepareTrade> _lPrepare = new List<PrepareTrade>();

        public static List<string> _lMa20 = new List<string>
        {
            "XRPUSDT",
            "DOGEUSDT",
            "LTCUSDT",
            "BCHUSDT",
            "LINKUSDT",
            "WLDUSDT",
            "ADAUSDT",
            "OPUSDT",
            "1INCHUSDT",
            "JOEUSDT",
            "MAGICUSDT",
            "IDUSDT",
            "STGUSDT",
            "ZILUSDT"
        };
        public static List<string> _lMa20Short = new List<string>
        {
            "AVAXUSDT",
            "HIGHUSDT",
            "XTZUSDT",
            "OPUSDT",
            "KNCUSDT",
            "ZILUSDT",
            "NEOUSDT",
            "APEUSDT",
            "FILUSDT",
            "DYDXUSDT",
            "DOTUSDT",
            "EOSUSDT",
            "CYBERUSDT",
            "WLDUSDT",
            "JOEUSDT",
            "BNBUSDT",
            "PERPUSDT",
            "TRBUSDT",
            "THETAUSDT",
            "STGUSDT",
            "BELUSDT",
            "MDTUSDT",
            "APTUSDT"
        };

        public static List<string> _lMa20_Bybit = new List<string>
        {
            "ARBUSDT",
            "MINAUSDT",
            "QIUSDT",
            "ADAUSDT",
            "LINKUSDT",
            "DOGEUSDT",
            "1INCHUSDT",
            "JOEUSDT",
            "IDUSDT",
            "KNCUSDT",
            "OPUSDT",
            "XRPUSDT",
            "DYDXUSDT",
            "PIXELUSDT",
        };
        public static List<string> _lMa20Short_Bybit = new List<string>
        {
            "HIGHUSDT",
            "AVAXUSDT",
            "WLDUSDT",
            "NEOUSDT",
            "OPUSDT",
            "KNCUSDT",
            "MOODENGUSDT",
            "TRBUSDT",
            "XTZUSDT",
            "MDTUSDT",
            "QIUSDT",
            "BNBUSDT",
            "EOSUSDT",
            "STGUSDT",
            "CYBERUSDT",
            "APEUSDT",
            "PERPUSDT",
            "ZILUSDT",
            "GRTUSDT",
            "JOEUSDT",
            "MINAUSDT",
            "DOTUSDT",
            "C98USDT",
            "DYDXUSDT",
             "1INCHUSDT",
            "MAGICUSDT",
            "ZKJUSDT",
        };
    }
}
