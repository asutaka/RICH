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
            {"XRPUSDT",7 },
            {"BNBUSDT",7 },
            {"SOLUSDT",7 },
            {"DOGEUSDT",7 },
            {"LTCUSDT",7 },
            {"BCHUSDT",7 },
            {"DOTUSDT",7 },
            {"LINKUSDT",7 },
            {"ARBUSDT",7 },
            {"WLDUSDT",7 },
            {"MATICUSDT",7 },
            {"ADAUSDT",7 },
            {"OPUSDT",7 },
            {"UNFIUSDT",7 },
            {"MKRUSDT",7 },
            {"PERPUSDT",7 },
            {"APTUSDT",7 },
            {"EOSUSDT",7 },
            {"SUIUSDT",7 },
            {"FILUSDT",7 },
            {"ETCUSDT",7 },
            {"AVAXUSDT",7 },
            {"CYBERUSDT",7 },
            {"ATOMUSDT",7 },
            {"APEUSDT",7 },
            {"DYDXUSDT",7 },
            {"TRBUSDT",7 },
            {"RUNEUSDT",7 },
            {"AGLDUSDT",7 },
            {"SUSHIUSDT",7 },
            {"GRTUSDT",7 },
            {"ANTUSDT",7 },
            {"1INCHUSDT",7 },
            {"JOEUSDT",7 },
            {"LINAUSDT",7 },
            {"GTCUSDT",7 },
            {"XTZUSDT",7 },
            {"NEOUSDT",7 },
            {"BELUSDT",7 },
            {"MAGICUSDT",7 },
            {"EGLDUSDT",7 },
            {"GMXUSDT",7 },
            {"IDUSDT",7 },
            {"THETAUSDT",7 },
            {"VETAUSDT",7 },
            {"KNCUSDT",7 },
            {"STORGUSDT",7 },
            {"AGIXUSDT",7 },
            {"OCEANUSDT",7 },
            {"STGUSDT",7 },
            {"C98USDT",7 },
            {"ZILUSDT",7 },
            {"MDTUSDT",7 },
            {"KLAYUSDT",7 },
            {"MINAUSDT",7 },
            {"HIGHUSDT",7 },
            {"STMXUSDT", 7 }
        };
    }
}
