using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;
using CoinUtilsPr;
using CoinUtilsPr.DAL;
using CoinUtilsPr.DAL.Entity;
using MongoDB.Driver;
using Newtonsoft.Json;
using TradePr.Utils;

namespace TradePr.Service
{
    public interface IBybitSocketService
    {
        //void Bybit_Socket();
    }
    public class BybitSocketService : IBybitSocketService
    {
        //private const long _idUser = 1066022551;
        //private readonly ILogger<WebSocketService> _logger;
        //private readonly IAPIService _apiService;
        //private readonly ITeleService _teleService;
        //private readonly IBinanceService _binanceService;
        //private readonly IPrepareRepo _prepareRepo;
        //private readonly IConfigDataRepo _configRepo;
        //private readonly IPlaceOrderTradeRepo _placeRepo;
        //private static Dictionary<string, DateTime> _dicRes = new Dictionary<string, DateTime>();
        //private object _locker = new object();
        //private readonly decimal _SL_RATE = 0.025m; //MA20 là 0.017
        //private const decimal _margin = 10;
        //private readonly int _exchange = (int)EExchange.Bybit;

        //public BybitSocketService(ILogger<WebSocketService> logger, IAPIService apiService, ITeleService teleService, IPrepareRepo prepareRepo, IBinanceService binanceService, IConfigDataRepo configRepo)
        //{
        //    _logger = logger;
        //    _apiService = apiService;
        //    _teleService = teleService;
        //    _prepareRepo = prepareRepo;
        //    _binanceService = binanceService;
        //    _configRepo = configRepo;
        //}

        //private void RemoveValue(Prepare val)
        //{
        //    //Monitor.Enter(_locker);
        //    //StaticTrade._lPrepare.Remove(val);
        //    //Monitor.Exit(_locker);
        //}
        //private async Task PositionMarket(Prepare val, decimal lastPrice)
        //{
        //    //try
        //    //{
        //    //    RemoveValue(val);
        //    //    //return;
        //    //    //action
        //    //    var res = await _binanceService.PlaceOrder(new SignalBase
        //    //    {
        //    //        s = val.s,
        //    //        Side = val.Side
        //    //    }, lastPrice);
        //    //    //if (res is null)
        //    //    //    return;

        //    //    //await _teleService.SendMessage(_idUser, $"[ACTION - {((Binance.Net.Enums.OrderSide)val.Side).ToString().ToUpper()}|Binance] {val.s}|ENTRY: {val.Entry}");
        //    //    //Console.WriteLine($"[ACTION - {((Binance.Net.Enums.OrderSide)val.Side).ToString().ToUpper()}|Binance] {val.s}|ENTRY: {val.Entry}");
        //    //    //val.Entry_Real = res.priceEntry;
        //    //    //val.SL_Real = res.priceStoploss;
        //    //    //val.entryDate = DateTime.Now;
        //    //    //val.entryTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
        //    //    //val.Status = 1;
        //    //    //_prepareRepo.Update(val);
        //    //    //RemoveValue(val);
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    _logger.LogError(ex, $"WebSocketService.PositionMarket|EXCEPTION| {ex.Message}");
        //    //}
        //}
        //public void Bybit_Socket()
        //{
        //    try
        //    {
        //        var now = DateTime.Now;
        //        var builderFilter = Builders<Prepare>.Filter;
        //        var lShort = _prepareRepo.GetByFilter(builderFilter.And(
        //            builderFilter.Eq(x => x.ex, _exchange),
        //            builderFilter.Eq(x => x.side, (int)OrderSide.Sell)
        //        ));

        //        //var liquid = StaticTrade.BybitSocketInstance().V5LinearApi.un   .SubscribeToTickerUpdatesAsync(async (data) =>
        //        //{
        //        //    try
        //        //    {
        //        //        Monitor.Enter(_locker);
        //        //        var dat = data.Data.Where(x => StaticTrade._lPrepare.Any(y => y.s == x.Symbol));
        //        //        Monitor.Exit(_locker);
        //        //        foreach (var item in dat)
        //        //        {
        //        //            var val = StaticTrade._lPrepare.FirstOrDefault(x => x.s == item.Symbol);
        //        //            if (val is null)
        //        //                continue;

        //        //            if ((DateTime.Now - val.detectDate).TotalMinutes >= 15)
        //        //            {
        //        //                RemoveValue(val);
        //        //                continue;
        //        //            }

        //        //            if (val.Side == (int)Binance.Net.Enums.OrderSide.Buy)
        //        //            {
        //        //                if ((double)item.LastPrice <= val.Entry)
        //        //                {
        //        //                    //action
        //        //                    await PositionMarket(val, item.LastPrice);
        //        //                }
        //        //            }
        //        //            else
        //        //            {
        //        //                if ((double)item.LastPrice >= val.Entry)
        //        //                {
        //        //                    //action
        //        //                    await PositionMarket(val, item.LastPrice);
        //        //                }
        //        //            }
        //        //        }
        //        //    }
        //        //    catch (Exception ex)
        //        //    {
        //        //        _logger.LogError(ex, $"WebSocketService.BinanceLiquid|EXCEPTION(Detail)| {ex.Message}");
        //        //    }
        //        //});
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"WebSocketService.BinanceLiquid|EXCEPTION| {ex.Message}");
        //    }
        //}

        //private async Task<BybitAssetBalance> Bybit_GetAccountInfo()
        //{
        //    try
        //    {
        //        var resAPI = await StaticTrade.ByBitInstance().V5Api.Account.GetBalancesAsync(AccountType.Unified);
        //        return resAPI?.Data?.List?.FirstOrDefault().Assets.FirstOrDefault(x => x.Asset == "USDT");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitSocketService.Bybit_GetAccountInfo|EXCEPTION| {ex.Message}");
        //    }
        //    return null;
        //}
        //private async Task<bool> PlaceOrder(SignalBase entity)
        //{
        //    try
        //    {
        //        int _exchange = (int)EExchange.Bybit;
        //        Console.WriteLine($"BYBIT PlaceOrder: {JsonConvert.SerializeObject(entity)}");
        //        var curTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
        //        var account = await Bybit_GetAccountInfo();
        //        if (account == null)
        //        {
        //            await _teleService.SendMessage(_idUser, "[ERROR_bybit] Không lấy được thông tin tài khoản");
        //            return false;
        //        }
        //        //Lay Unit tu database
        //        var lConfig = _configRepo.GetAll();
        //        var max = lConfig.First(x => x.ex == _exchange && x.op == (int)EOption.Max);
        //        var thread = lConfig.First(x => x.ex == _exchange && x.op == (int)EOption.Thread);

        //        if ((account.WalletBalance.Value - account.TotalPositionInitialMargin.Value) * _margin <= (decimal)max.value)
        //            return false;

        //        //Nếu trong 4 tiếng gần nhất giảm quá 10% thì không mua mới
        //        var lIncome = await StaticTrade.ByBitInstance().V5Api.Account.GetTransactionHistoryAsync(limit: 200);
        //        if (lIncome == null || !lIncome.Success)
        //        {
        //            await _teleService.SendMessage(_idUser, "[ERROR_bybit] Không lấy được lịch sử thay đổi số dư");
        //            return false;
        //        }
        //        var lIncomeCheck = lIncome.Data.List.Where(x => x.TransactionTime >= DateTime.UtcNow.AddHours(-4));
        //        if (lIncomeCheck.Count() >= 2)
        //        {
        //            var first = lIncomeCheck.First();//giá trị mới nhất
        //            var last = lIncomeCheck.Last();//giá trị cũ nhất
        //            if (first.CashBalance > 0)
        //            {
        //                var rate = 1 - last.CashBalance.Value / first.CashBalance.Value;
        //                var div = last.CashBalance.Value - first.CashBalance.Value;

        //                if ((double)div * 10 > 0.6 * max.value)
        //                    return false;

        //                if (rate <= -0.13m)
        //                    return false;
        //            }
        //        }

        //        var pos = await StaticTrade.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, settleAsset: "USDT");
        //        if (pos.Data.List.Count() >= thread.value)
        //            return false;

        //        if (pos.Data.List.Any(x => x.Symbol == entity.s))
        //            return false;

        //        var lInfo = await StaticTrade.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, entity.s);
        //        var info = lInfo.Data.List.FirstOrDefault();
        //        if (info == null) return false;
        //        var tronGia = (int)info.PriceScale;
        //        var tronSL = info.LotSizeFilter.QuantityStep;

        //        var side = (OrderSide)entity.Side;
        //        var SL_side = side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
        //        var direction = side == OrderSide.Buy ? TriggerDirection.Fall : TriggerDirection.Rise;

        //        decimal soluong = (decimal)max.value / entity.quote.Close;
        //        if (tronSL == 1)
        //        {
        //            soluong = Math.Round(soluong);
        //        }
        //        else if (tronSL == 10)
        //        {
        //            soluong = Math.Round(soluong);
        //            var odd = soluong % 10;
        //            soluong -= odd;
        //        }
        //        else if (tronSL == 100)
        //        {
        //            soluong = Math.Round(soluong);
        //            var odd = soluong % 100;
        //            soluong -= odd;
        //        }
        //        else if (tronSL == 1000)
        //        {
        //            soluong = Math.Round(soluong);
        //            var odd = soluong % 1000;
        //            soluong -= odd;
        //        }
        //        else
        //        {
        //            var lamtronSL = tronSL.ToString("#.##########").Split('.').Last().Length;
        //            soluong = Math.Round(soluong, lamtronSL);
        //        }

        //        var res = await StaticTrade.ByBitInstance().V5Api.Trading.PlaceOrderAsync(Category.Linear,
        //                                                                                entity.s,
        //                                                                                side: side,
        //                                                                                type: NewOrderType.Market,
        //                                                                                reduceOnly: false,
        //                                                                                quantity: soluong);
        //        Thread.Sleep(500);
        //        //nếu lỗi return
        //        if (!res.Success)
        //        {
        //            if (!_lIgnoreCode.Any(x => x == res.Error.Code))
        //            {
        //                await _teleService.SendMessage(_idUser, $"[ERROR_Bybit] |{entity.s}|{res.Error.Code}:{res.Error.Message}");
        //            }
        //            return false;
        //        }

        //        var resPosition = await StaticTrade.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, entity.s);
        //        Thread.Sleep(500);
        //        if (!resPosition.Success)
        //        {
        //            await _teleService.SendMessage(_idUser, $"[ERROR_Bybit] |{entity.s}|{res.Error.Code}:{res.Error.Message}");
        //            return false;
        //        }

        //        if (resPosition.Data.List.Any())
        //        {
        //            var first = resPosition.Data.List.First();

        //            decimal sl = 0;
        //            if (side == OrderSide.Buy)
        //            {
        //                sl = Math.Round(first.MarkPrice.Value * (decimal)(1 - _SL_RATE), tronGia);
        //            }
        //            else
        //            {
        //                sl = Math.Round(first.MarkPrice.Value * (decimal)(1 + _SL_RATE), tronGia);
        //            }
        //            res = await StaticTrade.ByBitInstance().V5Api.Trading.PlaceOrderAsync(Category.Linear,
        //                                                                                    first.Symbol,
        //                                                                                    side: SL_side,
        //                                                                                    type: NewOrderType.Market,
        //                                                                                    triggerPrice: sl,
        //                                                                                    triggerDirection: direction,
        //                                                                                    triggerBy: TriggerType.LastPrice,
        //                                                                                    quantity: soluong,
        //                                                                                    timeInForce: TimeInForce.GoodTillCanceled,
        //                                                                                    reduceOnly: true,
        //                                                                                    stopLossOrderType: OrderType.Limit,
        //                                                                                    stopLossTakeProfitMode: StopLossTakeProfitMode.Partial,
        //                                                                                    stopLossTriggerBy: TriggerType.LastPrice,
        //                                                                                    stopLossLimitPrice: sl);
        //            Thread.Sleep(500);
        //            if (!res.Success)
        //            {
        //                await _teleService.SendMessage(_idUser, $"[ERROR_Bybit_SL] |{first.Symbol}|{res.Error.Code}:{res.Error.Message}");
        //                return false;
        //            }
        //            //Print
        //            var entry = Math.Round(first.AveragePrice.Value, tronGia);

        //            var mes = $"[ACTION - {side.ToString().ToUpper()}|Bybit] {first.Symbol}({entity.rank})|ENTRY: {entry}";
        //            await _teleService.SendMessage(_idUser, mes);

        //            _placeRepo.InsertOne(new PlaceOrderTrade
        //            {
        //                ex = _exchange,
        //                s = first.Symbol,
        //                time = DateTime.Now
        //            });
        //            return true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitSocketService.PlaceOrder|EXCEPTION| {ex.Message}");
        //    }
        //    return false;
        //}

        //private List<long> _lIgnoreCode = new List<long>
        //{
        //    110007
        //};
    }
}