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
    }
}
