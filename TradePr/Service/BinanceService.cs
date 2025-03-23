using Binance.Net.Objects.Models.Futures;
using MongoDB.Driver;
using Skender.Stock.Indicators;
using TradePr.DAL.Entity;
using TradePr.DAL;
using TradePr.Utils;
using SharpCompress.Common;

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
                    if (trade.Side == (int)Binance.Net.Enums.OrderSide.Buy)
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

                        model.Side = (int)Binance.Net.Enums.OrderSide.Buy;
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

                        model.Side = (int)Binance.Net.Enums.OrderSide.Sell;
                        model.Entry = (double)cur.High;
                    }

                    if(model.Entry > 0)
                    {
                        _prepareRepo.InsertOne(model);
                        Monitor.Enter(_locker);
                        StaticVal._lPrepare.Add(model);
                        Monitor.Exit(_locker);
                        //await _teleService.SendMessage(_idUser, $"[PREPARE-Liquid] {model.s}|{(Binance.Net.Enums.OrderSide)model.Side}|ENTRY: {model.Entry}| Time: {(int)DateTimeOffset.Now.ToUnixTimeSeconds()}");
                        Console.WriteLine($"[PREPARE-Liquid] {model.s}|{(Binance.Net.Enums.OrderSide)model.Side}|ENTRY: {model.Entry}| Time: {(int)DateTimeOffset.Now.ToUnixTimeSeconds()}");
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

                        model.Side = (int)Binance.Net.Enums.OrderSide.Buy;
                        model.Entry = (double)low;
                    }
                    else if(rsi.Rsi >= 75 && rsi.Rsi < 80)
                    {
                        if (!StaticVal._lRsiShort.Contains(sym))
                            continue;
                        if (lRsi.SkipLast(2).TakeLast(5).Any(x => x.Rsi is null || x.Rsi > 80))
                            continue;

                        model.Side = (int)Binance.Net.Enums.OrderSide.Sell;
                        model.Entry = (double)cur.High;
                    }

                    if (model.Entry > 0)
                    {
                        _prepareRepo.InsertOne(model);
                        Monitor.Enter(_locker);
                        StaticVal._lPrepare.Add(model);
                        Monitor.Exit(_locker);
                        //await _teleService.SendMessage(_idUser, $"[PREPARE-RSI] {model.s}|{(Binance.Net.Enums.OrderSide)model.Side}|ENTRY: {model.Entry}| Time: {(int)DateTimeOffset.Now.ToUnixTimeSeconds()}");
                        Console.WriteLine($"[PREPARE-RSI] {model.s}|{(Binance.Net.Enums.OrderSide)model.Side}|ENTRY: {model.Entry}| Time: {(int)DateTimeOffset.Now.ToUnixTimeSeconds()}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.Binance_TradeRSI|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Binance_TakeProfit(DateTime dt)
        {
            try
            {
                var timeEnd = (int)DateTimeOffset.Now.AddHours(-2).ToUnixTimeSeconds();
                var builder = Builders<PrepareTrade>.Filter;
                var lViThe = _prepareRepo.GetByFilter(builder.And(
                    builder.Lte(x => x.entryTime, timeEnd),
                    builder.Eq(x => x.Status, 1)
                ));
                var index = 0;
                var pos = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.GetPositionsAsync();
                #region Sell
                foreach (var item in pos.Data)
                {
                    var side = item.PositionAmt < 0 ? Binance.Net.Enums.OrderSide.Sell : Binance.Net.Enums.OrderSide.Buy;
                    var SL_side = item.PositionAmt < 0 ? Binance.Net.Enums.OrderSide.Buy : Binance.Net.Enums.OrderSide.Sell;

                    var vithe = lViThe.FirstOrDefault(x => x.s == item.Symbol && x.Side == (int)side);
                    var curTime = (DateTime.UtcNow - item.UpdateTime.Value).TotalHours;
                    if(curTime >= 2 || (vithe != null && (DateTime.UtcNow - vithe.entryDate).TotalHours >= 2))
                    {
                        index++;
                        await PlaceOrderClose(item.Symbol, Math.Abs(item.PositionAmt), SL_side);
                        
                        if (vithe != null)
                        {
                            vithe.stopDate = DateTime.Now;
                            vithe.stopTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                            vithe.SL_Real = (double)item.MarkPrice;
                            vithe.Status = 2;
                            _prepareRepo.Update(vithe);
                            var rate = Math.Round(100 * (-1 + vithe.SL_Real / vithe.Entry), 1);
                            var winloss = "LOSS";
                            if(side == Binance.Net.Enums.OrderSide.Buy && rate > 0)
                            {
                                winloss = "WIN";
                                rate = Math.Abs(rate);
                            }
                            else if(side == Binance.Net.Enums.OrderSide.Sell && rate < 0)
                            {
                                winloss = "WIN";
                                rate = Math.Abs(rate);
                            }
                            else
                            {
                                rate = - Math.Abs(rate);
                            }

                            await _teleService.SendMessage(_idUser, $"[CLOSE-{side.ToString().ToUpper()}({winloss}({rate}%))] {item.Symbol}|ENTRY: {item.MarkPrice}|CLOSE: {vithe.SL_Real}");
                        }
                    }
                }
                #endregion

                #region Force Sell
                var num = pos.Data.Count() - index;
                //Force Sell - Khi trong 1 khoảng thời gian ngắn có một loạt các lệnh thanh lý ngược chiều vị thế
                if (num <= 0)
                    return;
                var timeForce = (int)DateTimeOffset.Now.AddMinutes(-15).ToUnixTimeSeconds();
                var lForce = _tradingRepo.GetByFilter(Builders<Trading>.Filter.Gte(x => x.d, timeForce));
                var countForceSell = lForce.Count(x => x.Side == (int)Binance.Net.Enums.OrderSide.Sell);
                var countForceBuy = lForce.Count(x => x.Side == (int)Binance.Net.Enums.OrderSide.Buy);
                if (countForceSell >= _forceSell)
                {
                    var lSell = pos.Data.Where(x => x.PositionAmt < 0);
                    await ForceMarket(lSell);
                    await _teleService.SendMessage(_idUser, $"Thanh lý lệnh SHORT hàng loạt| {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}");
                }
                if (countForceBuy >= _forceSell)
                {
                    var lBuy = pos.Data.Where(x => x.PositionAmt > 0);
                    await ForceMarket(lBuy);
                    await _teleService.SendMessage(_idUser, $"Thanh lý lệnh LONG hàng loạt| {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}");
                } 
                #endregion
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.Binance_TradeRSI|EXCEPTION| {ex.Message}");
            }
        }

        private async Task<List<BinancePositionV3>> ForceMarket(IEnumerable<BinancePositionV3> lData)
        {
            var lRes = new List<BinancePositionV3>();
            try
            {
                foreach (var item in lData)
                {
                    var SL_Side = (item.PositionAmt < 0) ? Binance.Net.Enums.OrderSide.Buy : Binance.Net.Enums.OrderSide.Sell;
                    var pos = (item.PositionAmt < 0) ? Binance.Net.Enums.PositionSide.Short : Binance.Net.Enums.PositionSide.Long;
                    item.PositionSide = pos;
                    var res = await PlaceOrderClose(item.Symbol, Math.Abs(item.PositionAmt), SL_Side);
                    if (!res)
                        continue;

                    lRes.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.ForceMarket|EXCEPTION| {ex.Message}");
            }

            return lRes;
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

                var marginType = await StaticVal.BinanceInstance().UsdFuturesApi.Account.ChangeMarginTypeAsync(entity.s, Binance.Net.Enums.FuturesMarginType.Isolated);
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

                var side = (Binance.Net.Enums.OrderSide)entity.Side;
                var SL_side = side == Binance.Net.Enums.OrderSide.Buy ? Binance.Net.Enums.OrderSide.Sell : Binance.Net.Enums.OrderSide.Buy;

                var pos = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.GetPositionsAsync();
                if (pos.Data.Any())
                {
                    var index = 0;
                    foreach (var item in pos.Data)
                    {
                        if(item.Symbol == entity.s)
                        {
                            if (item.PositionAmt < 0 && side == Binance.Net.Enums.OrderSide.Sell)
                            {
                                return null;
                            }
                            else if(item.PositionAmt > 0 && side == Binance.Net.Enums.OrderSide.Buy)
                            {
                                return null;
                            }
                            else
                            {
                                index++;
                                await PlaceOrderClose(entity.s, Math.Abs(item.PositionAmt), SL_side);
                            }
                        }
                    }
                   
                    var num = pos.Data.Count() - index;
                    if (num >= 3)
                        return null;
                }

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
                                                                                                    type: Binance.Net.Enums.FuturesOrderType.Market,
                                                                                                    positionSide: Binance.Net.Enums.PositionSide.Both,
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
                if (side == Binance.Net.Enums.OrderSide.Buy)
                {
                    sl = Math.Round(first.MarkPrice * (1 - _SL_RATE), near);
                }
                else
                {
                    sl = Math.Round(first.MarkPrice * (1 + _SL_RATE), near);
                }

                res = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.PlaceOrderAsync(first.Symbol,
                                                                                                side: SL_side,
                                                                                                type: Binance.Net.Enums.FuturesOrderType.StopMarket,
                                                                                                positionSide: Binance.Net.Enums.PositionSide.Both,
                                                                                                quantity: soluong,
                                                                                                timeInForce: Binance.Net.Enums.TimeInForce.GoodTillExpiredOrCanceled,
                                                                                                reduceOnly: true,
                                                                                                workingType: Binance.Net.Enums.WorkingType.Mark,
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

        private async Task<bool> PlaceOrderClose(string symbol, decimal quan, Binance.Net.Enums.OrderSide side)
        {
            try
            {
                var res = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.PlaceOrderAsync(symbol,
                                                                                                    side: side,
                                                                                                    type: Binance.Net.Enums.FuturesOrderType.Market,
                                                                                                    positionSide: Binance.Net.Enums.PositionSide.Both,
                                                                                                    reduceOnly: false,
                                                                                                    quantity: quan);
                Thread.Sleep(500);
                if (res.Success)
                {
                    return true;
                }
                else
                {
                    var mes = $"[ERROR_binance_Close] {symbol}|{side}|AMOUNT: {quan}|Error Không thể đóng lệnh| {res.Error?.Message}";
                    Console.WriteLine(mes);
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
