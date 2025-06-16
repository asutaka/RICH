using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;
using CoinUtilsPr;
using CoinUtilsPr.DAL;
using CoinUtilsPr.DAL.Entity;
using MongoDB.Driver;
using Newtonsoft.Json;
using Skender.Stock.Indicators;
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
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private readonly IPlaceOrderTradeRepo _placeRepo;
        private readonly ISymbolRepo _symRepo;
        private readonly IConfigDataRepo _configRepo;
        private readonly IPrepareRepo _prepareRepo;
        private const long _idUser = 1066022551;
        private const decimal _margin = 10;
        private readonly int _HOUR = 4;//MA20 là 2h
        private readonly decimal _SL_RATE = 0.025m; //MA20 là 0.017
        private readonly decimal _TP_RATE_MIN = 0.025m;
        private readonly decimal _TP_RATE_MAX = 0.07m;
        private readonly int _exchange = (int)EExchange.Bybit;
        private Dictionary<string, List<Quote>> _dData = new Dictionary<string, List<Quote>>();

        public BybitService(ILogger<BybitService> logger,
                            IAPIService apiService, ITeleService teleService, IPlaceOrderTradeRepo placeRepo, ISymbolRepo symRepo,
                            IConfigDataRepo configRepo, IPrepareRepo prepareRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _teleService = teleService;
            _placeRepo = placeRepo;
            _symRepo = symRepo;
            _configRepo = configRepo;
            _prepareRepo = prepareRepo;
        }
        public async Task<BybitAssetBalance> Bybit_GetAccountInfo()
        {
            try
            {
                var resAPI = await StaticTrade.ByBitInstance().V5Api.Account.GetBalancesAsync( AccountType.Unified);
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
                var dt = DateTime.Now;

                await Bybit_TakeProfit();
                var lConfig = _configRepo.GetAll();
                var disableAll = lConfig.FirstOrDefault(x => x.ex == _exchange && x.op == (int)EOption.DisableAll && x.status == 1);
                var disableLong = lConfig.FirstOrDefault(x => x.ex == _exchange && x.op == (int)EOption.DisableLong && x.status == 1);
                var disableShort = lConfig.FirstOrDefault(x => x.ex == _exchange && x.op == (int)EOption.DisableShort && x.status == 1);

                var flagLong = disableAll != null || disableLong != null;
                var flagShort = disableAll != null || disableShort != null;

                if(flagLong)
                {
                    var builder = Builders<Prepare>.Filter;
                    _prepareRepo.DeleteMany(builder.And(
                        builder.Eq(x => x.ex, _exchange),
                        builder.Eq(x => x.side, (int)OrderSide.Buy)
                    ));
                }

                if (flagShort)
                {
                    var builder = Builders<Prepare>.Filter;
                    _prepareRepo.DeleteMany(builder.And(
                        builder.Eq(x => x.ex, _exchange),
                        builder.Eq(x => x.side, (int)OrderSide.Sell)
                    ));
                }

                await Entry_LONG();
                await Entry_SHORT();

                if (dt.Minute % 15 == 0)
                {
                    if (!flagLong)
                    {
                        var builderLONG = Builders<Symbol>.Filter;
                        var lLong = _symRepo.GetByFilter(builderLONG.And(
                            builderLONG.Eq(x => x.ex, _exchange),
                            builderLONG.Eq(x => x.ty, (int)OrderSide.Buy),
                            builderLONG.Eq(x => x.status, 0)
                        )).OrderBy(x => x.rank).ToList();
                        await Bybit_TradeRSI_LONG(lLong);
                    }

                    if(!flagShort)
                    {
                        var builderSHORT = Builders<Symbol>.Filter;
                        var lShort = _symRepo.GetByFilter(builderSHORT.And(
                            builderSHORT.Eq(x => x.ex, _exchange),
                            builderSHORT.Eq(x => x.ty, (int)OrderSide.Sell),
                            builderSHORT.Eq(x => x.status, 0)
                        )).OrderBy(x => x.rank).ToList();
                        await Bybit_TradeRSI_SHORT(lShort);
                    }

                    _dData.Clear();
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_Trade|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Entry_LONG()
        {
            var now = DateTime.UtcNow;
            var builderFilter = Builders<Prepare>.Filter;
            var lLong = _prepareRepo.GetByFilter(builderFilter.And(
                builderFilter.Eq(x => x.ex, _exchange),
                builderFilter.Eq(x => x.side, (int)OrderSide.Buy)
            ));
            //Console.WriteLine($"LONG: {lLong.Count()}");

            foreach (var item in lLong)
            {
                try
                {
                    var l15m = await GetData(item.s, true);
                    if (l15m is null || !l15m.Any())
                        continue;

                    var bb = l15m.GetBollingerBands();

                    var builder = Builders<Prepare>.Filter;
                    var last = l15m.Last();
                    var near = l15m.SkipLast(1).Last();
                    var bb_last = bb.First(x => x.Date == last.Date);
                    var bb_near = bb.First(x => x.Date == near.Date);
                    if (last.High >= (decimal)bb_last.UpperBand.Value
                        || near.High >= (decimal)bb_near.UpperBand.Value)
                    {
                        _prepareRepo.DeleteMany(builder.And(
                              builder.Eq(x => x.ex, _exchange),
                              builder.Eq(x => x.s, item.s)
                          ));
                        continue;
                    }

                    var action = last.IsBuy(item.Close, (EOrderSideOption)item.op);

                    if (action.Item1)
                    {
                        await PlaceOrder(new SignalBase
                        {
                            s = item.s,
                            ex = _exchange,
                            Side = item.side,
                            timeFlag = (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                            quote = last,
                            rank = item.Index
                        });

                        _prepareRepo.DeleteMany(builder.And(
                               builder.Eq(x => x.ex, _exchange),
                               builder.Eq(x => x.s, item.s)
                           ));
                        continue;
                    }    

                    var time = (now - item.Date).TotalHours;
                    if (time >= 2)
                    {
                        _prepareRepo.DeleteMany(builder.And(
                               builder.Eq(x => x.ex, _exchange),
                               builder.Eq(x => x.s, item.s)
                           ));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Entry_LONG|INPUT: {JsonConvert.SerializeObject(item)}|EXCEPTION| {ex.Message}");
                }
            }
        }
        private async Task Entry_SHORT()
        {
            var now = DateTime.UtcNow;
            var builderFilter = Builders<Prepare>.Filter;
            var lShort = _prepareRepo.GetByFilter(builderFilter.And(
                builderFilter.Eq(x => x.ex, _exchange),
                builderFilter.Eq(x => x.side, (int)OrderSide.Sell)
            ));

            //Console.WriteLine($"SHORT: {lShort.Count()}");

            foreach (var item in lShort)
            {
                try
                {
                    var l15m = await GetData(item.s, true);
                    if (l15m is null || !l15m.Any())
                        continue;
                    var bb = l15m.GetBollingerBands();

                    var builder = Builders<Prepare>.Filter;
                    var last = l15m.Last();
                    if ((last.Date - item.Date).TotalMinutes < 15)
                        continue;

                    var near = l15m.SkipLast(1).Last();
                    var bb_last = bb.First(x => x.Date == last.Date);
                    var bb_near = bb.First(x => x.Date == near.Date);
                    if(last.Low <= (decimal)bb_last.LowerBand.Value
                        || near.Low <= (decimal)bb_near.LowerBand.Value)
                    {
                        _prepareRepo.DeleteMany(builder.And(
                              builder.Eq(x => x.ex, _exchange),
                              builder.Eq(x => x.s, item.s)
                          ));
                        continue;
                    }

                    var action = last.IsSell(item.Close, (EOrderSideOption)item.op);
                    if (action.Item1)
                    {
                        await PlaceOrder(new SignalBase
                        {
                            s = item.s,
                            ex = _exchange,
                            Side = item.side,
                            timeFlag = (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                            quote = last,
                            rank = item.Index
                        });

                        _prepareRepo.DeleteMany(builder.And(
                               builder.Eq(x => x.ex, _exchange),
                               builder.Eq(x => x.s, item.s)
                           ));
                        continue;
                    }

                    var time = (now - item.Date).TotalHours;
                    if(time >= 2)
                    {
                        _prepareRepo.DeleteMany(builder.And(
                               builder.Eq(x => x.ex, _exchange),
                               builder.Eq(x => x.s, item.s)
                           ));
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Entry_SHORT|INPUT: {JsonConvert.SerializeObject(item)}|EXCEPTION| {ex.Message}");
                }
            }
        }

        private async Task Bybit_TradeRSI_LONG(IEnumerable<Symbol> lSym)
        {
            try
            {
                if (!lSym.Any())
                    return;

                foreach (var sym in lSym)
                {
                    try
                    {
                        //gia
                        var l15m = await GetData(sym.s, false);
                        if (l15m is null || !l15m.Any())
                            continue;

                        var last = l15m.Last();
                        if (last.Volume <= 0)
                            continue;

                        var flag = l15m.IsFlagBuy();
                        if (!flag.Item1)
                            continue;

                        var builder = Builders<Prepare>.Filter;
                        var filter = builder.And(
                            builder.Eq(x => x.ex, _exchange),
                            builder.Eq(x => x.s, sym.s)
                        );
                        var entityPrepare = _prepareRepo.GetEntityByFilter(filter);

                        if (entityPrepare != null)
                        {
                            _prepareRepo.DeleteMany(filter);
                        }

                        var pivot = flag.Item2;
                        _prepareRepo.InsertOne(new Prepare
                        {
                            Open = pivot.Open,
                            Close = pivot.Close,
                            High = pivot.High,
                            Low = pivot.Low,
                            Volume = pivot.Volume,
                            Date = pivot.Date,
                            s = sym.s,
                            ex = _exchange,
                            Index = sym.rank,
                            side = (int)OrderSide.Buy,
                            op = sym.op
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TradeRSI_LONG|INPUT: {sym.s}|EXCEPTION| {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TradeRSI_LONG|EXCEPTION| {ex.Message}");
            }
        }
        
        private async Task Bybit_TradeRSI_SHORT(IEnumerable<Symbol> lSym)
        {
            try
            {
                if (!lSym.Any())
                    return;

                var lPrepare = new List<Prepare>();
                foreach (var sym in lSym)
                {
                    try
                    {
                        //gia
                        var l15m = await GetData(sym.s, false);
                        if (l15m is null || !l15m.Any())
                            continue;

                        var last = l15m.Last();
                        if (last.Volume <= 0)
                            continue;

                        var flag = l15m.IsFlagSell();
                        if (!flag.Item1)
                            continue;

                        var builder = Builders<Prepare>.Filter;
                        var filter = builder.And(
                            builder.Eq(x => x.ex, _exchange),
                            builder.Eq(x => x.s, sym.s)
                        );
                        var entityPrepare = _prepareRepo.GetEntityByFilter(filter);

                        if (entityPrepare != null)
                        {
                            _prepareRepo.DeleteMany(filter);
                        }

                        var pivot = flag.Item2;
                        _prepareRepo.InsertOne(new Prepare
                        {
                            Open = pivot.Open,
                            Close = pivot.Close,
                            High = pivot.High,
                            Low = pivot.Low,
                            Volume = pivot.Volume,
                            Date = pivot.Date,
                            s = sym.s,
                            ex = _exchange,
                            Index = sym.rank,
                            side = (int)OrderSide.Sell
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TradeRSI_SHORT|INPUT: {sym.s}|EXCEPTION| {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TradeRSI_SHORT|EXCEPTION| {ex.Message}");
            }
        }

        private async Task<List<Quote>> GetData(string symbol, bool isOverride)
        {
            try
            {
                if(!isOverride)
                {
                    if (_dData.ContainsKey(symbol))
                        return _dData[symbol].ToList();
                }
               
                var l15m = await _apiService.GetData_Bybit(symbol, DateTime.MinValue);
                Thread.Sleep(100);
                if (l15m is null || !l15m.Any())
                    return null;

                if (_dData.ContainsKey(symbol))
                {
                    _dData[symbol] = l15m;
                }
                else
                {
                    _dData.Add(symbol, l15m);
                }

                return l15m.ToList();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.GetData|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private async Task Bybit_TakeProfit()
        {
            try
            {
                var pos = await StaticTrade.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, settleAsset: "USDT");
                if (!pos.Data.List.Any())
                    return;

                var dt = DateTime.UtcNow;

                #region TP
                var dic = new Dictionary<BybitPosition, decimal>();
                foreach (var item in pos.Data.List)
                {
                    var side = item.Side == PositionSide.Sell ? OrderSide.Sell : OrderSide.Buy;

                    var curTime = (dt - item.UpdateTime.Value).TotalHours;
                    double dicTime = 0;

                    var builder = Builders<PlaceOrderTrade>.Filter;
                    var place = _placeRepo.GetEntityByFilter(builder.And(
                        builder.Eq(x => x.ex, _exchange),
                        builder.Eq(x => x.s, item.Symbol),
                        builder.Gte(x => x.time, DateTime.Now.AddHours(-5)),
                        builder.Lte(x => x.time, DateTime.Now.AddHours(-4))
                    ));

                    if (place != null)
                    {
                        dicTime = 10;
                    }    

                    if (curTime >= _HOUR || dicTime >= _HOUR)
                    {
                        await PlaceOrderClose(item);
                    }
                    else
                    {
                        var l15m = await GetData(item.Symbol, true);
                        if (l15m is null || !l15m.Any())
                            continue;

                        var lbb = l15m.GetBollingerBands();

                        var last = l15m.Last();
                        var bb_Last = lbb.First(x => x.Date == last.Date);
                        l15m.Remove(last);

                        var cur = l15m.Last();
                        var bb_Cur = lbb.First(x => x.Date == cur.Date);

                        var flag = false;
                        var lChotNon = l15m.Where(x => x.Date > item.UpdateTime.Value && x.Date < cur.Date);
                        var isChotNon = false;
                        if (side == OrderSide.Buy)
                        {
                            foreach (var chot in lChotNon)
                            {
                                var bbCheck = lbb.First(x => x.Date == chot.Date);
                                if(chot.High >= (decimal)bbCheck.Sma.Value)
                                {
                                    //isChotNon = true;
                                }
                            }

                            if (last.High > (decimal)bb_Last.UpperBand.Value
                                || cur.High > (decimal)bb_Cur.UpperBand.Value)
                            {
                                flag = true;
                            }
                            else if (isChotNon
                                    && cur.Close < (decimal)bb_Cur.Sma.Value
                                    && cur.Close <= cur.Open
                                    && last.Close >= item.AveragePrice.Value)
                            {
                                flag = true;
                            }
                        }
                        else if (side == OrderSide.Sell)
                        {
                            foreach (var chot in lChotNon)
                            {
                                var bbCheck = lbb.First(x => x.Date == chot.Date);
                                if (chot.Low <= (decimal)bbCheck.Sma.Value)
                                {
                                    //isChotNon = true;
                                }
                            }

                            if (last.Low < (decimal)bb_Last.LowerBand.Value
                                || cur.Low < (decimal)bb_Cur.LowerBand.Value)
                            {  
                                flag = true; 
                            }
                            else if(isChotNon
                                && cur.Close > (decimal)bb_Cur.Sma.Value
                                && cur.Close >= cur.Open
                                && last.Close <= item.AveragePrice.Value)
                            {
                                flag = true;
                            }
                        }

                        var rateBB = (decimal)(Math.Round((-1 + bb_Cur.UpperBand.Value / bb_Cur.LowerBand.Value)) - 1);
                        if (rateBB < _TP_RATE_MIN - 0.01m)
                        {
                            rateBB = _TP_RATE_MIN;
                        }
                        else if (rateBB > _TP_RATE_MAX)
                        {
                            rateBB = _TP_RATE_MAX;
                        }

                        var rate = Math.Abs(Math.Round((-1 + cur.Close / item.AveragePrice.Value), 1));
                        if (rate >= rateBB)
                        {
                            flag = true;
                        }

                        if (flag)
                        {
                            await PlaceOrderClose(item);
                        }
                        else
                        {
                            if((cur.Close >= item.AveragePrice.Value && side == OrderSide.Buy)
                                || (cur.Close <= item.AveragePrice.Value && side == OrderSide.Sell)) 
                            {
                                dic.Add(item, rate);
                            }
                            else
                            {
                                dic.Add(item, -rate);
                            }
                        }
                    }
                }
                //Nếu có ít nhất 3 lệnh xanh thì sẽ bán bất kỳ lệnh nào lãi hơn _TP_RATE_MIN
                if (dic.Count(x => x.Value > 0) >= 3)
                {
                    foreach (var item in dic)
                    {
                        if(item.Value >= _TP_RATE_MIN) 
                        {
                            await PlaceOrderClose(item.Key);
                        }
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TakeProfit|EXCEPTION| {ex.Message}");
            }
        }

        private async Task<bool> PlaceOrder(SignalBase entity)
        {
            try
            {
                Console.WriteLine($"BYBIT PlaceOrder: {JsonConvert.SerializeObject(entity)}");
                var curTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                var account = await Bybit_GetAccountInfo();
                if (account == null)
                {
                    await _teleService.SendMessage(_idUser, "[ERROR_bybit] Không lấy được thông tin tài khoản");
                    return false;
                }
                //Lay Unit tu database
                var lConfig = _configRepo.GetAll();
                var max = lConfig.First(x => x.ex == _exchange && x.op == (int)EOption.Max);
                var thread = lConfig.First(x => x.ex == _exchange && x.op == (int)EOption.Thread);

                if ((account.WalletBalance.Value - account.TotalPositionInitialMargin.Value) * _margin <= (decimal)max.value)
                    return false;

                //Nếu trong 4 tiếng gần nhất giảm quá 10% thì không mua mới
                var lIncome = await StaticTrade.ByBitInstance().V5Api.Account.GetTransactionHistoryAsync(limit: 200);
                if (lIncome == null || !lIncome.Success)
                {
                    await _teleService.SendMessage(_idUser, "[ERROR_bybit] Không lấy được lịch sử thay đổi số dư");
                    return false;
                }
                var lIncomeCheck = lIncome.Data.List.Where(x => x.TransactionTime >= DateTime.UtcNow.AddHours(-4));
                if (lIncomeCheck.Count() >= 2)
                {
                    var first = lIncomeCheck.First();//giá trị mới nhất
                    var last = lIncomeCheck.Last();//giá trị cũ nhất
                    if (first.CashBalance > 0)
                    {
                        var rate = 1 - last.CashBalance.Value/first.CashBalance.Value;
                        var div = last.CashBalance.Value - first.CashBalance.Value;

                        if ((double)div * 10 > 0.6 * max.value)
                            return false;

                        if (rate <= -0.13m)
                            return false;
                    }
                }

                var pos = await StaticTrade.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, settleAsset: "USDT");
                if (pos.Data.List.Count() >= thread.value)
                    return false;

                if (pos.Data.List.Any(x => x.Symbol == entity.s))
                    return false;

                var lInfo = await StaticTrade.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, entity.s);
                var info = lInfo.Data.List.FirstOrDefault();
                if (info == null) return false;
                var tronGia = (int)info.PriceScale;
                var tronSL = info.LotSizeFilter.QuantityStep;

                var side = (OrderSide)entity.Side;
                var SL_side = side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
                var direction = side == OrderSide.Buy ? TriggerDirection.Fall : TriggerDirection.Rise;
              
                decimal soluong = (decimal)max.value / entity.quote.Close;
                if(tronSL == 1)
                {
                    soluong = Math.Round(soluong);
                }
                else if(tronSL == 10)
                {
                    soluong = Math.Round(soluong);
                    var odd = soluong % 10;
                    soluong -= odd;
                }
                else if(tronSL == 100)
                {
                    soluong = Math.Round(soluong);
                    var odd = soluong % 100;
                    soluong -= odd;
                }
                else if(tronSL == 1000)
                {
                    soluong = Math.Round(soluong);
                    var odd = soluong % 1000;
                    soluong -= odd;
                }
                else
                {
                    var lamtronSL = tronSL.ToString("#.##########").Split('.').Last().Length;
                    soluong = Math.Round(soluong, lamtronSL);
                }

                var res = await StaticTrade.ByBitInstance().V5Api.Trading.PlaceOrderAsync(Category.Linear,
                                                                                        entity.s,
                                                                                        side: side,
                                                                                        type: NewOrderType.Market,
                                                                                        reduceOnly: false,
                                                                                        quantity: soluong);
                Thread.Sleep(500);
                //nếu lỗi return
                if (!res.Success)
                {
                    if (!_lIgnoreCode.Any(x => x == res.Error.Code))
                    {
                        await _teleService.SendMessage(_idUser, $"[ERROR_Bybit] |{entity.s}|{res.Error.Code}:{res.Error.Message}");
                    }
                    return false;
                }

                var resPosition = await StaticTrade.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, entity.s);
                Thread.Sleep(500);
                if (!resPosition.Success)
                {
                    await _teleService.SendMessage(_idUser, $"[ERROR_Bybit] |{entity.s}|{res.Error.Code}:{res.Error.Message}");
                    return false;
                }

                if (resPosition.Data.List.Any())
                {
                    var first = resPosition.Data.List.First();

                    decimal sl = 0;
                    if (side == OrderSide.Buy)
                    {
                        sl = Math.Round(first.MarkPrice.Value * (decimal)(1 - _SL_RATE * 2), tronGia);
                    }
                    else
                    {
                        sl = Math.Round(first.MarkPrice.Value * (decimal)(1 + _SL_RATE * 2), tronGia);
                    }
                    res = await StaticTrade.ByBitInstance().V5Api.Trading.PlaceOrderAsync(Category.Linear,
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
                        await _teleService.SendMessage(_idUser, $"[ERROR_Bybit_SL] |{first.Symbol}|{res.Error.Code}:{res.Error.Message}");
                        return false;
                    }
                    //Print
                    var entry = Math.Round(first.AveragePrice.Value, tronGia);

                    var mes = $"[ACTION - {side.ToString().ToUpper()}|Bybit] {first.Symbol}({entity.rank})|ENTRY: {entry}";
                    await _teleService.SendMessage(_idUser, mes);

                    _placeRepo.InsertOne(new PlaceOrderTrade
                    {
                        ex = _exchange,
                        s = first.Symbol,
                        time = DateTime.Now
                    });
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.PlaceOrder|EXCEPTION| {ex.Message}");
            }
            return false;
        }

        private async Task<bool> PlaceOrderClose(BybitPosition pos)
        {
            var side = pos.Side == PositionSide.Sell ? OrderSide.Sell : OrderSide.Buy;
            var CLOSE_side = pos.Side == PositionSide.Sell ? OrderSide.Buy : OrderSide.Sell;
            try
            {
                var res = await StaticTrade.ByBitInstance().V5Api.Trading.PlaceOrderAsync(Category.Linear,
                                                                                        pos.Symbol,
                                                                                        side: CLOSE_side,
                                                                                        type: NewOrderType.Market,
                                                                                        quantity: Math.Abs(pos.Quantity));
                if (res.Success)
                {
                    var resCancel = await StaticTrade.ByBitInstance().V5Api.Trading.CancelAllOrderAsync(Category.Linear, pos.Symbol);

                    var rate = Math.Round(100 * (-1 + pos.MarkPrice.Value / pos.AveragePrice.Value), 1);
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

                    var balance = string.Empty;
                    var account = await Bybit_GetAccountInfo();
                    if (account != null)
                    {
                        balance = $"|Balance: {Math.Round(account.WalletBalance.Value, 1)}$";
                    }

                    await _teleService.SendMessage(_idUser, $"[CLOSE - {side.ToString().ToUpper()}({winloss}: {rate}%)|Bybit] {pos.Symbol}|TP: {pos.MarkPrice}|Entry: {pos.AveragePrice}{balance}");

                    var builder = Builders<PlaceOrderTrade>.Filter;
                    _placeRepo.DeleteMany(builder.And(
                                                        builder.Eq(x => x.ex, _exchange),
                                                        builder.Eq(x => x.s, pos.Symbol)
                                                    ));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.PlaceOrderClose|EXCEPTION| {ex.Message}");
            }
            await _teleService.SendMessage(_idUser, $"[Bybit] Không thể đóng lệnh {side}: {pos.Symbol}!");
            return false;
        }

        private List<long> _lIgnoreCode = new List<long>
        {
            110007
        };
    }
}
