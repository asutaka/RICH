using Amazon.Runtime.Internal.Transform;
using Binance.Net.Clients;
using Bybit.Net.Clients;

namespace CoinPr.Utils
{
    public static class StaticVal
    {
        private static BybitRestClient _bybit;
        private static BybitSocketClient _bybitSocket;
        private static BinanceRestClient _binance;
        private static BinanceSocketClient _binanceSocket;

        public static BybitRestClient ByBitInstance()
        {
            if (_bybit == null)
            {
                _bybit = new BybitRestClient();
            }
            return _bybit;
        }

        public static BybitSocketClient BybitSocketInstance()
        {
            if (_bybitSocket == null)
            {
                _bybitSocket = new BybitSocketClient();
            }
            return _bybitSocket;
        }


        public static BinanceRestClient BinanceInstance()
        {
            if (_binance == null)
            {
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

        public static List<string> _lCoinAnk = new List<string>
        {
            "BTCUSDT",
            "ETHUSDT",
            "XRPUSDT",
            "BNBUSDT",
            "SOLUSDT",
            "DOGEUSDT",
            "LTCUSDT",
            "BCHUSDT",
            "DOTUSDT",
            "LINKUSDT",
            "ARBUSDT",
            "WLDUSDT",
            "MATICUSDT",
            "ADAUSDT",
            "OPUSDT",
            "UNFIUSDT",
            "MKRUSDT",
            "PERPUSDT",
            "APTUSDT",
            "EOSUSDT",
            "SUIUSDT",
            "FILUSDT",
            "ETCUSDT",
            "AVAXUSDT",
            "CYBERUSDT",
            "ATOMUSDT",
            "APEUSDT",
            "DYDXUSDT",
            "TRBUSDT",
            "RUNEUSDT",
            "AGLDUSDT",
            "SUSHIUSDT",
            "GRTUSDT",
            "ANTUSDT",
            "1INCHUSDT",
            "JOEUSDT",
            "LINAUSDT",
            "GTCUSDT",
            "XTZUSDT",
            "NEOUSDT",
            "BELUSDT",
            "MAGICUSDT",
            "EGLDUSDT",
            "GMXUSDT",
            "IDUSDT",
            "THETAUSDT",
            "VETAUSDT",
            "KNCUSDT",
            "STORGUSDT",
            "AGIXUSDT",
            "OCEANUSDT",
            "STGUSDT",
            "C98USDT",
            "ZILUSDT",
            "MDTUSDT",
            "KLAYUSDT",
            "MINAUSDT",
            "HIGHUSDT",
            "STMXUSDT"
        };

        public static Dictionary<string, int> _dicMargin = new Dictionary<string, int>
        {
            {"BTCUSDT",10 },
            {"ETHUSDT",10 },
            {"XRPUSDT",5 },
            {"BNBUSDT",5 },
            {"SOLUSDT",5 },
            {"DOGEUSDT",5 },
            {"LTCUSDT",5 },
            {"BCHUSDT",5 },
            {"DOTUSDT",5 },
            {"LINKUSDT",5 },
            {"ARBUSDT",5 },
            {"WLDUSDT",5 },
            {"MATICUSDT",5 },
            {"ADAUSDT",5 },
            {"OPUSDT",5 },
            {"UNFIUSDT",5 },
            {"MKRUSDT",5 },
            {"PERPUSDT",5 },
            {"APTUSDT",5 },
            {"EOSUSDT",5 },
            {"SUIUSDT",5 },
            {"FILUSDT",5 },
            {"ETCUSDT",5 },
            {"AVAXUSDT",5 },
            {"CYBERUSDT",5 },
            {"ATOMUSDT",5 },
            {"APEUSDT",5 },
            {"DYDXUSDT",5 },
            {"TRBUSDT",5 },
            {"RUNEUSDT",5 },
            {"AGLDUSDT",5 },
            {"SUSHIUSDT",5 },
            {"GRTUSDT",5 },
            {"ANTUSDT",5 },
            {"1INCHUSDT",5 },
            {"JOEUSDT",5 },
            {"LINAUSDT",5 },
            {"GTCUSDT",5 },
            {"XTZUSDT",5 },
            {"NEOUSDT",5 },
            {"BELUSDT",5 },
            {"MAGICUSDT",5 },
            {"EGLDUSDT",5 },
            {"GMXUSDT",5 },
            {"IDUSDT",5 },
            {"THETAUSDT",5 },
            {"VETAUSDT",5 },
            {"KNCUSDT",5 },
            {"STORGUSDT",5 },
            {"AGIXUSDT",5 },
            {"OCEANUSDT",5 },
            {"STGUSDT",5 },
            {"C98USDT",5 },
            {"ZILUSDT",5 },
            {"MDTUSDT",5 },
            {"KLAYUSDT",5 },
            {"MINAUSDT",5 },
            {"HIGHUSDT",5 },
            {"STMXUSDT", 5 }
        };
    }
}
