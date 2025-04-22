using Binance.Net.Clients;
using Bybit.Net.Clients;

namespace TestPr.Utils
{
    public static class StaticVal
    {
        private static BinanceRestClient _binance;
        private static BybitRestClient _bybit;
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

        public static List<string> _lCoinRecheck = new List<string>
        {
            "ZBCNUSDT"
        };

        public static List<string> _lCoinSpecial = new List<string>
        {
            "MUBARAKUSDT"
        };
    }
}
