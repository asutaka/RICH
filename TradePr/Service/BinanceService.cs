﻿using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects.Models.Futures;
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
        //Task GetAccountInfo();
        Task TradeAction();
        Task TradeTokenUnlock();
        Task TradeTokenUnlockTest();
    }
    public class BinanceService : IBinanceService
    {
        private readonly ILogger<BinanceService> _logger;
        private readonly ICacheService _cacheService;
        private readonly IActionTradeRepo _actionRepo;
        private readonly ITokenUnlockTradeRepo _tokenUnlockTradeRepo;
        private readonly IErrorPartnerRepo _errRepo;
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private const long _idUser = 1066022551;
        private const decimal _unit = 30;
        private const decimal _margin = 2;
        public BinanceService(ILogger<BinanceService> logger, ICacheService cacheService, 
                            IActionTradeRepo actionRepo, IAPIService apiService, ITokenUnlockTradeRepo tokenUnlockTradeRepo, 
                            ITeleService teleService, IErrorPartnerRepo errRepo) 
        { 
            _logger = logger;
            _cacheService = cacheService;
            _actionRepo = actionRepo;
            _apiService = apiService;
            _teleService = teleService;
            _tokenUnlockTradeRepo = tokenUnlockTradeRepo;
            _errRepo = errRepo;
        }

        private async Task<BinanceUsdFuturesAccountBalance> GetAccountInfo()
        {
            try
            {
                var resAPI = await StaticVal.BinanceInstance().UsdFuturesApi.Account.GetBalancesAsync();
                return resAPI?.Data?.FirstOrDefault(x => x.Asset == "USDT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.GetAccountInfo|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private async Task<decimal> GetPrice(string symbol)
        {
            try
            {
                var res = await _apiService.GetData(symbol, EInterval.M15);
                return res?.LastOrDefault()?.Close ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.GetPrice|EXCEPTION| {ex.Message}");
            }
            return 0;
        }

        private async Task<bool> PlaceOrder(TokenUnlock token)
        {
            try
            {
                var symbol = $"{token.s}USDT";
                var curPrice = await GetPrice(symbol);
                if (curPrice <= 0)
                    return false;

                var curTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                var account = await GetAccountInfo();
                if (account == null)
                {
                    await _teleService.SendMessage(_idUser, "[ERROR] Không lấy được thông tin tài khoản");
                    return false;
                }

                if (account.AvailableBalance * _margin <= _unit)
                    return false;

                var near = 2; if (curPrice < 1)
                {
                    near = 0;
                }
                var soluong = Math.Round(_unit / curPrice, near);
                var res = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.PlaceOrderAsync(symbol, 
                                                                                                    side: Binance.Net.Enums.OrderSide.Sell, 
                                                                                                    type: Binance.Net.Enums.FuturesOrderType.Market,
                                                                                                    quantity: soluong);
                //nếu lỗi return
                if (!res.Success)
                {
                    _errRepo.InsertOne(new ErrorPartner
                    {
                        s = symbol,
                        time = curTime,
                        ty = (int)ETypeBot.TokenUnlock,
                        action = (int)EAction.Short,
                        des = $"side: {Binance.Net.Enums.OrderSide.Sell.ToString()}, type: {Binance.Net.Enums.FuturesOrderType.Market.ToString()}, quantity: {soluong}"
                    });
                    return false;
                }

                var trade = new TokenUnlockTrade
                {
                    s = symbol,
                    timeUnlock = token.time,
                    timeShort = curTime,
                };

                var resPosition = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.GetPositionsAsync(symbol);
                if (!resPosition.Success)
                {
                    _tokenUnlockTradeRepo.InsertOne(trade);

                    _errRepo.InsertOne(new ErrorPartner
                    {
                        s = symbol,
                        time = curTime,
                        ty = (int)ETypeBot.TokenUnlock,
                        action = (int)EAction.GetPosition
                    });
                   
                    return false;
                }

                if (resPosition.Data.Any())
                {
                    var first = resPosition.Data.First();
                    trade.priceEntry = (double)first.EntryPrice;

                    if (curPrice < 1)
                    {
                        var price = curPrice.ToString().Split('.').Last();
                        price.Reverse();
                        near = long.Parse(price).ToString().Length;
                    }
                    var checkLenght = curPrice.ToString().Split('.').Last();
                    var sl = Math.Round(first.EntryPrice * (decimal)1.016, near);
                    res = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.PlaceOrderAsync(first.Symbol,
                                                                                            side: Binance.Net.Enums.OrderSide.Buy,
                                                                                            type: Binance.Net.Enums.FuturesOrderType.StopMarket,
                                                                                            quantity: soluong,
                                                                                            stopPrice: sl);
                    if (!res.Success)
                    {
                        _tokenUnlockTradeRepo.InsertOne(trade);

                        _errRepo.InsertOne(new ErrorPartner
                        {
                            s = symbol,
                            time = curTime,
                            ty = (int)ETypeBot.TokenUnlock,
                            action = (int)EAction.Short_SL,
                            des = $"side: {Binance.Net.Enums.OrderSide.Buy.ToString()}, type: {Binance.Net.Enums.FuturesOrderType.StopMarket.ToString()}, quantity: {soluong}, stopPrice: {sl}"
                        });
                        
                        return false;
                    }

                    trade.timeStoploss = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                    trade.priceStoploss = (double)sl;

                    _tokenUnlockTradeRepo.InsertOne(trade);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.GetAccountInfo|EXCEPTION| {ex.Message}");
            }
            return false;
        }

        private async Task<bool> PlaceOrderCloseAll(string symbol, decimal quan)
        {
            try
            {
                var res = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.PlaceOrderAsync(symbol,
                                                                                                    side: Binance.Net.Enums.OrderSide.Buy,
                                                                                                    type: Binance.Net.Enums.FuturesOrderType.Market,
                                                                                                    quantity: quan);
                if (!res.Success)
                {
                    await _teleService.SendMessage(_idUser, $"[ERROR] Không thể đóng lệnh {symbol}!");
                    return false;
                }
                var dt = DateTime.UtcNow;
                var time = (int)new DateTimeOffset(dt.Year, dt.Month, dt.Day, 0, 0, 0, TimeSpan.Zero).AddDays(1).ToUnixTimeSeconds();

                FilterDefinition<TokenUnlockTrade> filter = null;
                var builder = Builders<TokenUnlockTrade>.Filter;
                var lFilter = new List<FilterDefinition<TokenUnlockTrade>>()
                        {
                            builder.Eq(x => x.timeUnlock, time),
                            builder.Eq(x => x.s, symbol.Replace("USDT","")),
                        };
                foreach (var itemFilter in lFilter)
                {
                    if (filter is null)
                    {
                        filter = itemFilter;
                        continue;
                    }
                    filter &= itemFilter;
                }
                var entity = _tokenUnlockTradeRepo.GetEntityByFilter(filter);
                if (entity != null)
                {
                    entity.timeClose = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                    entity.priceClose = (double)(await GetPrice(symbol));

                    var rate = Math.Round(100 * (-1 + entity.priceClose / entity.priceEntry), 1);
                    entity.rate = rate;
                    _tokenUnlockTradeRepo.Update(entity);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.PlaceOrderCloseAll|EXCEPTION| {ex.Message}");
            }
            return false;
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
                var dt = DateTime.UtcNow;
                if (dt.Hour == 23 && dt.Minute == 57)
                {
                    //action
                    var resPosition = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.GetPositionsAsync();
                    if (resPosition?.Data?.Any() ?? false)
                    {
                        //close all
                        foreach (var position in resPosition.Data)
                        {
                            await PlaceOrderCloseAll(position.Symbol, Math.Abs(position.PositionAmt));
                        }
                    }
                }

                if (dt.Hour == 23 && dt.Minute == 59)
                {
                    var tokens = _cacheService.GetTokenUnlock(dt);
                    if (!tokens.Any())
                        return;

                    //action  
                    foreach (var item in tokens)
                    {

                        FilterDefinition<TokenUnlockTrade> filter = null;
                        var builder = Builders<TokenUnlockTrade>.Filter;
                        var lFilter = new List<FilterDefinition<TokenUnlockTrade>>()
                        {
                            builder.Eq(x => x.timeUnlock, item.time),
                            builder.Eq(x => x.s, item.s),
                        };
                        foreach (var itemFilter in lFilter)
                        {
                            if (filter is null)
                            {
                                filter = itemFilter;
                                continue;
                            }
                            filter &= itemFilter;
                        }
                        var entity = _tokenUnlockTradeRepo.GetEntityByFilter(filter);
                        if (entity != null)
                            continue;

                        await PlaceOrder(item);
                    }
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
                int totalRSI70 = 0;
                int totalRSI30 = 0;
                var val = 100;
                var dt = new DateTime(2024, 9, 30);
                var lMes = new List<string>();
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
                        Thread.Sleep(200);
                        if (!dat.Any())
                            continue;
                        var entityEntry = dat.First();
                        var dat1D = await _apiService.GetData($"{item.s}USDT", EInterval.D1);
                        var lRsi = dat1D.GetRsi();
                        Thread.Sleep(200);
                        var entityTP = dat1D.FirstOrDefault(x => x.Date >= dt.AddDays(1));
                        if (entityTP is null)
                            continue;
                        var rsi = lRsi.LastOrDefault(x => x.Date < entityTP.Date);
                        if ((rsi?.Rsi ?? 0) <= 30)
                        {
                            continue;
                        }

                        var checkSL = Math.Abs(Math.Round(100 * (-1 + entityTP.High / entityEntry.Open), 1));
                        if (checkSL >= (decimal)1.6)
                        {
                            var SL = Math.Round(val * 0.016, 1);
                            totalVal -= SL;
                            totalSL++;
                            var mesSL = $"{dt.AddDays(1).ToString("dd/MM/yyyy")}|SL|{item.s}|-{SL}";
                            lMes.Add(mesSL);
                            continue;
                        }

                        var rate = Math.Round(100 * (-1 + entityEntry.Open / entityTP.Close), 1);
                        var TP = Math.Round(val * (-1 + entityEntry.Open / entityTP.Close), 1);
                        totalVal += (double)TP;
                        totalTP++;
                        var mesTP = $"{dt.AddDays(1).ToString("dd/MM/yyyy")}|TP|{item.s}|{rate}%|{TP}";
                        lMes.Add(mesTP);
                    }
                }
                while (dt < DateTime.Now);

                foreach (var me in lMes)
                {
                    Console.WriteLine(me);
                }
                Console.WriteLine($"Tong: {totalVal}|TP/SL: {totalTP}/{totalSL}|RSI70/RSI30:{totalRSI70}/{totalRSI30}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.TradeTokenUnlock|EXCEPTION| {ex.Message}");
            }
        }
    }
}
