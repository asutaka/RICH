using Newtonsoft.Json;
using TradePr.Utils;

namespace TradePr.Service
{
    public interface IBinanceService
    {
        Task GetAccountInfo();
        Task TradeAction();
    }
    public class BinanceService : IBinanceService
    {
        private readonly ILogger<BinanceService> _logger;
        private readonly ICacheService _cacheService;
        private readonly string _api_key = string.Empty;
        private readonly string _api_secret = string.Empty;
        public BinanceService(ILogger<BinanceService> logger, IConfiguration config, ICacheService cacheService) 
        { 
            _logger = logger;
            _api_key = config["Account:API_KEY"];
            _api_secret = config["Account:SECRET_KEY"];
            _cacheService = cacheService;
        }

        public async Task GetAccountInfo()
        {
            try
            {
                var tmp = await StaticVal.BinanceInstance(_api_key, _api_secret).SpotApi.Account.GetAccountInfoAsync();
                var tmp2 = await StaticVal.BinanceInstance(_api_key, _api_secret).UsdFuturesApi.Account.GetBalancesAsync();
                var tmp1 = 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.GetAccountInfo|EXCEPTION| {ex.Message}");
            }
        }

        public async Task TradeAction()
        {
            try
            {
                var liquid = StaticVal.BinanceSocketInstance().UsdFuturesApi.ExchangeData.SubscribeToMiniTickerUpdatesAsync(StaticVal._lCoinAnk, (data) =>
                {
                    var lTrading = _cacheService.GetListTrading();
                    var lSymbol = lTrading.Select(x => x.s).Distinct();
                    if (!lSymbol.Any(x => x == data.Symbol))
                        return;

                    Console.Write($"{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}: {JsonConvert.SerializeObject(data)}");
                    var tmp = 1;



                    //var val = Math.Round(data.Data.AveragePrice * data.Data.Quantity);
                    //if (val >= MIN_VALUE && StaticVal._lCoinAnk.Contains(data.Data.Symbol))
                    //{
                    //    Console.WriteLine(JsonConvert.SerializeObject(data.Data));
                    //    var dt = DateTime.Now;
                    //    var first = _dicRes.FirstOrDefault(x => x.Key == data.Data.Symbol);
                    //    if (first.Key != null)
                    //    {
                    //        var div = (dt - first.Value).TotalSeconds;
                    //        if (div >= 120)
                    //        {
                    //            _dicRes[data.Data.Symbol] = dt;
                    //        }
                    //        else
                    //        {
                    //            return;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        _dicRes.Add(data.Data.Symbol, dt);
                    //    }

                    //    var mes = HandleMessage(data.Data).GetAwaiter().GetResult();
                    //    if (!string.IsNullOrWhiteSpace(mes))
                    //    {
                    //        _teleService.SendMessage(_idUser, mes).GetAwaiter().GetResult();
                    //        //Console.WriteLine(mes);
                    //    }
                    //}
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.GetAccountInfo|EXCEPTION| {ex.Message}");
            }
        }
    }
}
