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
        //Sym, Quan, Price
        public static Dictionary<string, (int, int)> _dicCoinAnk = new Dictionary<string, (int, int)>
        {
            { "BTCUSDT", ( 3,1 ) },  
            { "ETHUSDT", ( 2,2 ) },
            { "XRPUSDT", ( 0,4 ) },
            { "BNBUSDT", ( 2,1 ) },
            { "SOLUSDT", ( 0,2 ) },
            { "DOGEUSDT", ( 0,5 ) },
            { "LTCUSDT",  ( 1,2 ) },
            { "BCHUSDT", ( 2,1 ) },
            { "DOTUSDT", ( 1,4 ) },
            { "LINKUSDT", ( 1,3 ) },
            { "ARBUSDT",  ( 1,4 ) },
            { "WLDUSDT",  ( 0,4 ) },
            { "ADAUSDT",  ( 0,4 ) },
            { "OPUSDT", ( 1,4 ) },
            { "MKRUSDT", ( 3,1 ) },
            { "PERPUSDT", ( 0,4 ) },
            { "APTUSDT", ( 1,3 ) },
            { "EOSUSDT", ( 1,4 ) },
            { "SUIUSDT", ( 0,4 ) },
            { "FILUSDT", ( 1,3 ) },
            { "ETCUSDT", ( 1,3 ) },
            { "AVAXUSDT", ( 0,3 ) },
            { "CYBERUSDT", ( 1,3 ) },
            { "ATOMUSDT", ( 1,3 ) },
            { "APEUSDT", ( 0,4 ) },
            { "DYDXUSDT", ( 1,4 ) },
            { "TRBUSDT", ( 1,2 ) },
            { "RUNEUSDT", ( 0,3 ) },
            { "AGLDUSDT", ( 0,4 ) },
            { "SUSHIUSDT", ( 0,4 ) },
            { "GRTUSDT", ( 0,5 ) },
            { "1INCHUSDT", ( 0,4 ) },
            { "JOEUSDT", ( 0,4 ) },
            { "LINAUSDT", ( 0,5 ) },
            { "GTCUSDT", ( 1,4 ) },
            { "XTZUSDT", ( 1,4 ) },
            { "NEOUSDT", ( 2,3 ) },
            { "BELUSDT", ( 0,4 ) },
            { "MAGICUSDT", ( 0,4 ) },
            { "EGLDUSDT", ( 1,2 ) },
            { "GMXUSDT", ( 2,3 ) },
            { "IDUSDT", ( 0,4 ) },
            { "THETAUSDT", ( 1,4 ) },
            { "KNCUSDT", ( 0,4 ) },
            { "STGUSDT", ( 0,4 ) },
            { "C98USDT", ( 0,5 ) },
            { "ZILUSDT", ( 0,5 ) },
            { "MDTUSDT", ( 0,5 ) },
            { "MINAUSDT", ( 0,4 ) },
            { "HIGHUSDT", ( 1,4 ) },
            { "PIXELUSDT", ( 0,5 ) },
            { "QIUSDT", ( 0,5 ) },
            { "ZKJUSDT", ( 1,4 ) },
            { "MERLUSDT", ( 1,5 ) },
            { "ZBCNUSDT", ( 0,6 ) },
        };

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

        public static List<string> _lIgnoreThreeSignal = new List<string>
        {
            "BTCUSDT",
            "ETHUSDT",
            "XRPUSDT"
        };

        public static List<string> _lRsiShort = new List<string>
        {
            "1INCHUSDT",
            "LINKUSDT",
            "ZILUSDT",
            "OPUSDT",
            "HIGHUSDT",
            "STGUSDT",
            "XTZUSDT",
            "ATOMUSDT",
            "ARBUSDT",
            "LINAUSDT",
            "DOTUSDT",
            "FILUSDT",
            "ETCUSDT",
            "DOGEUSDT",
            "MAGICUSDT",
            //"MDTUSDT",
            "APEUSDT",
            //"BTCUSDT",
            "SUIUSDT",
            "MINAUSDT",
            "AVAXUSDT",
            "AGLDUSDT",
            "GRTUSDT",
            "DYDXUSDT",
            "NEOUSDT",
            "IDUSDT",
            "GTCUSDT",
            "ETHUSDT",
            "THETAUSDT"
        };

        public static List<string> _lRsiLong = new List<string>
        {
            "ETCUSDT",
            "HIGHUSDT",
            "FILUSDT",
            "DOTUSDT",
            //"BTCUSDT",
            "GTCUSDT",
            "LINKUSDT",
            "1INCHUSDT",
            "ARBUSDT",
            "EOSUSDT",
            "BCHUSDT",
            "LTCUSDT",
            "ETHUSDT",
            //"MDTUSDT",
            "NEOUSDT",
            "XRPUSDT",
            "STGUSDT",
            "TRBUSDT",
            "WLDUSDT",
            "MKRUSDT",
            "PIXELUSDT",
            "APTUSDT",
            "THETAUSDT",
            "DOGEUSDT",
            "JOEUSDT",
            "EGLDUSDT",
            "LINAUSDT",
            "AGLDUSDT",
            "C98USDT",
            "ZILUSDT",
            "CYBERUSDT",
            "BELUSDT",
            "DYDXUSDT",
            "ATOMUSDT",
            "ADAUSDT"
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
            { "ADAUSDT", 75 }
        };

        public static List<string> _lRsi = new List<string>();
        public static List<PrepareTrade> _lPrepare = new List<PrepareTrade>();
    }
}
