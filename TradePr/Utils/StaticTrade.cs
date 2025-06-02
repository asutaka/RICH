using Binance.Net.Clients;
using Bybit.Net.Clients;
using CryptoExchange.Net.Authentication;

namespace TradePr.Utils
{
    public static class StaticTrade
    {
        private static BinanceRestClient _binance;
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
    }
}
