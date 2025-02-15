using MongoDB.Driver;
using Newtonsoft.Json;
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
        public BinanceService(ILogger<BinanceService> logger, IConfiguration config, ICacheService cacheService, IActionTradeRepo actionRepo) 
        { 
            _logger = logger;
            _api_key = config["Account:API_KEY"];
            _api_secret = config["Account:SECRET_KEY"];
            _cacheService = cacheService;
            _actionRepo = actionRepo;
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
                //TP
                var dt = DateTime.Now;
                if (dt.Hour == 23 && dt.Minute == 58)
                {
                    //action  

                }

                var tokens = _cacheService.GetTokenUnlock(dt);
                if (!tokens.Any())
                    return;

                if (dt.Hour == 23 && dt.Minute == 59)
                {
                    //action  

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.TradeTokenUnlock|EXCEPTION| {ex.Message}");
            }
        }
    }
}
