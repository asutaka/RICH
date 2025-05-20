using Binance.Net.Objects.Models.Futures;
using MongoDB.Driver;
using Skender.Stock.Indicators;
using TradePr.DAL.Entity;
using TradePr.DAL;
using TradePr.Utils;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TradePr.Service
{
    public interface IBinanceService
    {
        Task<BinanceUsdFuturesAccountBalance> Binance_GetAccountInfo();
        Task Binance_Trade();
        Task<bool> PlaceOrder(SignalBase entity);
    }
    public class BinanceService : IBinanceService
    {
        private readonly ILogger<BinanceService> _logger;
        private readonly ITradingRepo _tradingRepo;
        private readonly ISymbolRepo _symRepo;
        private readonly IPlaceOrderTradeRepo _placeRepo;
        private readonly IConfigDataRepo _configRepo;
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private const long _idUser = 1066022551;
        private const decimal _margin = 10;
        private readonly int _HOUR = 4;
        private readonly decimal _SL_RATE = 0.025m;
        private readonly decimal _TP_RATE_MIN = 0.025m;
        private readonly decimal _TP_RATE_MAX = 0.07m;
        private readonly int _exchange = (int)EExchange.Binance;
        public BinanceService(ILogger<BinanceService> logger, ITradingRepo tradingRepo,
                            IAPIService apiService, ITeleService teleService, IPlaceOrderTradeRepo placeRepo, ISymbolRepo symRepo,
                            IConfigDataRepo configRepo)
        {
            _logger = logger;
            _tradingRepo = tradingRepo;
            _apiService = apiService;
            _teleService = teleService;
            _placeRepo = placeRepo;
            _symRepo = symRepo;
            _configRepo = configRepo;
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
                await InitSymbolConfig();
                await Binance_TakeProfit();

                var lConfig = _configRepo.GetAll();
                var disableAll = lConfig.FirstOrDefault(x => x.ex == _exchange && x.op == (int)EOption.DisableAll && x.status == 1);

                if (dt.Minute % 15 == 0
                    && (disableAll is null || disableAll.status == 0))
                {
                    var disableLong = lConfig.FirstOrDefault(x => x.ex == _exchange && x.op == (int)EOption.DisableLong && x.status == 1);
                    var disableShort = lConfig.FirstOrDefault(x => x.ex == _exchange && x.op == (int)EOption.DisableShort && x.status == 1);

                    var builderLONG = Builders<Symbol>.Filter;
                    var lLong = _symRepo.GetByFilter(builderLONG.And(
                        builderLONG.Eq(x => x.ex, _exchange),
                        builderLONG.Eq(x => x.ty, (int)OrderSide.Buy),
                        builderLONG.Eq(x => x.status, 0)
                    )).OrderBy(x => x.rank).Select(x => x.s); 
                    if(disableLong != null && disableLong.status == 1)
                    {
                        lLong = new List<string>();
                    }

                    var builderSHORT = Builders<Symbol>.Filter;
                    var lShort = _symRepo.GetByFilter(builderSHORT.And(
                        builderSHORT.Eq(x => x.ex, _exchange),
                        builderSHORT.Eq(x => x.ty, (int)OrderSide.Sell),
                        builderSHORT.Eq(x => x.status, 0)
                    )).OrderBy(x => x.rank).Select(x => x.s);
                    if (disableShort != null && disableShort.status == 1)
                    {
                        lShort = new List<string>();
                    }

                    //await Binance_TradeLiquid();
                    await Binance_TradeRSI_LONG(lLong.Take(20));
                    await Binance_TradeRSI_SHORT(lShort.Take(20));

                    await Binance_TradeRSI_LONG(lLong.Skip(20));
                    await Binance_TradeRSI_SHORT(lShort.Skip(20));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.Binance_Trade|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Binance_TradeLiquid()
        {
            try
            {
                var builder = Builders<Trading>.Filter;
                var lTrade = _tradingRepo.GetByFilter(builder.And(
                    builder.Eq(x => x.Status, 0),
                    builder.Gte(x => x.d, (int)DateTimeOffset.Now.AddHours(-1).ToUnixTimeSeconds()))
                );
                if (!(lTrade?.Any() ?? false))
                    return;

                var pos = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.GetPositionsAsync();
                foreach ( var item in lTrade)
                {
                    try
                    {
                        if (pos.Data.Any(x => x.Symbol == item.s))
                            continue;

                        //gia
                        var l15m = await _apiService.GetData_Binance(item.s, EInterval.M15);
                        Thread.Sleep(100);
                        if (l15m is null
                            || !l15m.Any())
                            continue;
                        var last = l15m.Last();
                        var curPrice = last.Close;
                        l15m.Remove(last);

                        last = l15m.Last();
                        var near = l15m.SkipLast(1).Last();
                        var rateVol = Math.Round(last.Volume / near.Volume, 1);
                        if (rateVol > (decimal)0.6) //Vol hiện tại phải nhỏ hơn hoặc bằng 0.6 lần vol của nến liền trước
                            continue;

                        var lRsi = l15m.GetRsi();
                        var rsi = lRsi.Last();
                        var lma = l15m.GetSma(20);
                        var ma = lma.Last();
                        var sideDetect = -1;
                        if (rsi.Rsi >= 25 && rsi.Rsi <= 35 && curPrice < (decimal)ma.Sma.Value) //LONG
                        {
                            sideDetect = (int)OrderSide.Buy;
                        }
                        else if(rsi.Rsi >= 65 &&  rsi.Rsi <= 75 && curPrice > (decimal)ma.Sma.Value)//SHORT
                        {
                            sideDetect = (int)OrderSide.Sell;
                        }

                        if (sideDetect > -1)
                        {
                            await PlaceOrder(new SignalBase
                            {
                                s = item.s,
                                ex = _exchange,
                                Side = sideDetect,
                                timeFlag = (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                                quote = last
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.Binance_TradeLiquid|EXCEPTION| {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.Binance_TradeLiquid|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Binance_TradeRSI_LONG(IEnumerable<string> lSym)
        {
            try
            {
                foreach (var sym in lSym)
                {
                    try
                    {
                        //gia
                        var l15m = await _apiService.GetData_Binance(sym, EInterval.M15);
                        Thread.Sleep(100);
                        if (l15m is null
                              || !l15m.Any())
                            continue;
                        var last = l15m.Last();
                        if (last.Volume <= 0)
                            continue;

                        var curPrice = last.Close;
                        l15m.Remove(last);

                        var pivot = l15m.Last();
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
                        //Console.WriteLine($"LONG|{sym}|{curPrice}|{bbPivot.Sma.Value}|{rsiPivot.Rsi}");
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

                            var checkTop = l15m.IsExistTopB();
                            if (!checkTop.Item1)
                                continue;

                            sideDetect = (int)OrderSide.Buy;
                        }

                        if (sideDetect > -1)
                        {
                            await PlaceOrder(new SignalBase
                            {
                                s = sym,
                                ex = _exchange,
                                Side = sideDetect,
                                timeFlag = (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                                quote = last
                            });
                        }
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.Binance_TradeRSI_LONG|INPUT: {sym}|EXCEPTION| {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.Binance_TradeRSI_LONG|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Binance_TradeRSI_SHORT(IEnumerable<string> lSym)
        {
            try
            {
                foreach (var sym in lSym)
                {
                    try
                    {
                        //gia
                        var l15m = await _apiService.GetData_Binance(sym, EInterval.M15);
                        Thread.Sleep(100);
                        if (l15m is null
                              || !l15m.Any())
                            continue;
                        var last = l15m.Last();
                        if (last.Volume <= 0)
                            continue;

                        var curPrice = last.Close;
                        l15m.Remove(last);

                        var pivot = l15m.Last();
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
                            if (isHammer){}
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
                                timeFlag = (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                                quote = last
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.Binance_TradeRSI_SHORT|INPUT: {sym}|EXCEPTION| {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.Binance_TradeRSI_SHORT|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Binance_TakeProfit()
        {
            try
            {
                var pos = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.GetPositionsAsync();

                #region Sell
                foreach (var item in pos.Data)
                {
                    var side = item.PositionAmt < 0 ? OrderSide.Sell : OrderSide.Buy;
                    var curTime = (DateTime.UtcNow - item.UpdateTime.Value).TotalHours;
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
                        var l15m = await _apiService.GetData_Binance(item.Symbol, EInterval.M15);
                        Thread.Sleep(100);
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
                                if (chot.High >= (decimal)bbCheck.Sma.Value)
                                {
                                    isChotNon = true;
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
                                    && last.Close >= item.EntryPrice)
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
                                    isChotNon = true;
                                }
                            }

                            if (last.Low < (decimal)bb_Last.LowerBand.Value
                                || cur.Low < (decimal)bb_Cur.LowerBand.Value)
                            {
                                flag = true;
                            }
                            else if (isChotNon
                                && cur.Close > (decimal)bb_Cur.Sma.Value
                                && cur.Close >= cur.Open
                                && last.Close <= item.EntryPrice)
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

                        var rate = Math.Abs(Math.Round((-1 + cur.Close / item.EntryPrice), 1));
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
            catch(Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.Binance_TradeRSI|EXCEPTION| {ex.Message}");
            }
        }

        public async Task<bool> PlaceOrder(SignalBase entity)
        {
            try
            {
                Console.WriteLine($"BINANCE PlaceOrder: {JsonConvert.SerializeObject(entity)}");
                var curTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                var account = await Binance_GetAccountInfo();
                if (account == null)
                {
                    await _teleService.SendMessage(_idUser, "[ERROR_binance] Không lấy được thông tin tài khoản");
                    return false;
                }

                //Lay Unit tu database
                var lConfig = _configRepo.GetAll();
                var max = lConfig.First(x => x.ex == _exchange && x.op == (int)EOption.Max);
                var thread = lConfig.First(x => x.ex == _exchange && x.op == (int)EOption.Thread);

                if (account.AvailableBalance * _margin <= (decimal)max.value)
                {
                    //await _teleService.SendMessage(_idUser, $"[ERROR_binance] Tiền không đủ| Balance: {account.WalletBalance}");
                    return false;
                }

                //Nếu trong 2 tiếng gần nhất có 4 lệnh thua và tổng âm thì ko mua mới
                var lIncome = await StaticVal.BinanceInstance().UsdFuturesApi.Account.GetIncomeHistoryAsync(incomeType: "REALIZED_PNL");
                if (lIncome == null || !lIncome.Success)
                {
                    await _teleService.SendMessage(_idUser, "[ERROR_binance] Không lấy được lịch sử thay đổi số dư");
                    return false;
                }
                if (lIncome.Data.Any())
                {
                    var income = lIncome.Data.Where(x => x.Timestamp >= DateTime.UtcNow.AddHours(-4)).Sum(x => x.Income);
                    var rate = income / account.WalletBalance;

                    if ((double)income * -10 > 0.6 * max.value)
                        return false;

                    if (rate <= -0.13m) 
                        return false;
                }

                var pos = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.GetPositionsAsync();
                if (pos.Data.Count() >= thread.value)
                    return false;

                if (pos.Data.Any(x => x.Symbol == entity.s))
                    return false;

                var marginType = await StaticVal.BinanceInstance().UsdFuturesApi.Account.ChangeMarginTypeAsync(entity.s, FuturesMarginType.Isolated);
                //if (!marginType.Success)
                //{
                //    await _teleService.SendMessage(_idUser, $"[ERROR_binance] Không chuyển được sang Isolated| {entity.s}");
                //    //return false;
                //}
                   

                var eMargin = StaticVal._dicBinanceMargin.FirstOrDefault(x => x.Key == entity.s);
                int margin = (int)_margin;
                if(eMargin.Key != null && eMargin.Value < (int)_margin)
                {
                    margin = eMargin.Value;
                }

                var initLevel = await StaticVal.BinanceInstance().UsdFuturesApi.Account.ChangeInitialLeverageAsync(entity.s, margin);
                if (!initLevel.Success)
                {
                    await _teleService.SendMessage(_idUser, $"[ERROR_binance] Không chuyển được đòn bẩy| {entity.s}(x{margin})");
                    //return false;
                }    
                    

                var side = (OrderSide)entity.Side;
                var SL_side = side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
                var symConfig = _lSymConfig.FirstOrDefault(x => x.s == entity.s);
                if (symConfig is null)
                    return false;

                decimal soluong = Math.Round((decimal)max.value / entity.quote.Close, symConfig.quan);
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
                    if (!_lIgnoreCode.Any(x => x == res.Error.Code))
                    {
                        await _teleService.SendMessage(_idUser, $"[ERROR_Binance] |{entity.s}|{res.Error.Code}:{res.Error.Message}");
                    }
                    
                    return false;
                }

                var resPosition = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.GetPositionsAsync(entity.s);
                Thread.Sleep(500);
                if (!resPosition.Success)
                {
                    await _teleService.SendMessage(_idUser, $"[ERROR_Binance] |{entity.s}|{res.Error.Code}:{res.Error.Message}");
                    return false;
                }

                if (resPosition.Data.Any())
                {
                    var first = resPosition.Data.First();

                    decimal sl = 0;
                    if (side == OrderSide.Buy)
                    {
                        sl = Math.Round(first.MarkPrice * (decimal)(1 - _SL_RATE), symConfig.price);
                    }
                    else
                    {
                        sl = Math.Round(first.MarkPrice * (decimal)(1 + _SL_RATE), symConfig.price);
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
                        await _teleService.SendMessage(_idUser, $"[ERROR_Binance_SL] |{first.Symbol}|{res.Error.Code}:{res.Error.Message}");
                        return false;
                    }
                    //Print
                    var entry = Math.Round(first.EntryPrice, symConfig.price);

                    var mes = $"[ACTION - {side.ToString().ToUpper()}|Binance] {first.Symbol}|ENTRY: {entry}";
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
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.PlaceOrder|EXCEPTION| {ex.Message}");
            }
            return false;
        }

        private async Task<bool> PlaceOrderClose(BinancePositionV3 pos)
        {
            var side = pos.PositionAmt < 0 ? OrderSide.Sell : OrderSide.Buy;
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
                    var account = await Binance_GetAccountInfo();
                    if (account != null)
                    {
                        balance = $"|Balance: {Math.Round(account.WalletBalance, 1)}$";
                    }

                    await _teleService.SendMessage(_idUser, $"[CLOSE - {side.ToString().ToUpper()}({winloss}: {rate}%)|Binance] {pos.Symbol}|TP: {pos.MarkPrice}|Entry: {pos.EntryPrice}{balance}");
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
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.PlaceOrderClose|EXCEPTION| {ex.Message}");
            }
            await _teleService.SendMessage(_idUser, $"[Binance] Không thể đóng lệnh {side}: {pos.Symbol}!");
            return false;
        }

        private async Task InitSymbolConfig()
        {
            try
            {
                var dt = DateTime.UtcNow;
                if(!_lSymConfig.Any()
                    || (dt.Hour == 0 && dt.Minute == 0))
                {
                    _lSymConfig.Clear();
                    var info = await StaticVal.BinanceInstance().UsdFuturesApi.ExchangeData.GetExchangeInfoAsync();
                    foreach (var item in info.Data.Symbols.Where(x => x.QuoteAsset == "USDT"))
                    {
                        try
                        {
                            var price = item.Filters.FirstOrDefault(x => x.FilterType == SymbolFilterType.Price) as BinanceSymbolPriceFilter;
                            var priceCount = price.TickSize.ToString("#.##########").Split('.').Last().Length;
                            _lSymConfig.Add(new clsSymbol
                            {
                                s = item.Name,
                                quan = item.QuantityPrecision,
                                price = priceCount
                            });
                        }
                        catch(Exception ex)
                        {
                            _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.InitSymbol|EXCEPTION| {ex.Message}");
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BinanceService.InitSymbol|EXCEPTION| {ex.Message}");
            }
        }

        private List<long> _lIgnoreCode = new List<long>
        {
        };

        private static List<clsSymbol> _lSymConfig = new List<clsSymbol>();
    }
    public class clsSymbol
    {
        public string s { get; set; }
        public int price { get; set; }
        public int quan { get; set; }
    }
}
