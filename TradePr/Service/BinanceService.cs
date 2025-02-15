using MongoDB.Driver;
using Newtonsoft.Json;
using Skender.Stock.Indicators;
using TradePr.DAL;
using TradePr.DAL.Entity;
using TradePr.Utils;

namespace TradePr.Service
{
    public interface IBinanceService
    {
        Task GetAccountInfo();
        Task TradeAction();
        Task TradeTokenUnlock();
        Task TradeTokenUnlockTest();
    }
    public class BinanceService : IBinanceService
    {
        private readonly ILogger<BinanceService> _logger;
        private readonly ICacheService _cacheService;
        private readonly string _api_key = string.Empty;
        private readonly string _api_secret = string.Empty;
        private readonly IActionTradeRepo _actionRepo;
        private readonly IAPIService _apiService;
        public BinanceService(ILogger<BinanceService> logger, IConfiguration config, ICacheService cacheService, IActionTradeRepo actionRepo, IAPIService apiService) 
        { 
            _logger = logger;
            _api_key = config["Account:API_KEY"];
            _api_secret = config["Account:SECRET_KEY"];
            _cacheService = cacheService;
            _actionRepo = actionRepo;
            _apiService = apiService;
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

                    FilterDefinition<ActionTrade> filter = null;
                    var builder = Builders<ActionTrade>.Filter;
                    var lFilter = new List<FilterDefinition<ActionTrade>>()
                    {
                        builder.Eq(x => x.IsFinish, false),
                        builder.Eq(x => x.s, data.Symbol),
                    };
                    foreach (var item in lFilter)
                    {
                        if (filter is null)
                        {
                            filter = item;
                            continue;
                        }
                        filter &= item;
                    }
                    var lProcessing = _actionRepo.GetByFilter(filter);
                    var lTradingClean = lTrading.Where(x => x.s == data.Symbol);
                    foreach ( var item in lTradingClean)
                    {
                        if (lProcessing.Any(x => x.key == item.key))
                            continue;

                        if(item.Mode == (int)ELiquidMode.BanNguocChieu
                           || item.Mode == (int)ELiquidMode.MuaNguocChieu)
                        {
                            //Buy, Sell imediate 
                            _actionRepo.InsertOne(new ActionTrade
                            {
                                key = item.key,
                                s = item.s,
                                Mode = item.Mode,
                                dbuy = (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                                Entry = item.Entry,
                                TP = item.TP,
                                SL = item.SL,
                                IsFinish = false
                            });
                            Console.WriteLine($"{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}: {JsonConvert.SerializeObject(data)}");
                        }
                        else if(item.Mode == (int)ELiquidMode.MuaCungChieu)
                        {
                            if(data.Data.LastPrice >= (decimal)item.Entry && data.Data.LastPrice < (decimal)(item.Entry + Math.Abs(item.TP - item.Entry)/3))
                            {
                                _actionRepo.InsertOne(new ActionTrade
                                {
                                    key = item.key,
                                    s = item.s,
                                    Mode = item.Mode,
                                    dbuy = (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                                    Entry = item.Entry,
                                    TP = item.TP,
                                    SL = item.SL,
                                    IsFinish = false
                                });
                                Console.WriteLine($"{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}: {JsonConvert.SerializeObject(data)}");
                            }
                        }
                        else
                        {
                            if (data.Data.LastPrice <= (decimal)item.Entry && data.Data.LastPrice > (decimal)(item.Entry - Math.Abs(item.TP - item.Entry) / 3))
                            {
                                _actionRepo.InsertOne(new ActionTrade
                                {
                                    key = item.key,
                                    s = item.s,
                                    Mode = item.Mode,
                                    dbuy = (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                                    Entry = item.Entry,
                                    TP = item.TP,
                                    SL = item.SL,
                                    IsFinish = false
                                });
                                Console.WriteLine($"{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}: {JsonConvert.SerializeObject(data)}");
                            }
                        }
                    }

                    if(lProcessing.Any())
                    {
                        var now = DateTimeOffset.Now.ToUnixTimeSeconds();
                        foreach(var item in lProcessing)
                        {
                            if(item.Mode == (int)ELiquidMode.MuaCungChieu 
                            || item.Mode == (int)ELiquidMode.MuaNguocChieu)
                            {
                                if(data.Data.LastPrice >= (decimal)item.TP)
                                {
                                    item.dsell = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                                    item.IsFinish = true;
                                    item.IsWin = 1;
                                    _actionRepo.Update(item);
                                }
                                else if(data.Data.LastPrice <= (decimal)item.SL)
                                {
                                    item.dsell = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                                    item.IsFinish = true;
                                    item.IsWin = 2;
                                    _actionRepo.Update(item);
                                }
                            }
                            else
                            {
                                if (data.Data.LastPrice <= (decimal)item.TP)
                                {
                                    item.dsell = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                                    item.IsFinish = true;
                                    item.IsWin = 1;
                                    _actionRepo.Update(item);
                                }
                                else if (data.Data.LastPrice >= (decimal)item.SL)
                                {
                                    item.dsell = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                                    item.IsFinish = true;
                                    item.IsWin = 2;
                                    _actionRepo.Update(item);
                                }
                            }

                            if(Math.Abs(now - item.dbuy) >= 7200)
                            {
                                item.dsell = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                                item.IsFinish = true;
                                item.IsWin = 3;
                                _actionRepo.Update(item);
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.TradeAction|EXCEPTION| {ex.Message}");
            }
        }

        public async Task TradeTokenUnlock()
        {
            try
            {
                //TP
                var dt = DateTime.Now;
                if (dt.Hour == 23 && dt.Minute == 58)
                {
                    //action  

                }

                var tokens = _cacheService.GetTokenUnlock(dt);
                if (!tokens.Any())
                    return;
               
                if(dt.Hour == 23 && dt.Minute == 59)
                {
                    //action  

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.TradeTokenUnlock|EXCEPTION| {ex.Message}");
            }
        }

        public async Task TradeTokenUnlockTest()
        {
            try
            {
                double totalVal = 0;
                int totalTP = 0;
                int totalSL = 0;
                var val = 150;
                var dt = new DateTime(2024, 9, 30);
                do
                {
                    dt = dt.AddDays(1);
                    var tokens = _cacheService.GetTokenUnlock(dt);
                    if (!tokens.Any())
                        continue;

                    foreach (var item in tokens)
                    {
                        long fromTime = item.time - 60;
                        var dat = await _apiService.GetData($"{item.s}USDT", EInterval.M1, fromTime * 1000);
                        Thread.Sleep(1000);
                        if (!dat.Any())
                            continue;
                        var entityEntry = dat.First();
                        var dat1D = await _apiService.GetData($"{item.s}USDT", EInterval.D1);
                        var lRsi = dat1D.GetRsi();
                        Thread.Sleep(1000);
                        var entityTP = dat.FirstOrDefault(x => x.Date >= dt.AddDays(1));
                        if (entityTP is null)
                            continue;
                        var rsi = lRsi.First(x => x.Date == entityTP.Date);

                        var checkSL = Math.Abs(Math.Round(100 * (-1 + entityTP.High / entityEntry.Open), 1));
                        if (checkSL >= (decimal)1.6)
                        {
                            var SL = Math.Round(val * 0.016, 1);
                            totalVal -= SL;
                            totalSL++;
                            Console.WriteLine($"{dt.ToString("dd/MM/yyyy")}|SL(RSI: {Math.Round(rsi.Rsi ?? 0, 1)})|{item.s}|-{SL}");
                            continue;
                        }

                        var TP = Math.Round(val * (-1 + entityEntry.Open / entityTP.Close), 1);
                        totalVal += (double)TP;
                        totalTP++;
                        Console.WriteLine($"{dt.ToString("dd/MM/yyyy")}|TP(RSI: {Math.Round(rsi.Rsi ?? 0, 1)})|{item.s}|{TP}");
                    }
                }
                while (dt < DateTime.Now);

                Console.WriteLine($"Tong: {totalVal}|TP/SL: {totalTP}/{totalSL}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.TradeTokenUnlock|EXCEPTION| {ex.Message}");
            }
        }
    }
}
