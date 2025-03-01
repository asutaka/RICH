using Binance.Net.Clients;

namespace TestPr.Utils
{
    public static class StaticVal
    {
        private static BinanceRestClient _binance;
        private static BinanceSocketClient _binanceSocket;
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
    }
}
