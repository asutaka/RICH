using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;
using MongoDB.Driver;
using Skender.Stock.Indicators;
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
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private readonly IPlaceOrderTradeRepo _placeRepo;
        private readonly ISymbolRepo _symRepo;
        private readonly ISymbolConfigRepo _symConfigRepo;
        private readonly IConfigDataRepo _configRepo;
        private const long _idUser = 1066022551;
        private const decimal _margin = 10;
        private readonly int _HOUR = 4;//MA20 là 2h
        private readonly decimal _SL_RATE = 0.025m; //MA20 là 0.017
        private readonly decimal _TP_RATE_MIN = 0.025m;
        private readonly decimal _TP_RATE_MAX = 0.07m;

        private readonly int _exchange = (int)EExchange.Bybit;
        public BybitService(ILogger<BybitService> logger,
                            IAPIService apiService, ITeleService teleService, ISymbolConfigRepo symConfigRepo, IPlaceOrderTradeRepo placeRepo, ISymbolRepo symRepo,
                            IConfigDataRepo configRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _teleService = teleService;
            _symConfigRepo = symConfigRepo;
            _placeRepo = placeRepo;
            _symRepo = symRepo;
            _configRepo = configRepo;
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
                var dt = DateTime.Now;

                await Bybit_TakeProfit();
                await Bybit_TradeRSI_LONG(dt);
                await Bybit_TradeRSI_SHORT(dt);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_Trade|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Bybit_TradeRSI_LONG(DateTime dt)
        {
            try
            {
                if (dt.Minute % 15 != 0)
                    return;

                var builder = Builders<Symbol>.Filter;
                var lSym = _symRepo.GetByFilter(builder.And(
                    builder.Eq(x => x.ex, _exchange),
                    builder.Eq(x => x.ty, (int)OrderSide.Buy),
                    builder.Eq(x => x.status, 0)
                ));
                foreach (var sym in lSym.Select(x => x.s))
                {
                    try
                    {
                        //gia
                        var l15m = await _apiService.GetData_Bybit(sym, EInterval.M15);
                        Thread.Sleep(100);
                        if (l15m is null
                              || !l15m.Any())
                            continue;
                        var pivot = l15m.Last();
                        if (pivot.Volume <= 0)
                            continue;

                        var curPrice = pivot.Close;
                        l15m.Remove(pivot);

                        pivot = l15m.Last();
                        var near = l15m.SkipLast(1).Last();
                        var rateVol = Math.Round(pivot.Volume / near.Volume, 1);
                        if (rateVol > (decimal)0.6) //Vol hiện tại phải nhỏ hơn hoặc bằng 0.6 lần vol của nến liền trước
                            continue;

                        var lRsi = l15m.GetRsi();
                        var lbb = l15m.GetBollingerBands();
                        var rsiPivot = lRsi.Last();
                        var bbPivot = lbb.Last();

                        var lVol = l15m.Select(x => new Quote
                        {
                            Date = x.Date,
                            Close = x.Volume
                        }).ToList();
                        var lMaVol = lVol.GetSma(20);

                        var rsi_near = lRsi.SkipLast(1).Last();
                        var bb_near = lbb.SkipLast(1).Last();
                        var maVol_near = lMaVol.SkipLast(1).Last();
                        var sideDetect = -1;
                        if (rsiPivot.Rsi >= 25 && rsiPivot.Rsi <= 35 && curPrice < (decimal)bbPivot.Sma.Value) //LONG
                        {
                            //check nến liền trước
                            if (near.Close >= near.Open
                                || rsi_near.Rsi > 35
                                || near.Low >= (decimal)bb_near.LowerBand.Value)
                            {
                                continue;
                            }
                            if (!StaticVal._lCoinSpecial.Contains(sym))
                            {
                                if (near.Volume < (decimal)(maVol_near.Sma.Value * 1.5))
                                    continue;
                            }
                            var minOpenClose = Math.Min(near.Open, near.Close);
                            if (Math.Abs(minOpenClose - (decimal)bb_near.LowerBand.Value) > Math.Abs((decimal)bb_near.Sma.Value - minOpenClose))
                                continue;
                            //check tiếp nến pivot
                            if (pivot.Low >= (decimal)bbPivot.LowerBand.Value
                                || pivot.High >= (decimal)bbPivot.Sma.Value
                                || (pivot.Low >= near.Low && pivot.High <= near.High))
                                continue;
                            var ratePivot = Math.Abs((pivot.Open - pivot.Close) / (pivot.High - pivot.Low));
                            if (ratePivot > (decimal)0.8)
                            {
                                /*
                                    Nếu độ dài nến pivot >= độ dài nến tín hiệu thì bỏ qua
                                 */
                                var isValid = Math.Abs(pivot.Open - pivot.Close) >= Math.Abs(near.Open - near.Close);
                                if (isValid)
                                    continue;
                            }

                            sideDetect = (int)OrderSide.Buy;
                        }

                        if (sideDetect > -1)
                        {
                            await PlaceOrder(new SignalBase
                            {
                                s = sym,
                                ex = _exchange,
                                Side = sideDetect,
                                timeFlag = (int)DateTimeOffset.Now.ToUnixTimeSeconds()
                            }, curPrice);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TradeRSI_LONG|INPUT: {sym}|EXCEPTION| {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TradeRSI_LONG|EXCEPTION| {ex.Message}");
            }
        }
        
        private async Task Bybit_TradeRSI_SHORT(DateTime dt)
        {
            try
            {
                if (dt.Minute % 15 != 0)
                    return;

                var builder = Builders<Symbol>.Filter;
                var lSym = _symRepo.GetByFilter(builder.And(
                    builder.Eq(x => x.ex, _exchange),
                    builder.Eq(x => x.ty, (int)OrderSide.Sell),
                    builder.Eq(x => x.status, 0)
                ));
                foreach (var sym in lSym.Select(x => x.s))
                {
                    try
                    {
                        //gia
                        var l15m = await _apiService.GetData_Bybit(sym, EInterval.M15);
                        Thread.Sleep(100);
                        if (l15m is null
                              || !l15m.Any())
                            continue;
                        var pivot = l15m.Last();
                        if (pivot.Volume <= 0)
                            continue;

                        var curPrice = pivot.Close;
                        l15m.Remove(pivot);

                        pivot = l15m.Last();
                        var near = l15m.SkipLast(1).Last();
                        var rateVol = Math.Round(pivot.Volume / near.Volume, 1);
                        if (rateVol > (decimal)0.6) //Vol hiện tại phải nhỏ hơn hoặc bằng 0.6 lần vol của nến liền trước
                            continue;

                        var lRsi = l15m.GetRsi();
                        var lbb = l15m.GetBollingerBands();
                        var rsiPivot = lRsi.Last();
                        var bbPivot = lbb.Last();

                        var lVol = l15m.Select(x => new Quote
                        {
                            Date = x.Date,
                            Close = x.Volume
                        }).ToList();
                        var lMaVol = lVol.GetSma(20);

                        var rsi_near = lRsi.SkipLast(1).Last();
                        var bb_near = lbb.SkipLast(1).Last();
                        var maVol_near = lMaVol.SkipLast(1).Last();
                        var sideDetect = -1;
                        //Console.WriteLine($"SHORT|{sym}|{curPrice}|{bbPivot.Sma.Value}|{rsiPivot.Rsi}");
                        if (rsiPivot.Rsi >= 65 && rsiPivot.Rsi <= 80 && curPrice > (decimal)bbPivot.Sma.Value)//SHORT
                        {
                            //check nến liền trước
                            if (near.Close <= near.Open
                                || rsi_near.Rsi < 65
                                || near.High <= (decimal)bb_near.UpperBand.Value)
                            {
                                continue;
                            }
                            if (!StaticVal._lCoinSpecial.Contains(sym))
                            {
                                if (near.Volume < (decimal)(maVol_near.Sma.Value * 1.5))
                                    continue;
                            }
                            var maxOpenClose = Math.Max(near.Open, near.Close);
                            if (Math.Abs(maxOpenClose - (decimal)bb_near.UpperBand.Value) > Math.Abs((decimal)bb_near.Sma.Value - maxOpenClose))
                                continue;
                            //check tiếp nến pivot
                            if (pivot.High <= (decimal)bbPivot.UpperBand.Value
                                || pivot.Low <= (decimal)bbPivot.Sma.Value)
                                continue;
                            //check div by zero
                            if (near.High == near.Low
                                || pivot.High == pivot.Low
                                || Math.Min(pivot.Open, pivot.Close) == pivot.Low)
                                continue;

                            var rateNear = Math.Abs((near.Open - near.Close) / (near.High - near.Low));  //độ dài nến hiện tại
                            var ratePivot = Math.Abs((pivot.Open - pivot.Close) / (pivot.High - pivot.Low));  //độ dài nến pivot
                            var isHammer = (near.High - near.Close) >= (decimal)1.2 * (near.Close - near.Low);
                            if (isHammer) { }
                            else if (ratePivot < (decimal)0.2)
                            {
                                var checkDoji = (pivot.High - Math.Max(pivot.Open, pivot.Close)) / (Math.Min(pivot.Open, pivot.Close) - pivot.Low);
                                if (checkDoji >= (decimal)0.75 && checkDoji <= (decimal)1.25)
                                {
                                    continue;
                                }
                            }
                            else if (rateNear > (decimal)0.8)
                            {
                                //check độ dài nến pivot
                                var isValid = Math.Abs(pivot.Open - pivot.Close) >= Math.Abs(near.Open - near.Close);
                                if (isValid)
                                    continue;
                            }

                            sideDetect = (int)OrderSide.Sell;
                        }

                        if (sideDetect > -1)
                        {
                            await PlaceOrder(new SignalBase
                            {
                                s = sym,
                                ex = _exchange,
                                Side = sideDetect,
                                timeFlag = (int)DateTimeOffset.Now.ToUnixTimeSeconds()
                            }, curPrice);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TradeRSI_SHORT|INPUT: {sym}|EXCEPTION| {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TradeRSI_SHORT|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Bybit_TakeProfit()
        {
            try
            {
                var pos = await StaticVal.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, settleAsset: "USDT");
                if (!pos.Data.List.Any())
                    return;

                var dt = DateTime.UtcNow;

                #region Sell
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
                        //var rate = Math.Abs(Math.Round(100 * (-1 + cur.Close / item.AveragePrice.Value), 1));
                        //if (rate < 1)
                        //    continue;

                        if (side == OrderSide.Buy && Math.Max(last.High, cur.High) > (decimal)bb.UpperBand.Value)
                        {
                            flag = true;
                        }
                        else if (side == OrderSide.Sell && Math.Min(last.Low, cur.Low) < (decimal)bb.LowerBand.Value)
                        {
                            flag = true;
                        }

                        var rateBB = (decimal)(Math.Round((-1 + bb.UpperBand.Value / bb.LowerBand.Value)) - 1);
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
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TakeProfit|EXCEPTION| {ex.Message}");
            }
        }

        private async Task<bool> PlaceOrder(SignalBase entity, decimal lastPrice)
        {
            try
            {
                //var SL_RATE = 0.017;
                var curTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                var account = await Bybit_GetAccountInfo();
                if (account == null)
                {
                    await _teleService.SendMessage(_idUser, "[ERROR_bybit] Không lấy được thông tin tài khoản");
                    return false;
                }
                //Lay Unit tu database
                var lConfig = _configRepo.GetAll();
                var config = lConfig.First(x => x.op == (int)EOption.Unit && x.ex == (int)EExchange.Bybit);

                if ((account.WalletBalance.Value - account.TotalPositionInitialMargin.Value) * _margin <= (decimal)config.value)
                    return false;

                //Nếu trong 2 tiếng gần nhất giảm quá 10% thì không mua mới
                var lIncome = await StaticVal.ByBitInstance().V5Api.Account.GetTransactionHistoryAsync(limit: 200);
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
                        if (rate <= -0.1m)
                            return false;
                    }
                }

                var pos = await StaticVal.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, settleAsset: "USDT");
                if (pos.Data.List.Count() >= 4)
                    return false;

                if (pos.Data.List.Any(x => x.Symbol == entity.s))
                    return false;

                var side = (OrderSide)entity.Side;
                var SL_side = side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
                var direction = side == OrderSide.Buy ? TriggerDirection.Fall : TriggerDirection.Rise;

                var tronSL = 2;
                var exists = _symConfigRepo.GetEntityByFilter(Builders<SymbolConfig>.Filter.Eq(x => x.s, entity.s));
                if (exists != null)
                {
                    tronSL = exists.amount;
                }
                else if (lastPrice < 5)
                {
                    tronSL = 0;
                }
              
                decimal soluong = (decimal)config.value / lastPrice;
                if(tronSL == -1)
                {
                    soluong = Math.Round(soluong);
                    var odd = soluong % 10;
                    soluong -= odd;
                }
                else if(tronSL == -2)
                {
                    soluong = Math.Round(soluong);
                    var odd = soluong % 100;
                    soluong -= odd;
                }
                else
                {
                    soluong = Math.Round(soluong, tronSL);
                }

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
                    if (!_lIgnoreCode.Any(x => x == res.Error.Code))
                    {
                        await _teleService.SendMessage(_idUser, $"[ERROR_Bybit] |{entity.s}|{res.Error.Code}:{res.Error.Message}");
                    }
                    return false;
                }

                var resPosition = await StaticVal.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, entity.s);
                Thread.Sleep(500);
                if (!resPosition.Success)
                {
                    await _teleService.SendMessage(_idUser, $"[ERROR_Bybit] |{entity.s}|{res.Error.Code}:{res.Error.Message}");
                    return false;
                }

                if (resPosition.Data.List.Any())
                {
                    var first = resPosition.Data.List.First();
                    var tronGia = 0;
                    if (exists != null)
                    {
                        tronGia = exists.price;
                    }
                    else if(lastPrice < 5)
                    {
                        var price = lastPrice.ToString().Split('.').Last();
                        price = price.ReverseString();
                        tronGia = long.Parse(price).ToString().Length;
                    }

                    decimal sl = 0;
                    if (side == OrderSide.Buy)
                    {
                        sl = Math.Round(first.MarkPrice.Value * (decimal)(1 - _SL_RATE), tronGia);
                    }
                    else
                    {
                        sl = Math.Round(first.MarkPrice.Value * (decimal)(1 + _SL_RATE), tronGia);
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
                        await _teleService.SendMessage(_idUser, $"[ERROR_Bybit_SL] |{first.Symbol}|{res.Error.Code}:{res.Error.Message}");
                        return false;
                    }
                    //Print
                    var entry = Math.Round(first.AveragePrice.Value, tronGia);

                    var mes = $"[ACTION - {side.ToString().ToUpper()}|Bybit] {first.Symbol}|ENTRY: {entry}";
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
                var res = await StaticVal.ByBitInstance().V5Api.Trading.PlaceOrderAsync(Category.Linear,
                                                                                        pos.Symbol,
                                                                                        side: CLOSE_side,
                                                                                        type: NewOrderType.Market,
                                                                                        quantity: Math.Abs(pos.Quantity));
                if (res.Success)
                {
                    var resCancel = await StaticVal.ByBitInstance().V5Api.Trading.CancelAllOrderAsync(Category.Linear, pos.Symbol);

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
