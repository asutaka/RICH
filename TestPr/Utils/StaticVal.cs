using Binance.Net.Clients;
using Bybit.Net.Clients;

namespace TestPr.Utils
{
    public static class StaticVal
    {
        private static BinanceRestClient _binance;
        private static BybitRestClient _bybit;
        private static BinanceSocketClient _binanceSocket;
        public static BinanceRestClient BinanceInstance()
        {
            if (_binance == null)
            {
                _binance = new BinanceRestClient();
            }
            return _binance;
        }

        public static BybitRestClient ByBitInstance()
        {
            if (_bybit == null)
            {
                _bybit = new BybitRestClient();
            }
            return _bybit;
        }

        public static BinanceSocketClient BinanceSocketInstance()
        {
            if (_binanceSocket == null)
            {
                _binanceSocket = new BinanceSocketClient();
            }
            return _binanceSocket;
        }

        public static List<string> _lIgnoreThreeSignal = new List<string>
        {
            "BTCUSDT",
            "ETHUSDT",
            "XRPUSDT"
        };

        public static List<string> _lIgnoreSignal = new List<string>
        {
            "BTCUSDT",
        };

        public static Dictionary<string, (int, int)> _dicCoinAnk = new Dictionary<string, (int, int)>
        {
            { "BTCUSDT", ( 3,1 ) },
            { "ETHUSDT", ( 2,2 ) },
            { "XRPUSDT", ( 0,4 ) },
            { "BNBUSDT", ( 2,1 ) },
            { "SOLUSDT", ( 1,2 ) },
            { "DOGEUSDT", ( 0,5 ) },
            { "LTCUSDT",  ( 1,2 ) },
            { "BCHUSDT", ( 2,1 ) },
            { "DOTUSDT", ( 1,4 ) },
            { "LINKUSDT", ( 1,3 ) },
            { "ARBUSDT",  ( 1,4 ) },
            { "WLDUSDT",  ( 1,4 ) },
            { "ADAUSDT",  ( 0,4 ) },
            { "OPUSDT", ( 1,4 ) },
            { "MKRUSDT", ( 3,1 ) },
            { "PERPUSDT", ( 0,4 ) },
            { "APTUSDT", ( 2,3 ) },
            { "EOSUSDT", ( 1,4 ) },
            { "SUIUSDT", ( 0,4 ) },
            { "FILUSDT", ( 1,3 ) },
            { "ETCUSDT", ( 1,3 ) },
            { "AVAXUSDT", ( 1,3 ) },
            { "CYBERUSDT", ( 1,3 ) },
            { "ATOMUSDT", ( 1,3 ) },
            { "APEUSDT", ( 1,4 ) },
            { "DYDXUSDT", ( 1,4 ) },
            { "TRBUSDT", ( 2,2 ) },
            { "RUNEUSDT", ( 1,3 ) },
            { "AGLDUSDT", ( 1,4 ) },
            { "SUSHIUSDT", ( 1,4 ) },
            { "GRTUSDT", ( 1,5 ) },
            { "1INCHUSDT", ( 1,4 ) },
            { "JOEUSDT", ( 0,4 ) },
            { "LINAUSDT", ( 0,5 ) },
            { "GTCUSDT", ( 1,4 ) },
            { "XTZUSDT", ( 1,4 ) },
            { "NEOUSDT", ( 2,3 ) },
            { "BELUSDT", ( 0,4 ) },
            { "MAGICUSDT", ( 0,4 ) },
            { "EGLDUSDT", ( 2,2 ) },
            { "GMXUSDT", ( 2,3 ) },
            { "IDUSDT", ( 0,4 ) },
            { "THETAUSDT", ( 1,4 ) },
            { "KNCUSDT", ( 1,4 ) },
            { "STGUSDT", ( 1,4 ) },
            { "C98USDT", ( 1,5 ) },
            { "ZILUSDT", ( 0,5 ) },
            { "MDTUSDT", ( 0,5 ) },
            { "MINAUSDT", ( 1,4 ) },
            { "HIGHUSDT", ( 1,4 ) },
            { "PIXELUSDT", ( 0,5 ) },
            { "QIUSDT", ( 0,5 ) },
            { "ZKJUSDT", ( 1,4 ) },
            { "MERLUSDT", ( 1,5 ) },
            { "ZBCNUSDT", ( 0,6 ) },
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
            "MDTUSDT",
            "APEUSDT",
            "BTCUSDT",
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
            "BTCUSDT",
            "GTCUSDT",
            "LINKUSDT",
            "1INCHUSDT",
            "ARBUSDT",
            "EOSUSDT",
            "BCHUSDT",
            "LTCUSDT",
            "ETHUSDT",
            "MDTUSDT",
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

        public static List<string> _lMa20Short_Bybit = new List<string>
        {
            "ARBUSDT",
            "OPUSDT",
            "MKRUSDT",
            "EOSUSDT",
            "FILUSDT",
            "ETCUSDT",
            "DYDXUSDT",
            "AGLDUSDT",
            "GRTUSDT",
            "IDUSDT",
            "STGUSDT",
            "C98USDT"
        };
    }
}
