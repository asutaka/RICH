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
    }
}
