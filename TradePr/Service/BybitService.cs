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
        private readonly ISymbolRepo _symRepo;
        private readonly IConfigDataRepo _configRepo;
        private readonly ITradingRepo _tradeRepo;
        private const long _idUser = 1066022551;
        private const decimal _margin = 10;
        private readonly int _HOUR = 8;//MA20 là 2h
        private readonly decimal _SL_RATE = 0.03m; //MA20 là 0.017
        private readonly decimal _TP_RATE_MIN = 0.025m;
        private readonly decimal _TP_RATE_MAX = 0.07m;
        private readonly int _exchange = (int)EExchange.Bybit;

        public BybitService(ILogger<BybitService> logger,
                            IAPIService apiService, ITeleService teleService, ISymbolRepo symRepo,
                            IConfigDataRepo configRepo, ITradingRepo tradeRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _teleService = teleService;
            _symRepo = symRepo;
            _configRepo = configRepo;
            _tradeRepo = tradeRepo;
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
                var disableLong = lConfig.FirstOrDefault(x => x.ex == _exchange && x.op == (int)EOption.DisableLong && x.status == 1);
                var disableShort = lConfig.FirstOrDefault(x => x.ex == _exchange && x.op == (int)EOption.DisableShort && x.status == 1);

                var flagLong = disableLong != null;
                var flagShort = disableShort != null;

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

                    if (!flagShort)
                    {
                        var builderSHORT = Builders<Symbol>.Filter;
                        var lShort = _symRepo.GetByFilter(builderSHORT.And(
                            builderSHORT.Eq(x => x.ex, _exchange),
                            builderSHORT.Eq(x => x.ty, (int)OrderSide.Sell),
                            builderSHORT.Eq(x => x.status, 0)
                        )).OrderBy(x => x.rank).ToList();
                        await Bybit_TradeRSI_SHORT(lShort);
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_Trade|EXCEPTION| {ex.Message}");
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
                        var l15m = await GetData(sym.s);
                        if (l15m is null || !l15m.Any())
                            continue;

                        var last = l15m.Last();
                        if (last.Volume <= 0)
                            continue;

                        var eOp = EOptionTrade.None;
                        (bool, QuoteEx) flag = (false, null);

                        if (sym.op == (int)EOptionTrade.Doji 
                            || sym.op == (int)EOptionTrade.Normal_Doji)
                        {
                            flag = l15m.SkipLast(1).IsFlagBuy_Doji();
                            if (flag.Item1)
                            {
                                eOp = EOptionTrade.Doji;
                            }
                        }

                        if (sym.op == (int)EOptionTrade.Normal
                           || sym.op == (int)EOptionTrade.Normal_Doji)
                        {
                            flag = l15m.SkipLast(1).IsFlagBuy();
                            if (flag.Item1)
                            {
                                eOp = EOptionTrade.Normal;
                            }
                        }

                        if (eOp == EOptionTrade.None)
                            continue;

                        //
                        var builder = Builders<Trading>.Filter;
                        var filter = builder.And(
                            builder.Eq(x => x.ex, _exchange),
                            builder.Eq(x => x.s, sym.s),
                            builder.Eq(x => x.Status, 0),
                            builder.Gte(x => x.d, (int)DateTimeOffset.Now.AddHours(-8).ToUnixTimeSeconds())
                        );
                        var exists = _tradeRepo.GetEntityByFilter(filter);
                        if (exists != null)
                        {
                            continue;
                        }

                        var res = await PlaceOrder(new Trading
                        {
                            ex = _exchange,
                            s = sym.s,
                            d = (int)DateTimeOffset.Now.AddHours(-1).ToUnixTimeSeconds(),
                            Date = DateTime.UtcNow,
                            Side = (int)OrderSide.Buy,
                            Op = (int)eOp,
                            Entry = (double)last.Close,
                            RateTP = (double)flag.Item2.Rate_TP
                        });

                        if (res == EError.MaxThread)
                            break;
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

                foreach (var sym in lSym)
                {
                    try
                    {
                        //gia
                        var l15m = await GetData(sym.s);
                        if (l15m is null || !l15m.Any())
                            continue;

                        var last = l15m.Last();
                        if (last.Volume <= 0)
                            continue;

                        var eOp = EOptionTrade.None;
                        (bool, QuoteEx) flag = (false, null);
                        if(sym.op == (int)EOptionTrade.Doji)
                        {
                            flag = l15m.SkipLast(1).IsFlagSell_Doji();
                            if (flag.Item1)
                            {
                                eOp = EOptionTrade.Doji;
                            }
                        }
                        else
                        {
                            flag = l15m.SkipLast(1).IsFlagSell();
                            if (flag.Item1)
                            {
                                eOp = EOptionTrade.Normal;
                            }
                        }

                        if (eOp == EOptionTrade.None)
                            continue;

                        //
                        var builder = Builders<Trading>.Filter;
                        var filter = builder.And(
                            builder.Eq(x => x.ex, _exchange),
                            builder.Eq(x => x.s, sym.s),
                            builder.Eq(x => x.Status, 0),
                            builder.Gte(x => x.d, (int)DateTimeOffset.Now.AddHours(-8).ToUnixTimeSeconds())
                        );
                        var exists = _tradeRepo.GetEntityByFilter(filter);
                        if (exists != null)
                        {
                            continue;
                        }

                        var res = await PlaceOrder(new Trading
                        {
                            ex = _exchange,
                            s = sym.s,
                            d = (int)DateTimeOffset.Now.AddHours(-1).ToUnixTimeSeconds(),
                            Date = DateTime.UtcNow,
                            Side = (int)OrderSide.Sell,
                            Op = (int)eOp,
                            Entry = (double)last.Close,
                            RateTP = (double)flag.Item2.Rate_TP
                        });

                        if (res == EError.MaxThread)
                            break;
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

        private async Task<List<Quote>> GetData(string symbol)
        {
            try
            {
                var l15m = await _apiService.GetData_Bybit(symbol);
                Thread.Sleep(100);
                if (l15m is null || !l15m.Any())
                    return null;

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

                    var builder = Builders<Trading>.Filter;
                    var place = _tradeRepo.GetEntityByFilter(builder.And(
                        builder.Eq(x => x.ex, _exchange),
                        builder.Eq(x => x.s, item.Symbol),
                        builder.Eq(x => x.Status, 0)
                        //builder.Gte(x => x.Date, DateTime.Now.AddHours(-8)),
                        //builder.Lte(x => x.Date, DateTime.Now.AddHours(-7))
                    ));
                    //Lỗi gì đó ko lưu lại đc log
                    if (place is null)
                    {
                        Console.WriteLine($"Place null");
                        await PlaceOrderClose(item);
                        continue;
                    }
                    //Hết thời gian
                    var dTime = (dt - place.Date).TotalHours;
                    if(curTime >= _HOUR || dTime >= _HOUR)
                    {
                        Console.WriteLine($"Het thoi gian: curTime: {curTime}, dTime: {dTime}");
                        await PlaceOrderClose(item);
                        continue;
                    }
                    //Đạt Target
                    var rate = Math.Round((-1 + item.MarkPrice.Value / item.AveragePrice.Value), 1);
                    if((place.Side == (int)OrderSide.Buy && rate >= (decimal)place.RateTP)
                        || (place.Side == (int)OrderSide.Sell && rate <= -(decimal)place.RateTP))
                    {
                        Console.WriteLine($"Dat Target: rate: {rate}, RateTP:{place.RateTP}");
                        await PlaceOrderClose(item);
                        continue;
                    }    

                    var l15m = await GetData(item.Symbol);
                    if (l15m is null || !l15m.Any())
                        continue;

                    var lbb = l15m.GetBollingerBands();
                    var last = l15m.Last();

                    var cur = l15m.SkipLast(1).Last();
                    var bb_Cur = lbb.First(x => x.Date == cur.Date);

                    var lChotNon = l15m.Where(x => x.Date > item.UpdateTime.Value && x.Date < cur.Date);
                    var flag = false;
                    var isChotNon = false;
                    if (side == OrderSide.Buy)
                    {
                        foreach (var chot in lChotNon)
                        {
                            var bbCheck = lbb.First(x => x.Date == chot.Date);
                            if (chot.High >= (decimal)bbCheck.Sma.Value)
                            {
                                isChotNon = true;
                            }
                        }

                        if (last.High > (decimal)bb_Cur.UpperBand.Value
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
                    else
                    {
                        foreach (var chot in lChotNon)
                        {
                            var bbCheck = lbb.First(x => x.Date == chot.Date);
                            if (chot.Low <= (decimal)bbCheck.Sma.Value)
                            {
                                isChotNon = true;
                            }
                        }

                        if (last.Low < (decimal)bb_Cur.LowerBand.Value
                            || cur.Low < (decimal)bb_Cur.LowerBand.Value)
                        {
                            flag = true;
                        }
                        else if (isChotNon
                            && cur.Close > (decimal)bb_Cur.Sma.Value
                            && cur.Close >= cur.Open
                            && last.Close <= item.AveragePrice.Value)
                        {
                            flag = true;
                        }
                    }
                    if(flag)
                    {
                        Console.WriteLine($"Flag");
                        await PlaceOrderClose(item);
                        continue;
                    }    

                    if ((cur.Close >= item.AveragePrice.Value && side == OrderSide.Buy)
                            || (cur.Close <= item.AveragePrice.Value && side == OrderSide.Sell))
                    {
                        dic.Add(item, rate);
                    }
                    else
                    {
                        dic.Add(item, -rate);
                    }
                }
                //Nếu có ít nhất 3 lệnh xanh thì sẽ bán bất kỳ lệnh nào lãi hơn _TP_RATE_MIN
                if (dic.Count(x => x.Value > 0) >= 3)
                {
                    foreach (var item in dic)
                    {
                        if(item.Value >= _TP_RATE_MIN) 
                        {
                            Console.WriteLine($"_TP_RATE_MIN");
                            await PlaceOrderClose(item.Key);
                        }
                    }
                }
                #endregion

                #region Clean DB
                var lAll = _tradeRepo.GetAll().Where(x => x.Status == 0);
                foreach (var item in lAll)
                {
                    var exist = pos.Data.List.FirstOrDefault(x => x.Symbol == item.s);
                    if(exist is null)
                    {
                        item.Status = 1;
                        _tradeRepo.Update(item);
                    }    
                }
                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TakeProfit|EXCEPTION| {ex.Message}");
            }
        }

        private async Task<EError> PlaceOrder(Trading entity)
        {
            try
            {
                Console.WriteLine($"{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}|BYBIT PlaceOrder: {JsonConvert.SerializeObject(entity)}");
                var curTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                var account = await Bybit_GetAccountInfo();
                if (account == null)
                {
                    await _teleService.SendMessage(_idUser, "[ERROR_bybit] Không lấy được thông tin tài khoản");
                    return EError.Error;
                }
                //Lay Unit tu database
                var lConfig = _configRepo.GetAll();
                var max = lConfig.First(x => x.ex == _exchange && x.op == (int)EOption.Max);
                var thread = lConfig.First(x => x.ex == _exchange && x.op == (int)EOption.Thread);

                if ((account.WalletBalance.Value - account.TotalPositionInitialMargin.Value) * _margin <= (decimal)max.value)
                    return EError.Error;

                //Nếu trong 4 tiếng gần nhất giảm quá 10% thì không mua mới
                var lIncome = await StaticTrade.ByBitInstance().V5Api.Account.GetTransactionHistoryAsync(limit: 200);
                if (lIncome == null || !lIncome.Success)
                {
                    await _teleService.SendMessage(_idUser, "[ERROR_bybit] Không lấy được lịch sử thay đổi số dư");
                    return EError.Error;
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
                            return EError.Error;

                        if (rate <= -0.13m)
                            return EError.Error;
                    }
                }

                var pos = await StaticTrade.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, settleAsset: "USDT");
                if (pos.Data.List.Count() >= thread.value)
                    return EError.MaxThread;

                if (pos.Data.List.Any(x => x.Symbol == entity.s))
                    return EError.Error;

                var lInfo = await StaticTrade.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, entity.s);
                var info = lInfo.Data.List.FirstOrDefault();
                if (info == null) return EError.Error;
                var tronGia = (int)info.PriceScale;
                var tronSL = info.LotSizeFilter.QuantityStep;

                var side = (OrderSide)entity.Side;
                var SL_side = side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
                var direction = side == OrderSide.Buy ? TriggerDirection.Fall : TriggerDirection.Rise;
              
                var soluong = (decimal)(max.value / entity.Entry);
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
                    return EError.Error;
                }

                var resPosition = await StaticTrade.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, entity.s);
                Thread.Sleep(500);
                if (!resPosition.Success)
                {
                    await _teleService.SendMessage(_idUser, $"[ERROR_Bybit] |{entity.s}|{res.Error.Code}:{res.Error.Message}");
                    return EError.Error;
                }

                if (resPosition.Data.List.Any())
                {
                    var first = resPosition.Data.List.First();
                    decimal sl = 0;
                    if (side == OrderSide.Buy)
                    {
                        sl = Math.Round(first.MarkPrice.Value * (decimal)(1 - _SL_RATE), tronGia);
                    }
                    else
                    {
                        sl = Math.Round(first.MarkPrice.Value * (decimal)(1 + _SL_RATE), tronGia);
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
                    }

                    //Save + Print
                    var entry = Math.Round(first.AveragePrice.Value, tronGia);
                    entity.Entry = (double)entry;
                    entity.SL = (double)Math.Round(sl, tronGia);
                    _tradeRepo.InsertOne(entity);

                    var mes = $"[ACTION - {side.ToString().ToUpper()}|Bybit] {first.Symbol}|ENTRY: {entry}";
                    await _teleService.SendMessage(_idUser, mes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.PlaceOrder|EXCEPTION| {ex.Message}");
                return EError.Error;
            }
            return EError.Success;
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

                    var builder = Builders<Trading>.Filter;
                    var filter = builder.And(
                        builder.Eq(x => x.ex, _exchange),
                        builder.Eq(x => x.s, pos.Symbol),
                        builder.Eq(x => x.Status, 0),
                        builder.Gte(x => x.d, (int)DateTimeOffset.Now.AddHours(-8).ToUnixTimeSeconds())
                    );
                    var exists = _tradeRepo.GetEntityByFilter(filter);
                    if (exists != null)
                    {
                        exists.RateClose = (double)rate;
                        exists.Status = 1;
                        _tradeRepo.Update(exists);
                    }

                    var balance = string.Empty;
                    var account = await Bybit_GetAccountInfo();
                    if (account != null)
                    {
                        balance = $"|Balance: {Math.Round(account.WalletBalance.Value, 1)}$";
                    }

                    await _teleService.SendMessage(_idUser, $"[CLOSE - {side.ToString().ToUpper()}({winloss}: {rate}%)|Bybit] {pos.Symbol}|TP: {pos.MarkPrice}|Entry: {pos.AveragePrice}{balance}");
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
