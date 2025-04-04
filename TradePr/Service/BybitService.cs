using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;
using MongoDB.Driver;
using Skender.Stock.Indicators;
using System.Text;
using TradePr.DAL;
using TradePr.DAL.Entity;
using TradePr.Utils;

namespace TradePr.Service
{
    public interface IBybitService
    {
        Task<BybitAssetBalance> Bybit_GetAccountInfo();
        Task Bybit_Trade();
    }
    public class BybitService : IBybitService
    {
        private readonly ILogger<BybitService> _logger;
        private readonly ITradingRepo _tradingRepo;
        private readonly ISignalTradeRepo _signalTradeRepo;
        private readonly IThreeSignalTradeRepo _threeSignalTradeRepo;
        private readonly ITokenUnlockTradeRepo _tokenUnlockTradeRepo;
        private readonly ITokenUnlockRepo _tokenUnlockRepo;
        private readonly IMa20TradeRepo _maRepo;
        private readonly IErrorPartnerRepo _errRepo;
        private readonly IConfigDataRepo _configRepo;
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private const long _idUser = 1066022551;
        private const decimal _unit = 50;
        private const decimal _margin = 10;
        private readonly int _exchange = (int)EExchange.Bybit;
        private readonly int _forceSell = 4;
        public BybitService(ILogger<BybitService> logger, ITradingRepo tradingRepo, ISignalTradeRepo signalTradeRepo, IErrorPartnerRepo errRepo,
                            IConfigDataRepo configRepo, IThreeSignalTradeRepo threeSignalTradeRepo, ITokenUnlockTradeRepo tokenUnlockTradeRepo, ITokenUnlockRepo tokenUnlockRepo,
                            IMa20TradeRepo maRepo,
                            IAPIService apiService, ITeleService teleService)
        {
            _logger = logger;
            _tradingRepo = tradingRepo;
            _signalTradeRepo = signalTradeRepo;
            _errRepo = errRepo;
            _configRepo = configRepo;
            _threeSignalTradeRepo = threeSignalTradeRepo;
            _tokenUnlockTradeRepo = tokenUnlockTradeRepo;
            _tokenUnlockRepo = tokenUnlockRepo;
            _apiService = apiService;
            _teleService = teleService;
            _maRepo = maRepo;
        }
        public async Task<BybitAssetBalance> Bybit_GetAccountInfo()
        {
            try
            {
                var resAPI = await StaticVal.ByBitInstance().V5Api.Account.GetBalancesAsync( AccountType.Unified);
                return resAPI?.Data?.List?.FirstOrDefault().Assets.FirstOrDefault(x => x.Asset == "USDT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_GetAccountInfo|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task Bybit_Trade()
        {
            try
            {
                var builder = Builders<ConfigData>.Filter;
                var lSignal = _configRepo.GetByFilter(builder.And(
                    builder.Eq(x => x.ex, _exchange),
                    builder.Eq(x => x.status, 1)
                ));
                var dt = DateTime.Now;

                await Bybit_TakeProfit();
                await Bybit_TradeMA20(dt);
                //if(lSignal.Any(x => x.op == (int)EOption.Signal))
                //{
                //    await Bybit_TradeSignal();
                //}
                if (lSignal.Any(x => x.op == (int)EOption.Unlock))
                {
                    await Bybit_TradeTokenUnlock();
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_Trade|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Bybit_TradeSignal()
        {
            try
            {
                var time = (int)DateTimeOffset.Now.AddMinutes(-60).ToUnixTimeSeconds();
                var lTrade = _tradingRepo.GetByFilter(Builders<Trading>.Filter.Gte(x => x.d, time));
                if (!(lTrade?.Any() ?? false))
                    return;

                var builder = Builders<SignalTrade>.Filter;
                var lSignal = _signalTradeRepo.GetByFilter(builder.And(
                    builder.Eq(x => x.ex, _exchange),
                    builder.Gte(x => x.timeFlag, time)
                ));
                var lSym = lTrade.Select(x => x.s).Distinct();
                foreach (var item in lSym)
                {
                    if (StaticVal._lIgnoreThreeSignal.Contains(item))
                        continue;

                    //Trong 1 tiếng tồn tại ít nhất 2 tín hiệu
                    var lTradeSym = lTrade.Where(x => x.s == item).OrderByDescending(x => x.d);
                    if (lTradeSym.Count() < 2)
                        continue;                  

                    //2 tín hiệu cùng chiều
                    var first = lTradeSym.First();
                    var second = lTradeSym.FirstOrDefault(x => x.Date < first.Date && x.Side == first.Side);
                    if (second is null)
                        continue;

                    if (lSignal.Any(x => x.s == item && x.Side == first.Side))
                        continue;

                    var divTime = (first.Date - second.Date).TotalMinutes;
                    if (divTime > 15)
                        continue;

                    //gia
                    var lData15m = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetMarkPriceKlinesAsync(Category.Linear, $"{item}USDT", KlineInterval.FifteenMinutes);
                    Thread.Sleep(100);
                    if (lData15m.Data is null
                        || !lData15m.Data.List.Any())
                        continue;

                    var last = lData15m.Data.List.Reverse().Last();
                    var itemCheck = lData15m.Data.List.Reverse().SkipLast(1).Last();
                    if (itemCheck.OpenPrice >= itemCheck.ClosePrice)
                        continue;

                    //Trade
                    var res = await PlaceOrder(new SignalBase
                    {
                        s = item,
                        ex = _exchange,
                        Side = first.Side,
                        timeFlag = (int)DateTimeOffset.Now.ToUnixTimeSeconds()
                    }, last.ClosePrice);

                    if(res != null)
                    {
                        _signalTradeRepo.InsertOne(new SignalTrade
                        {
                            s = res.s,
                            ex = res.ex,
                            Side = res.Side,
                            timeFlag = res.timeFlag,
                            timeStoploss = res.timeStoploss,
                            priceEntry = res.priceEntry,
                            priceStoploss = res.priceStoploss,
                        });
                        var mes = $"[SIGNAL_bybit][Mở vị thế {((OrderSide)res.Side).ToString().ToUpper()}] {res.s}|Giá mở: {res.priceEntry}|SL: {res.priceStoploss}";
                        await _teleService.SendMessage(_idUser, mes);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TradeSignal|EXCEPTION| {ex.Message}");
            }
            return;
        }

        private async Task Bybit_TradeTokenUnlock()
        {
            try
            {
                var dt = DateTime.UtcNow;
                //if (true)
                if (dt.Hour == 23 && dt.Minute == 58)
                {
                    var sBuilder = new StringBuilder();
                    #region Đóng vị thế
                    var resPosition = await StaticVal.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, settleAsset: "USDT");
                    if (resPosition?.Data?.List?.Any()?? false)
                    {
                        //close all
                        foreach (var position in resPosition.Data.List)
                        {
                            var time = (int)new DateTimeOffset(dt.Year, dt.Month, dt.Day, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
                            var builder = Builders<TokenUnlockTrade>.Filter;
                            var first = _tokenUnlockTradeRepo.GetEntityByFilter(builder.And(
                                builder.Eq(x => x.ex, _exchange),
                                builder.Eq(x => x.s, position.Symbol),
                                builder.Eq(x => x.timeUnlock, time)
                            ));

                            if (first is null)
                                continue;

                            var res = await PlaceOrderClose(position.Symbol, Math.Abs(position.Quantity), OrderSide.Buy);
                            if (res)
                            {
                                first.rate = Math.Round(100 * (-1 + first.priceEntry / (double)position.MarkPrice.Value), 1);
                                first.timeClose = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                                _tokenUnlockTradeRepo.Update(first);
                                var mes = $"[CLOSE - SELL({first.rate}%)|UNLOCK_bybit] {position.Symbol}";
                                sBuilder.AppendLine(mes);
                            }
                        }
                    }
                    #endregion

                    #region Mở vị thế
                    var timeUnlock = (int)new DateTimeOffset(dt.Year, dt.Month, dt.Day, 0, 0, 0, TimeSpan.Zero).AddDays(1).ToUnixTimeSeconds();
                    var tokens = _tokenUnlockRepo.GetByFilter(Builders<TokenUnlock>.Filter.Eq(x => x.time, timeUnlock)).Where(x => !StaticVal._lTokenUnlockBlackList.Contains(x.s));
                    if (!tokens.Any())
                        return;

                    //action  
                    foreach (var item in tokens)
                    {
                        var builder = Builders<TokenUnlockTrade>.Filter;
                        var entityCheck = _tokenUnlockTradeRepo.GetEntityByFilter(builder.And(
                                    builder.Eq(x => x.ex, _exchange),
                                    builder.Eq(x => x.timeUnlock, item.time),
                                    builder.Eq(x => x.s, $"{item.s}USDT")
                                )); 
                        if (entityCheck != null)
                            continue;

                        var lData15m = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetMarkPriceKlinesAsync(Category.Linear, $"{item.s}USDT", KlineInterval.FifteenMinutes);
                        Thread.Sleep(100);
                        if (lData15m.Data is null
                            || !lData15m.Data.List.Any())
                            continue;

                        var last = lData15m.Data.List.Reverse().Last();
                        var res = await PlaceOrder(new SignalBase
                        {
                            s = $"{item.s}USDT",
                            ex = _exchange,
                            Side = (int)OrderSide.Sell,
                            timeFlag = (int)DateTimeOffset.Now.ToUnixTimeSeconds()
                        }, last.ClosePrice);
                        if (res != null)
                        {
                            _tokenUnlockTradeRepo.InsertOne(new TokenUnlockTrade
                            {
                                s = res.s,
                                ex = res.ex,
                                timeUnlock = item.time,
                                timeShort = (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                                timeStoploss = res.timeStoploss,
                                priceEntry = res.priceEntry,
                                priceStoploss = res.priceStoploss
                            });

                            var first = StaticVal._dicCoinAnk.First(x => x.Key == res.s);
                            var mes = $"[ACTION - SELL|UNLOCK_bybit] {res.s}|ENTRY: {Math.Round(res.priceEntry, first.Value.Item2)}";
                            sBuilder.AppendLine(mes);
                        }
                    }
                    #endregion

                    if (sBuilder.Length > 0)
                    {
                        await _teleService.SendMessage(_idUser, sBuilder.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TradeTokenUnlock|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Bybit_TradeMA20(DateTime dt)
        {
            try
            {
                if (dt.Minute % 15 != 0)
                    return;
                var pos = await StaticVal.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, settleAsset: "USDT");
                var time = (int)DateTimeOffset.Now.AddMinutes(-60).ToUnixTimeSeconds();
                var lSym = StaticVal._lMa20Short_Bybit.Concat(StaticVal._lMa20_Bybit);
                foreach (var item in lSym)
                {
                    try
                    {
                        if (pos.Data.List.Any(x => x.Symbol == item))
                            continue;
                        //gia
                        var l15m = await _apiService.GetData_Bybit(item, EInterval.M15);
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
                        if (StaticVal._lMa20Short_Bybit.Any(x => x == item))
                        {
                            ShortAction();
                        }

                        //Long
                        if (StaticVal._lMa20_Bybit.Any(x => x == item))
                        {
                            LongAction();
                        }

                        if(sideDetect > -1)
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

                                _maRepo.InsertOne(new Ma20Trade
                                {
                                    ex = _exchange,
                                    s = res.s,
                                    Side = res.Side,
                                    priceEntry = price,
                                    timeFlag = res.timeFlag,
                                    dateFlag = DateTime.Now,
                                    timeStoploss = res.timeStoploss,
                                    priceStoploss = res.priceStoploss,
                                });

                                var mes = $"[ACTION - {side}|BB_bybit] {res.s}|ENTRY: {price}";
                                await _teleService.SendMessage(_idUser, mes);
                            }
                        }

                        void ShortAction()
                        {
                            if ( cur.Open <= cur.Close
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
                        _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TradeMA20|EXCEPTION|{item}| {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TradeMA20|EXCEPTION| {ex.Message}");
            }
            return;
        }

        private async Task Bybit_TakeProfit()
        {
            try
            {
                var pos = await StaticVal.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, settleAsset: "USDT");
                if (!pos.Data.List.Any())
                    return;

                //Unlock 
                var dt = DateTime.UtcNow;
                var timeUnlock = (int)new DateTimeOffset(dt.Year, dt.Month, dt.Day, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
                var builderUnlock = Builders<TokenUnlockTrade>.Filter;
                var lUnlock = _tokenUnlockTradeRepo.GetByFilter(builderUnlock.And(
                            builderUnlock.Eq(x => x.ex, _exchange),
                            builderUnlock.Eq(x => x.timeUnlock, timeUnlock)
                        ));

                var timeEnd = (int)DateTimeOffset.Now.AddHours(-2).ToUnixTimeSeconds();
                var builder = Builders<Ma20Trade>.Filter;
                var lViThe = _maRepo.GetByFilter(builder.And(
                    builder.Eq(x => x.ex, _exchange),
                    builder.Gte(x => x.timeFlag, timeEnd)
                ));

                #region Sell
                foreach (var item in pos.Data.List)
                {
                    if (lUnlock.Any(x => x.s == item.Symbol))
                        continue;

                    var side = item.Side == PositionSide.Sell ? OrderSide.Sell : OrderSide.Buy;
                    var SL_side = item.Side == PositionSide.Sell ? OrderSide.Buy : OrderSide.Sell;

                    var vithe = lViThe.FirstOrDefault(x => x.s == item.Symbol && x.Side == (int)side);
                    var curTime = (dt - item.UpdateTime.Value).TotalHours;
                    if (curTime >= 2 || (vithe != null && (DateTime.Now - vithe.dateFlag).TotalHours >= 2))
                    {
                        await PlaceOrderClose(item.Symbol, Math.Abs(item.Quantity), SL_side);

                        if (vithe != null)
                        {
                            vithe.priceClose = (double)item.MarkPrice;
                            var rate = Math.Round(100 * (-1 + vithe.priceClose / vithe.priceEntry), 1);
                            var winloss = "LOSS";
                            if (side == OrderSide.Buy && rate > 0)
                            {
                                winloss = "WIN";
                                rate = Math.Abs(rate);
                            }
                            else if (side == OrderSide.Sell && rate < 0)
                            {
                                winloss = "WIN";
                                rate = Math.Abs(rate);
                            }
                            else
                            {
                                rate = -Math.Abs(rate);
                            }
                            vithe.rate = rate;
                            _maRepo.Update(vithe);
                            await _teleService.SendMessage(_idUser, $"[CLOSE - {side.ToString().ToUpper()}({winloss}: {rate}%)|BB_bybit] {item.Symbol}| {item.MarkPrice}");
                            continue;
                        }
                        await _teleService.SendMessage(_idUser, $"[CLOSE - {side.ToString().ToUpper()}|BB_bybit] {item.Symbol}|CLOSE: {item.MarkPrice}");
                    }
                    else
                    {
                        var l15m = await _apiService.GetData_Bybit(item.Symbol, EInterval.M15);
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
                        else if(side == OrderSide.Sell && cur.Close < (decimal)bb.LowerBand.Value)
                        {
                            flag = true;
                        }

                        if(flag)
                        {
                            await PlaceOrderClose(item.Symbol, Math.Abs(item.Quantity), SL_side);

                            if (vithe != null)
                            {
                                vithe.priceClose = (double)item.MarkPrice;
                                var rate = Math.Round(100 * (-1 + vithe.priceClose / vithe.priceEntry), 1);
                                var winloss = "LOSS";
                                if (side == OrderSide.Buy && rate > 0)
                                {
                                    winloss = "WIN";
                                    rate = Math.Abs(rate);
                                }
                                else if (side == OrderSide.Sell && rate < 0)
                                {
                                    winloss = "WIN";
                                    rate = Math.Abs(rate);
                                }
                                else
                                {
                                    rate = -Math.Abs(rate);
                                }
                                vithe.rate = rate;
                                _maRepo.Update(vithe);
                                await _teleService.SendMessage(_idUser, $"[CLOSE - {side.ToString().ToUpper()}({winloss}: {rate}%)|BB_bybit] {item.Symbol}");
                                continue;
                            }
                            await _teleService.SendMessage(_idUser, $"[CLOSE - {side.ToString().ToUpper()}|BB_bybit] {item.Symbol}|CLOSE: {item.MarkPrice}");
                        }
                    }
                }
                #endregion

                #region Force Sell
                //Force Sell - Khi trong 1 khoảng thời gian ngắn có một loạt các lệnh thanh lý ngược chiều vị thế
                var timeForce = (int)DateTimeOffset.Now.AddMinutes(-15).ToUnixTimeSeconds();
                var lForce = _tradingRepo.GetByFilter(Builders<Trading>.Filter.Gte(x => x.d, timeForce));
                var countForceSell = lForce.Count(x => x.Side == (int)OrderSide.Sell);
                var countForceBuy = lForce.Count(x => x.Side == (int)OrderSide.Buy);
                if (countForceSell >= _forceSell)
                {
                    var lSell = pos.Data.List.Where(x => x.Side == PositionSide.Buy);
                    await ForceMarket(lSell);
                    await _teleService.SendMessage(_idUser, $"Thanh lý lệnh LONG hàng loạt| {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}");
                }
                if (countForceBuy >= _forceSell)
                {
                    var lBuy = pos.Data.List.Where(x => x.Side == PositionSide.Sell);
                    await ForceMarket(lBuy);
                    await _teleService.SendMessage(_idUser, $"Thanh lý lệnh SHORT hàng loạt| {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}");
                } 
                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TakeProfit|EXCEPTION| {ex.Message}");
            }
        }

        private async Task<List<BybitPosition>> ForceMarket(IEnumerable<BybitPosition> lData)
        {
            var lRes = new List<BybitPosition>();
            try
            {
                foreach (var item in lData)
                {
                    var side = (item.Side == PositionSide.Buy) ? OrderSide.Sell : OrderSide.Buy;
                    var res = await PlaceOrderClose(item.Symbol, item.Quantity, side);
                    if (!res)
                        continue;

                    lRes.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.ForceMarket|EXCEPTION| {ex.Message}");
            }

            return lRes;
        }

        private async Task<SignalBase> PlaceOrder(SignalBase entity, decimal lastPrice)
        {
            try
            {
                var SL_RATE = 0.017;
                var curTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                var account = await Bybit_GetAccountInfo();
                if (account == null)
                {
                    await _teleService.SendMessage(_idUser, "[ERROR_bybit] Không lấy được thông tin tài khoản");
                    return null;
                }

                if (account.WalletBalance * _margin <= _unit)
                    return null;


                var side = (OrderSide)entity.Side;
                var SL_side = side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
                var direction = side == OrderSide.Buy ? TriggerDirection.Fall : TriggerDirection.Rise;

                var pos = await StaticVal.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, settleAsset: "USDT");
                if (pos.Data.List.Any())
                {
                    var index = 0;
                    foreach (var item in pos.Data.List)
                    {
                        if (item.Symbol == entity.s)
                        {
                            if (item.Side == PositionSide.Sell && side == OrderSide.Sell)
                            {
                                return null;
                            }
                            else if (item.Side == PositionSide.Buy && side == OrderSide.Buy)
                            {
                                return null;
                            }
                            else
                            {
                                index++;
                                await PlaceOrderClose(entity.s, Math.Abs(item.Quantity), SL_side);
                            }
                        }
                    }

                    var num = pos.Data.List.Count() - index;
                    if (num >= 3)
                        return null;
                }

                var near = 2; 
                if (lastPrice < 5)
                {
                    near = 0;
                }
                var exists = StaticVal._dicCoinAnk.FirstOrDefault(x => x.Key == entity.s);
                if (exists.Key != null)
                {
                    near = exists.Value.Item1;
                }

                var soluong = Math.Round(_unit / lastPrice, near);
                var res = await StaticVal.ByBitInstance().V5Api.Trading.PlaceOrderAsync(Category.Linear,
                                                                                        entity.s,
                                                                                        side: side,
                                                                                        type: NewOrderType.Market,
                                                                                        reduceOnly: false,
                                                                                        quantity: soluong);
                Thread.Sleep(500);
                //nếu lỗi return
                if (!res.Success)
                {
                    _errRepo.InsertOne(new ErrorPartner
                    {
                        s = entity.s,
                        time = curTime,
                        ty = (int)ETypeBot.TokenUnlock,
                        action = (int)EAction.Short,
                        des = $"side: {side}, type: {NewOrderType.Market}, quantity: {soluong}"
                    });
                    return null;
                }

                var resPosition = await StaticVal.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, entity.s);
                Thread.Sleep(500);
                if (!resPosition.Success)
                {
                    _errRepo.InsertOne(new ErrorPartner
                    {
                        s = entity.s,
                        time = curTime,
                        ty = (int)ETypeBot.TokenUnlock,
                        action = (int)EAction.GetPosition
                    });
                    Console.WriteLine($"[ERROR_bybit] |{entity.s}|Soluong: {soluong}");
                    await _teleService.SendMessage(_idUser, $"[ERROR_bybit] |{entity.s}|{res.Error.Code}:{res.Error.Message}");
                    return entity;
                }

                if (resPosition.Data.List.Any())
                {
                    var first = resPosition.Data.List.First();
                    entity.priceEntry = (double)first.MarkPrice;

                    if (lastPrice < 5)
                    {
                        var price = lastPrice.ToString().Split('.').Last();
                        price = price.ReverseString();
                        near = long.Parse(price).ToString().Length;
                        if (exists.Key != null)
                        {
                            near = exists.Value.Item2;
                        }
                    }
                    var checkLenght = lastPrice.ToString().Split('.').Last();
                    decimal sl = 0;
                    if (side == OrderSide.Buy)
                    {
                        sl = Math.Round(first.MarkPrice.Value * (decimal)(1 - SL_RATE), near);
                    }
                    else
                    {
                        sl = Math.Round(first.MarkPrice.Value * (decimal)(1 + SL_RATE), near);
                    }
                    res = await StaticVal.ByBitInstance().V5Api.Trading.PlaceOrderAsync(Category.Linear,
                                                                                            first.Symbol,
                                                                                            side: SL_side,
                                                                                            type: NewOrderType.Market,
                                                                                            triggerPrice: sl,
                                                                                            triggerDirection: direction,
                                                                                            triggerBy: TriggerType.LastPrice,
                                                                                            quantity: soluong,
                                                                                            timeInForce: TimeInForce.GoodTillCanceled,
                                                                                            reduceOnly: true,
                                                                                            stopLossOrderType: OrderType.Limit,
                                                                                            stopLossTakeProfitMode: StopLossTakeProfitMode.Partial,
                                                                                            stopLossTriggerBy: TriggerType.LastPrice,
                                                                                            stopLossLimitPrice: sl);
                    Thread.Sleep(500);
                    if (!res.Success)
                    {
                        _errRepo.InsertOne(new ErrorPartner
                        {
                            s = entity.s,
                            time = curTime,
                            ty = (int)ETypeBot.TokenUnlock,
                            action = (int)EAction.Short_SL,
                            des = $"side: {SL_side}, type: {NewOrderType.Market}, quantity: {soluong}, stopPrice: {sl}"
                        });
                        Console.WriteLine($"[ERROR_bybit_SL] |{entity.s}|Soluong: {soluong}");
                        await _teleService.SendMessage(_idUser, $"[ERROR_bybit_SL] |{entity.s}|{res.Error.Code}:{res.Error.Message}");
                        return null;
                    }

                    entity.timeStoploss = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                    entity.priceStoploss = (double)sl;
                }
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.PlaceOrder|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private async Task<bool> PlaceOrderClose(string symbol, decimal quan, OrderSide side)
        {
            try
            {
                var res = await StaticVal.ByBitInstance().V5Api.Trading.PlaceOrderAsync(Category.Linear,
                                                                                        symbol,
                                                                                        side: side,
                                                                                        type: NewOrderType.Market,
                                                                                        quantity: quan);
                if (res.Success)
                {
                    var resCancel = await StaticVal.ByBitInstance().V5Api.Trading.CancelAllOrderAsync(Category.Linear, symbol);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.PlaceOrderClose|EXCEPTION| {ex.Message}");
            }
            Console.WriteLine($"[ERROR_bybit_Close] |{symbol}|Soluong: {quan}");
            await _teleService.SendMessage(_idUser, $"[ERROR_bybit_Close] Không thể đóng lệnh {side}: {symbol}!");
            return false;
        }
    }
}
