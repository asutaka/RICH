using Binance.Net.Objects.Models.Futures;
using MongoDB.Driver;
using Skender.Stock.Indicators;
using TradePr.DAL.Entity;
using TradePr.DAL;
using TradePr.Utils;
using Binance.Net.Enums;

namespace TradePr.Service
{
    public interface IBinanceService
    {
        Task<BinanceUsdFuturesAccountBalance> Binance_GetAccountInfo();
        Task Binance_Trade();
        Task<SignalBase> PlaceOrder(SignalBase entity, decimal lastPrice);
    }
    public class BinanceService : IBinanceService
    {
        private readonly ILogger<BinanceService> _logger;
        private readonly ITradingRepo _tradingRepo;
        private readonly IPrepareTradeRepo _prepareRepo;
        private readonly IErrorPartnerRepo _errRepo;
        private readonly IConfigDataRepo _configRepo;
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private const long _idUser = 1066022551;
        private const decimal _unit = 50;
        private const decimal _margin = 10;
        private readonly int _forceSell = 5;
        private readonly int _HOUR = 2;
        private readonly decimal _SL_RATE = 0.017m;
        private readonly int _exchange = (int)EExchange.Binance;
        private object _locker = new object();
        public BinanceService(ILogger<BinanceService> logger, ITradingRepo tradingRepo, ISignalTradeRepo signalTradeRepo, IErrorPartnerRepo errRepo,
                            IConfigDataRepo configRepo, IPrepareTradeRepo prepareRepo, 
                            IAPIService apiService, ITeleService teleService)
        {
            _logger = logger;
            _tradingRepo = tradingRepo;
            _prepareRepo = prepareRepo;
            _errRepo = errRepo;
            _configRepo = configRepo;
            _apiService = apiService;
            _teleService = teleService;
        }
        public async Task<BinanceUsdFuturesAccountBalance> Binance_GetAccountInfo()
        {
            try
            {
                var resAPI = await StaticVal.BinanceInstance().UsdFuturesApi.Account.GetBalancesAsync();
                return resAPI?.Data?.FirstOrDefault(x => x.Asset == "USDT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.Binance_GetAccountInfo|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task Binance_Trade()
        {
            try
            {
                var dt = DateTime.Now;
                await Binance_TakeProfit(dt);
                await Binance_TradeMA20(dt);
                await Binance_TradeLiquid(dt);
                await Binance_TradeRSI(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.Binance_Trade|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Binance_TradeLiquid(DateTime dt)
        {
            try
            {
                if (dt.Minute % 15 != 0)
                    return;

                var time = (int)DateTimeOffset.Now.AddMinutes(-15).ToUnixTimeSeconds();
                var lTrade = _tradingRepo.GetByFilter(Builders<Trading>.Filter.Gte(x => x.d, time));
                if (!(lTrade?.Any() ?? false))
                    return;

                var lSym = lTrade.Select(x => x.s).Distinct();
                foreach (var sym in lSym)
                {
                    var trade = lTrade.Where(x => x.s == sym).OrderByDescending(x => x.d).First();
                    var pos = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.GetPositionsAsync(sym);
                    Thread.Sleep(500);
                    if (pos.Data.Any())
                        continue;

                    //gia
                    var lData15m = await _apiService.GetData(sym, EInterval.M15);
                    var lRsi = lData15m.GetRsi();
                   
                    var cur = lData15m.SkipLast(1).Last();
                    var rsi = lRsi.SkipLast(1).Last();

                    var model = new PrepareTrade
                    {
                        s = sym,
                        ty = (int)EOption.Liquid,
                        detectTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                        detectDate = DateTime.Now
                    };
                    if (trade.Side == (int)OrderSide.Buy)
                    {
                        var curRate = Math.Round(Math.Abs(cur.Open - cur.Close) * 100 / Math.Abs(cur.High - cur.Low));
                        if (!StaticVal._lRsiLong.Contains(sym))
                            continue;
                        if (curRate >= 40)
                            continue;
                        if (rsi.Rsi >= 30 || rsi.Rsi <= 25)
                            continue;
                        if (Math.Abs(cur.Open - cur.Close) > (Math.Min(cur.Open, cur.Close) - cur.Low))
                            continue;
                        if ((cur.High - Math.Max(cur.Open, cur.Close)) > (Math.Min(cur.Open, cur.Close) - cur.Low))
                            continue;
                        if (lRsi.SkipLast(2).TakeLast(5).Any(x => x.Rsi is null || x.Rsi < 25))
                            continue;

                        var low = cur.Low + 0.1m * (cur.High - cur.Low);

                        model.Side = (int)OrderSide.Buy;
                        model.Entry = (double)low;
                    }
                    else
                    {
                        if (!StaticVal._lRsiShort.Contains(sym))
                            continue;

                        if (rsi.Rsi >= 80 || rsi.Rsi < 75)
                            continue;

                        if (lRsi.SkipLast(2).TakeLast(5).Any(x => x.Rsi is null || x.Rsi > 80))
                            continue;

                        model.Side = (int)OrderSide.Sell;
                        model.Entry = (double)cur.High;
                    }

                    if(model.Entry > 0)
                    {
                        _prepareRepo.InsertOne(model);
                        Monitor.Enter(_locker);
                        StaticVal._lPrepare.Add(model);
                        Monitor.Exit(_locker);
                        //await _teleService.SendMessage(_idUser, $"[PREPARE-Liquid] {model.s}|{(OrderSide)model.Side}|ENTRY: {model.Entry}| Time: {(int)DateTimeOffset.Now.ToUnixTimeSeconds()}");
                        Console.WriteLine($"[PREPARE-Liquid] {model.s}|{(OrderSide)model.Side}|ENTRY: {model.Entry}| Time: {(int)DateTimeOffset.Now.ToUnixTimeSeconds()}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.Binance_TradeLiquid|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Binance_TradeRSI(DateTime dt)
        {
            try
            {
                if (dt.Minute % 15 != 0)
                    return;

                var time = (int)DateTimeOffset.Now.AddMinutes(-15).ToUnixTimeSeconds();
                var lPrepare = _prepareRepo.GetByFilter(Builders<PrepareTrade>.Filter.Gte(x => x.detectTime, time));

                var lSym = StaticVal._lRsiShort.Concat(StaticVal._lRsiLong);
                foreach (var sym in lSym)
                {
                    if (lPrepare.Any(x => x.s == sym))
                        continue;

                    //gia
                    var lData15m = await _apiService.GetData(sym, EInterval.M15);
                    Thread.Sleep(100);
                    var lRsi = lData15m.GetRsi();

                    var cur = lData15m.SkipLast(1).Last();
                    var rsi = lRsi.SkipLast(1).Last();

                    var model = new PrepareTrade
                    {
                        s = sym,
                        ty = (int)EOption.RSI,
                        detectTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                        detectDate = DateTime.Now
                    };

                    if (rsi.Rsi > 25 && rsi.Rsi < 30)
                    {
                        var curRate = Math.Round(Math.Abs(cur.Open - cur.Close) * 100 / Math.Abs(cur.High - cur.Low));
                        if (!StaticVal._lRsiLong.Contains(sym))
                            continue;
                        if (curRate >= 40)
                            continue;
                        if (Math.Abs(cur.Open - cur.Close) > (Math.Min(cur.Open, cur.Close) - cur.Low))
                            continue;
                        if ((cur.High - Math.Max(cur.Open, cur.Close)) > (Math.Min(cur.Open, cur.Close) - cur.Low))
                            continue;
                        if (lRsi.SkipLast(2).TakeLast(5).Any(x => x.Rsi is null || x.Rsi < 25))
                            continue;

                        var low = cur.Low + 0.1m * (cur.High - cur.Low);

                        model.Side = (int)OrderSide.Buy;
                        model.Entry = (double)low;
                    }
                    else if(rsi.Rsi >= 75 && rsi.Rsi < 80)
                    {
                        if (!StaticVal._lRsiShort.Contains(sym))
                            continue;
                        if (lRsi.SkipLast(2).TakeLast(5).Any(x => x.Rsi is null || x.Rsi > 80))
                            continue;

                        model.Side = (int)OrderSide.Sell;
                        model.Entry = (double)cur.High;
                    }

                    if (model.Entry > 0)
                    {
                        _prepareRepo.InsertOne(model);
                        Monitor.Enter(_locker);
                        StaticVal._lPrepare.Add(model);
                        Monitor.Exit(_locker);
                        //await _teleService.SendMessage(_idUser, $"[PREPARE-RSI] {model.s}|{(OrderSide)model.Side}|ENTRY: {model.Entry}| Time: {(int)DateTimeOffset.Now.ToUnixTimeSeconds()}");
                        Console.WriteLine($"[PREPARE-RSI] {model.s}|{(OrderSide)model.Side}|ENTRY: {model.Entry}| Time: {(int)DateTimeOffset.Now.ToUnixTimeSeconds()}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.Binance_TradeRSI|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Binance_TradeMA20(DateTime dt)
        {
            try
            {
                if (dt.Minute % 15 != 0)
                    return;
                var pos = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.GetPositionsAsync();
                var time = (int)DateTimeOffset.Now.AddMinutes(-60).ToUnixTimeSeconds();
                var lSym = StaticVal._lMa20Short.Concat(StaticVal._lMa20);
                foreach (var item in lSym)
                {
                    try
                    {
                        if (pos.Data.Any(x => x.Symbol == item))
                            continue;
                        //gia
                        var l15m = await _apiService.GetData(item, EInterval.M15);
                        Thread.Sleep(100);
                        if (l15m is null
                            || !l15m.Any())
                            continue;
                        var last = l15m.Last();
                        l15m.Remove(last);
                        var lbb = l15m.GetBollingerBands();
                        var cur = l15m.Last();
                        var prev = l15m.SkipLast(1).Last();
                        var prev2 = l15m.SkipLast(2).Last();

                        var bb = lbb.Last();
                        var bb_Prev = lbb.SkipLast(1).Last();
                        var bb_Prev2 = lbb.SkipLast(2).Last();

                        var lPrev = l15m.SkipLast(1).TakeLast(5);
                        var indexPrev = 0;
                        decimal totalPrev = 0;
                        foreach (var itemPrev in lPrev)
                        {
                            var prevRate = Math.Round(Math.Abs(itemPrev.Open - itemPrev.Close) * 100 / Math.Abs(itemPrev.High - itemPrev.Low));
                            if (prevRate > 10)
                            {
                                indexPrev++;
                                totalPrev += Math.Abs(itemPrev.Open - itemPrev.Close);
                            }
                        }
                        if (indexPrev <= 0)
                            continue;

                        var avgPrev = totalPrev / indexPrev;
                        if (Math.Abs(cur.Open - cur.Close) <= 2 * avgPrev
                               || Math.Abs(cur.Open - cur.Close) > avgPrev * 4)
                            continue;

                        var sideDetect = -1;
                        //Short
                        if (StaticVal._lMa20Short.Any(x => x == item))
                        {
                            ShortAction();
                        }

                        //Long
                        if (StaticVal._lMa20.Any(x => x == item))
                        {
                            LongAction();
                        }

                        if (sideDetect > -1)
                        {
                            var res = await PlaceOrder(new SignalBase
                            {
                                s = item,
                                ex = _exchange,
                                Side = sideDetect,
                                timeFlag = (int)DateTimeOffset.Now.ToUnixTimeSeconds()
                            }, last.Close);

                            if (res != null)
                            {
                                var first = StaticVal._dicCoinAnk.First(x => x.Key == res.s);
                                var side = ((OrderSide)res.Side).ToString().ToUpper();
                                var price = Math.Round(res.priceEntry, first.Value.Item2);

                                var mes = $"[ACTION - {side}|BB_Binance] {res.s}|ENTRY: {price}";
                                await _teleService.SendMessage(_idUser, mes);
                            }
                        }

                        void ShortAction()
                        {
                            if (cur.Open <= cur.Close
                               || cur.Close >= (decimal)bb.Sma.Value
                               || cur.Open <= (decimal)bb.Sma.Value
                               || prev.Low < (decimal)bb_Prev.Sma.Value
                               || prev2.Low < (decimal)bb_Prev2.Sma.Value
                               || (((decimal)bb.Sma - cur.Close) > (cur.Close - (decimal)bb.LowerBand)))
                                return;
                            //SHORT
                            sideDetect = (int)OrderSide.Sell;
                        }

                        void LongAction()
                        {
                            if (cur.Open >= cur.Close
                               || cur.Close <= (decimal)bb.Sma.Value
                               || cur.Open >= (decimal)bb.Sma.Value
                               || prev.High > (decimal)bb_Prev.Sma.Value
                               || prev2.High > (decimal)bb_Prev2.Sma.Value
                               || ((cur.Close - (decimal)bb.Sma) > ((decimal)bb.UpperBand - cur.Close))
                               || (cur.Close - (decimal)bb.Sma) * 2 < ((decimal)bb.Sma - cur.Open))
                                return;
                            //LONG
                            sideDetect = (int)OrderSide.Buy;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.Binance_TradeMA20|EXCEPTION|{item}| {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.Binance_TradeMA20|EXCEPTION| {ex.Message}");
            }
            return;
        }

        private async Task Binance_TakeProfit(DateTime dt)
        {
            try
            {
                var index = 0;
                var pos = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.GetPositionsAsync();
                #region Sell
                foreach (var item in pos.Data)
                {
                    var side = item.PositionAmt < 0 ? OrderSide.Sell : OrderSide.Buy;
                    var curTime = (DateTime.UtcNow - item.UpdateTime.Value).TotalHours;
                    if(curTime >= 2)
                    {
                        index++;
                        await PlaceOrderClose(item);
                    }
                    else
                    {
                        var l15m = await _apiService.GetData(item.Symbol, EInterval.M15);
                        Thread.Sleep(100);
                        if (l15m is null || !l15m.Any())
                            continue;

                        var last = l15m.Last();
                        l15m.Remove(last);

                        var cur = l15m.Last();
                        var lbb = l15m.GetBollingerBands();
                        var bb = lbb.Last();
                        var flag = false;
                        if (side == OrderSide.Buy && cur.Close > (decimal)bb.UpperBand.Value)
                        {
                            flag = true;
                        }
                        else if (side == OrderSide.Sell && cur.Close < (decimal)bb.LowerBand.Value)
                        {
                            flag = true;
                        }

                        if (flag)
                        {
                            index++;
                            await PlaceOrderClose(item);
                        }
                    }
                }
                #endregion

                #region Force Sell
                var num = pos.Data.Count() - index;
                if (num <= 0)
                    return;
                //Force Sell - Khi trong 1 khoảng thời gian ngắn có một loạt các lệnh thanh lý ngược chiều vị thế
                var timeForce = (int)DateTimeOffset.Now.AddMinutes(-15).ToUnixTimeSeconds();
                var lForce = _tradingRepo.GetByFilter(Builders<Trading>.Filter.Gte(x => x.d, timeForce));
                var countForceSell = lForce.Count(x => x.Side == (int)OrderSide.Sell);
                var countForceBuy = lForce.Count(x => x.Side == (int)OrderSide.Buy);
                if (countForceSell >= _forceSell)
                {
                    var lSell = pos.Data.Where(x => x.PositionAmt > 0);
                    await ForceMarket(lSell);
                    await _teleService.SendMessage(_idUser, $"Thanh lý lệnh LONG hàng loạt| {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}");
                }
                if (countForceBuy >= _forceSell)
                {
                    var lBuy = pos.Data.Where(x => x.PositionAmt < 0);
                    await ForceMarket(lBuy);
                    await _teleService.SendMessage(_idUser, $"Thanh lý lệnh SHORT hàng loạt| {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}");
                } 
                #endregion
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.Binance_TradeRSI|EXCEPTION| {ex.Message}");
            }
        }

        private async Task ForceMarket(IEnumerable<BinancePositionV3> lData)
        {
            foreach (var item in lData)
            {
                try
                {
                    await PlaceOrderClose(item);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.ForceMarket|EXCEPTION| {ex.Message}");
                }
            }
        }

        public async Task<SignalBase> PlaceOrder(SignalBase entity, decimal lastPrice)
        {
            try
            {
                var curTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                var account = await Binance_GetAccountInfo();
                if (account == null)
                {
                    await _teleService.SendMessage(_idUser, "[ERROR_binance] Không lấy được thông tin tài khoản");
                    return null;
                }

                if (account.WalletBalance * _margin <= _unit)
                    return null;

                var marginType = await StaticVal.BinanceInstance().UsdFuturesApi.Account.ChangeMarginTypeAsync(entity.s, FuturesMarginType.Isolated);
                if (!marginType.Success)
                    return null;

                var eMargin = StaticVal._dicBinanceMargin.FirstOrDefault(x => x.Key == entity.s);
                int margin = (int)_margin;
                if(eMargin.Key != null && eMargin.Value < (int)_margin)
                {
                    margin = eMargin.Value;
                }

                var initLevel = await StaticVal.BinanceInstance().UsdFuturesApi.Account.ChangeInitialLeverageAsync(entity.s, margin);
                if (!initLevel.Success)
                    return null;

                var side = (OrderSide)entity.Side;
                var SL_side = side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;

                var pos = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.GetPositionsAsync();
                if (pos.Data.Count() >= 3)
                    return null;

                var near = 2;
                if (lastPrice < 5)
                {
                    near = 0;
                }
                var exists = StaticVal._dicCoinAnk.FirstOrDefault(x => x.Key == entity.s);
                if(exists.Key != null)
                {
                    near = exists.Value.Item1;
                }

                var soluong = Math.Round(_unit / lastPrice, near);
                var res = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.PlaceOrderAsync(entity.s,
                                                                                                    side: side,
                                                                                                    type: FuturesOrderType.Market,
                                                                                                    positionSide: PositionSide.Both,
                                                                                                    reduceOnly: false,
                                                                                                    quantity: soluong);
                Thread.Sleep(500);
                //nếu lỗi return
                if (!res.Success)
                {
                    var mes = $"[ERROR_binance] {entity.s}|{side}|AMOUNT: {soluong}| {res.Error?.Message}";
                    _errRepo.InsertOne(new ErrorPartner
                    {
                        s = entity.s,
                        time = curTime,
                        des = mes
                    });
                    Console.WriteLine(mes);
                    await _teleService.SendMessage(_idUser, mes);
                    return null;
                }

                var resPosition = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.GetPositionsAsync(entity.s);
                Thread.Sleep(500);
                if (!resPosition.Success)
                {
                    var mes = $"[ERROR_binance] {entity.s}|Error when get Position| {res.Error?.Message}";
                    _errRepo.InsertOne(new ErrorPartner
                    {
                        s = entity.s,
                        time = curTime,
                        des = mes
                    });
                    Console.WriteLine(mes);
                    await _teleService.SendMessage(_idUser, mes);
                    return entity;
                }

                if (!resPosition.Data.Any())
                    return null;


                var first = resPosition.Data.First();
                entity.priceEntry = (double)first.MarkPrice;

                if (lastPrice < 5)
                {
                    var price = lastPrice.ToString().Split('.').Last();
                    price = price.ReverseString();
                    near = long.Parse(price).ToString().Length;
                }
                if (exists.Key != null)
                {
                    near = exists.Value.Item2;
                }
                var checkLenght = lastPrice.ToString().Split('.').Last();
                decimal sl = 0;
                if (side == OrderSide.Buy)
                {
                    sl = Math.Round(first.MarkPrice * (1 - _SL_RATE), near);
                }
                else
                {
                    sl = Math.Round(first.MarkPrice * (1 + _SL_RATE), near);
                }

                res = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.PlaceOrderAsync(first.Symbol,
                                                                                                side: SL_side,
                                                                                                type: FuturesOrderType.StopMarket,
                                                                                                positionSide: PositionSide.Both,
                                                                                                quantity: soluong,
                                                                                                timeInForce: TimeInForce.GoodTillExpiredOrCanceled,
                                                                                                reduceOnly: true,
                                                                                                workingType: WorkingType.Mark,
                                                                                                stopPrice: sl);
                Thread.Sleep(500);
                if (!res.Success)
                {
                    var mes = $"[ERROR_binance_SL] {entity.s}|{SL_side}|AMOUNT: {soluong}|Entry: {entity.priceEntry}|SL: {sl}| {res.Error?.Message}";
                    _errRepo.InsertOne(new ErrorPartner
                    {
                        s = entity.s,
                        time = curTime,
                        des = mes
                    });
                    Console.WriteLine(mes);
                    await _teleService.SendMessage(_idUser, mes);
                    return null;
                }

                entity.timeStoploss = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                entity.priceStoploss = (double)sl;
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.PlaceOrder|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private async Task<bool> PlaceOrderClose(BinancePositionV3 pos)
        {
            var CLOSE_side = pos.PositionAmt < 0 ? OrderSide.Buy : OrderSide.Sell;
            try
            {
                var res = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.PlaceOrderAsync(pos.Symbol,
                                                                                                    side: CLOSE_side,
                                                                                                    type: FuturesOrderType.Market,
                                                                                                    positionSide: PositionSide.Both,
                                                                                                    reduceOnly: false,
                                                                                                    quantity: Math.Abs(pos.PositionAmt));
                Thread.Sleep(500);
                if (res.Success)
                {
                    var rate = Math.Round(100 * (-1 + pos.MarkPrice / pos.EntryPrice), 1);
                    var winloss = "LOSS";
                    if (CLOSE_side == OrderSide.Buy && rate > 0)
                    {
                        winloss = "WIN";
                        rate = Math.Abs(rate);
                    }
                    else if (CLOSE_side == OrderSide.Sell && rate < 0)
                    {
                        winloss = "WIN";
                        rate = Math.Abs(rate);
                    }
                    else
                    {
                        rate = -Math.Abs(rate);
                    }

                    await _teleService.SendMessage(_idUser, $"[CLOSE - {CLOSE_side.ToString().ToUpper()}({winloss}: {rate}%)|Bybit] {pos.Symbol}| {pos.MarkPrice}");
                    return true;
                }
                else
                {
                    var mes = $"[Binance] {pos.Symbol}|{CLOSE_side}|AMOUNT: {Math.Abs(pos.PositionAmt)}|Error Không thể đóng lệnh| {res.Error?.Message}";
                    await _teleService.SendMessage(_idUser, mes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.PlaceOrderClose|EXCEPTION| {ex.Message}");
            }
            return false;
        }
    }
}
