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
    public interface IBybitWyckoffService
    {
        Task<BybitAssetBalance> Bybit_GetAccountInfo();
        Task Bybit_Trade();
        Task Bybit_Signal();
    }
    public class BybitWyckoffService : IBybitWyckoffService
    {
        private readonly ILogger<BybitWyckoffService> _logger;
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private readonly ISymbolRepo _symRepo;
        private readonly IConfigDataRepo _configRepo;
        private readonly ITradingRepo _tradeRepo;
        private readonly IPrepareRepo _preRepo;
        private readonly int _exchange = (int)EExchange.Bybit;
        private const long _idUser = 1066022551;
        private readonly int _HOUR = 90;
        private readonly decimal _SL_RATE = 0.05m;
        private const decimal _margin = 10;
        public BybitWyckoffService(ILogger<BybitWyckoffService> logger,
                            IAPIService apiService, ITeleService teleService, ISymbolRepo symRepo,
                            IConfigDataRepo configRepo, ITradingRepo tradeRepo, IPrepareRepo preRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _teleService = teleService;
            _symRepo = symRepo;
            _configRepo = configRepo;
            _tradeRepo = tradeRepo;
            _preRepo = preRepo;
        }

        public async Task<BybitAssetBalance> Bybit_GetAccountInfo()
        {
            try
            {
                var resAPI = await StaticTrade.ByBitInstance().V5Api.Account.GetBalancesAsync(AccountType.Unified);
                return resAPI?.Data?.List?.FirstOrDefault().Assets.FirstOrDefault(x => x.Asset == "USDT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitWyckoffService.Bybit_GetAccountInfo|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task Bybit_Trade()
        {
            try
            {
                await Bybit_TakeProfit();
                await Bybit_Entry();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_Entry|EXCEPTION| {ex.Message}");
            }
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
                foreach (var item in pos.Data.List)
                {
                    var side = item.Side == PositionSide.Sell ? OrderSide.Sell : OrderSide.Buy;

                    var curTime = (dt - item.UpdateTime.Value).TotalHours;

                    var builder = Builders<Trading>.Filter;
                    var place = _tradeRepo.GetEntityByFilter(builder.And(
                        builder.Eq(x => x.ex, _exchange),
                        builder.Eq(x => x.s, item.Symbol),
                        builder.Eq(x => x.Status, 0)
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
                    if (curTime >= _HOUR || dTime >= _HOUR)
                    {
                        Console.WriteLine($"Het thoi gian: curTime: {curTime}, dTime: {dTime}");
                        await PlaceOrderClose(item);
                        continue;
                    }

                    var l1H = await _apiService.GetData_Bybit_1H(item.Symbol);
                    var entry = new Quote
                    {
                        Date = place.Date,
                        Close = (decimal)place.Entry
                    };
                    var res = entry.IsWyckoffOut(l1H);
                    if (res.Item1)
                    {
                        await PlaceOrderClose(item);
                        continue;
                    }
                }
                #endregion

                #region Clean DB
                var lAll = _tradeRepo.GetAll().Where(x => x.Status == 0);
                foreach (var item in lAll)
                {
                    var exist = pos.Data.List.FirstOrDefault(x => x.Symbol == item.s);
                    if (exist is null)
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

        private async Task Bybit_Entry()
        {
            try
            {
                var builder = Builders<Prepare>.Filter;
                var lPrepare = _preRepo.GetByFilter(builder.And(
                    builder.Eq(x => x.ex, _exchange),
                    builder.Eq(x => x.side, (int)OrderSide.Buy),
                    builder.Eq(x => x.status, (int)EStatus.Active)
                ));

                foreach (var item in lPrepare)
                {
                    try
                    {
                        var l1H = await _apiService.GetData_Bybit_1H(item.s);
                        var lCheck = l1H.Where(x => x.Date > item.signal.Date);
                        var count = lCheck.Count();
                        if(count <= 2)
                        {
                            continue;
                        }
                        if(count > 12)
                        {
                            //update prepare
                            item.status = (int)EStatus.Disable;
                            _preRepo.Update(item);
                            continue;
                        }

                        var flag = false;
                        var lbb = l1H.GetBollingerBands();
                        foreach (var itemCheck in lCheck.Skip(2))
                        {
                            var bb = lbb.First(x => x.Date == itemCheck.Date);
                            if (flag
                                && itemCheck.Close > (decimal)bb.Sma.Value
                                && itemCheck.Close < item.signal.Close)
                            {
                                //buy
                                var last = l1H.Last();
                                var res = await PlaceOrder(new Trading
                                {
                                    ex = _exchange,
                                    s = item.s,
                                    d = (int)DateTimeOffset.Now.AddHours(-1).ToUnixTimeSeconds(),
                                    Date = last.Date,
                                    Side = (int)OrderSide.Buy,
                                    Entry = (double)last.Close,
                                });
                                //update prepare
                                item.status = (int)EStatus.Disable;
                                _preRepo.Update(item);
                                break;
                            }
                            if (itemCheck.Low < (decimal)bb.Sma.Value)
                            {
                                flag = true;
                            }
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_Entry|EXCEPTION| {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TradeRSI_LONG|EXCEPTION| {ex.Message}");
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
                        var rate = 1 - last.CashBalance.Value / first.CashBalance.Value;
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
                if (tronSL == 1)
                {
                    soluong = Math.Round(soluong);
                }
                else if (tronSL == 10)
                {
                    soluong = Math.Round(soluong);
                    var odd = soluong % 10;
                    soluong -= odd;
                }
                else if (tronSL == 100)
                {
                    soluong = Math.Round(soluong);
                    var odd = soluong % 100;
                    soluong -= odd;
                }
                else if (tronSL == 1000)
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

        public async Task Bybit_Signal()
        {
            try
            {
                var lTake = new List<string>
                {
                    "BTCUSDT",
                    "ETHUSDT",
                    "XRPUSDT",
                    "BNBUSDT",
                    "SOLUSDT",
                    "TRXUSDT",
                    "ADAUSDT",
                    "LINKUSDT",
                    "XLMUSDT",
                    "BCHUSDT",
                    "AVAXUSDT",
                    "CROUSDT",
                    "HBARUSDT",
                    "LTCUSDT",
                    "TONUSDT",
                    "DOTUSDT",
                    "UNIUSDT",
                    "SUIUSDT",
                    "XMRUSDT",
                    "ETCUSDT",
                    "DOGEUSDT",
                    "SHIBUSDT",
                    "HYPEUSDT",
                    "QUICKUSDT"
                };

                foreach (var item in lTake)
                {
                    try
                    {
                        var l1H = await _apiService.GetData_Binance(item, EInterval.H1);
                        //Detect 
                        var lSOS = new List<SOSDTO>();
                        var count = l1H.Count();
                        for (int i = 1; i < count; i++)
                        {
                            var lDat = l1H.Take(i);
                            var itemSOS = lDat.IsWyckoff_Prepare();
                            if (itemSOS != null)
                            {
                                if (!lSOS.Any(x => x.sos.Date == itemSOS.sos.Date))
                                {
                                    lSOS.Add(itemSOS);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|{item}|BybitService.Bybit_Signal|EXCEPTION| {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_Signal|EXCEPTION| {ex.Message}");
            }
        }
    }
}
