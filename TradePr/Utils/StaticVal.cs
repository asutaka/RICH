﻿using Binance.Net.Clients;
using CryptoExchange.Net.Authentication;

namespace TradePr.Utils
{
    public static class StaticVal
    {
        private static BinanceRestClient _binance;
        private static BinanceSocketClient _binanceSocket;

        public static BinanceRestClient BinanceInstance(string key, string secret)
        {
            if (_binance == null)
            {
                BinanceRestClient.SetDefaultOptions(options =>
                {
                    options.ApiCredentials = new ApiCredentials(key, secret);
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

        public static List<string> _lTokenUnlockBlackList = new List<string>
        {
            //"TORN",
            "LAZIO",
            //"RAD",
            //"OGN",
            //"DAR",
            //"CGPT",
            //"VOXEL",
            //"SEI",
            "PORTO",
            "JUV",
            //"CFX",
            "SANTOS",
            "ATM",
            "ASR",
        };
    }
}
