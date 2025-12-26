using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;
using CoinUtilsPr;
using CoinUtilsPr.DAL;
using CoinUtilsPr.DAL.Entity;
using CoinUtilsPr.Model;
using CryptoExchange.Net.Objects;
using MongoDB.Driver;
using Newtonsoft.Json;
using SharpCompress.Common;
using Skender.Stock.Indicators;
using System.Collections.Concurrent;
using TradePr.Utils;

namespace TradePr.Service
{
    public interface ITakerService
    {
        Task<BybitAssetBalance> Bybit_GetAccountInfo();
        Task Bybit_Trade();
        Task Bybit_Signal();
    }
    public class TakerService : ITakerService
    {
        private readonly ILogger<TakerService> _logger;
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private readonly IConfigDataRepo _configRepo;
        private readonly IProRepo _proRepo;
        private const long _idUser = 1066022551;
        private readonly int _exchange = (int)EExchange.Bybit;
        private const decimal _margin = 10;
        private const int SONEN_NAMGIU = 24;
        private const decimal SL_RATE = 1.5m;
        public TakerService(ILogger<TakerService> logger,
                          IAPIService apiService, ITeleService teleService,
                          IConfigDataRepo configRepo, IProRepo proRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _teleService = teleService;
            _configRepo = configRepo;
            _proRepo = proRepo;
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
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_GetAccountInfo|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task Bybit_Trade()
        {
            try
            {
                var now = DateTime.UtcNow;

                await Bybit_TakeProfit();
                if (now.Minute % 15 == 0)
                {
                    var interval = EInterval.M15;
                    await Bybit_ENTRY(interval);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|MMService.Bybit_Trade|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Bybit_TakeProfit()
        {
            try
            {
                var dt = DateTime.UtcNow;
                var builder = Builders<Pro>.Filter;
                var lentity = _proRepo.GetByFilter(builder.And(
                        builder.Eq(x => x.status, 0)
                    ));

                WebCallResult<BybitResponse<BybitPosition>> pos = null;
                try
                {
                    pos = await StaticTrade.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, settleAsset: "USDT");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|GetPositionsAsync:| {ex.Message}");
                    return;
                }
                if (pos is null
                    || pos.Data is null
                    || pos.Data.List is null
                    || !pos.Data.List.Any())
                {
                    if (lentity.Any())//Case STOPLOSS
                    {
                        foreach (var item in lentity)
                        {
                            item.status = -1;
                            item.is_sl = true;
                            _proRepo.Update(item);

                            await _teleService.SendMessage(_idUser, $"[STOPLOSS|Bybit] {item.s}|SL: {item.sl_price}");
                        }
                    }
                    return;
                }
                #region TP
                foreach (var item in pos.Data.List)
                {
                    var side = item.Side == PositionSide.Sell ? OrderSide.Sell : OrderSide.Buy;
                    var place = lentity.FirstOrDefault(x => x.s == item.Symbol);
                    //Lỗi gì đó ko lưu lại đc log
                    if (place is null)
                    {
                        //Console.WriteLine($"Place null");
                        //await PlaceOrderClose(item);
                        break;
                    }
                    var quotes = await _apiService.GetData_Binance(item.Symbol, (EInterval)place.interval);
                    if (quotes is null || !quotes.Any())
                    {
                        //Console.WriteLine($"Quotes null");
                        continue;
                    }
                    var count = quotes.Count(x => x.Date > place.entity.Date);
                    //DONG LENH
                    if (count > SONEN_NAMGIU)
                    {
                        await PlaceOrderClose(item);
                        break;
                    }
                    var last = quotes.LastOrDefault();
                    if (last is null)
                    {
                        continue;
                    }
                    var lbb = quotes.GetBollingerBands().ToList();
                    var lrsi = quotes.GetRsi().ToList();
                    var lma9 = lrsi.GetSma(9).ToList();

                    var cur = quotes[^2];
                    var bb_cur = lbb[^2];
                    //STOP LOSS
                    if (last.Low <= place.sl_price)
                    {
                        await PlaceOrderClose(item);
                        place.status = -1;
                        place.is_sl = true;
                        _proRepo.Update(place);
                        break;
                    }


                    // TP1 – bán 40%
                    if (!place.is_tp1
                        && cur.Close > place.entity.Close
                        && cur.Close >= (decimal)bb_cur.Sma.Value)
                    {
                        await PlaceOrderClose(item, 0.4m);
                        place.is_tp1 = true;
                        _proRepo.Update(place);
                    }
                    // TP2 – bán 40%
                    if (place.is_tp1
                        && !place.is_tp2
                        && cur.High > (decimal)bb_cur.UpperBand.Value)
                    {
                        await PlaceOrderClose(item, 0.6m);
                        place.is_tp2 = true;
                        _proRepo.Update(place);
                    }
                    // TP3 – bán toàn bộ
                    if (place.is_tp1
                        && lrsi[^2].Rsi < lma9[^2].Sma)
                    {
                        await PlaceOrderClose(item);
                        place.is_tp3 = true;
                        _proRepo.Update(place);
                    }

                    //
                    var takevolumes = await _apiService.GetBuySellRate(item.Symbol, (EInterval)place.interval);
                    var mes = takevolumes.GetOut();
                    if (!string.IsNullOrEmpty(mes))
                    {
                        await _teleService.SendMessage(_idUser, $"[TAKEVOLUME|Bybit] [{item.Symbol}](https://www.binance.com/vi/futures/{item.Symbol})|{mes}");
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TakeProfit|EXCEPTION| {ex.Message}");
            }
        }

        private ConcurrentDictionary<string, TakerVolumneBuySellDTO> _takerStates = new ConcurrentDictionary<string, TakerVolumneBuySellDTO>();
        private readonly SemaphoreSlim _placeOrderSemaphore = new SemaphoreSlim(1, 1); // Đảm bảo PlaceOrder được gọi tuần tự
        
        public async Task Bybit_ENTRY(EInterval interval)
        {
            try
            {
                WebCallResult<BybitResponse<BybitPosition>> pos = null;
                try
                {
                    pos = await StaticTrade.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, settleAsset: "USDT");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|Bybit_ENTRY:| {ex.Message}");
                    return;
                }

                if (pos != null &&
                    pos.Data != null &&
                    pos.Data.List.Any())
                    return;

                var lSym = new List<string>
                {
                    "SOLUSDT",
                    "LUNA2USDT",
                    "QUSDT",
                    "ACEUSDT",
                    "MIRAUSDT",
                    "YGGUSDT",
                    "FUSDT",
                    "SCRUSDT",
                    "ENSOUSDT",
                    "VINEUSDT",
                    "BRUSDT",
                    "NOMUSDT",
                    "DOODUSDT"
                };

                // Chạy song song để tối ưu thời gian xử lý
                var tasks = lSym.Select(sym => ProcessSymbolEntry(sym, interval));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|MMService.Bybit_ENTRY|EXCEPTION| {ex.Message}");
            }
        }

        private async Task ProcessSymbolEntry(string sym, EInterval interval)
        {
            try
            {
                //gia
                var now = DateTime.Now;
                var quotes = await _apiService.GetData_Binance(sym, interval);
                if (quotes is null || !quotes.Any())
                    return;

                var last = quotes.Last();
                if (last.Volume <= 0)
                    return;
                quotes.Remove(last);

                var takevolumes = await _apiService.GetBuySellRate(sym, interval);
                if (takevolumes is null || !takevolumes.Any())
                    return;

                if (_takerStates.ContainsKey(sym) && _takerStates[sym] != null)
                {
                    var currentTaker = _takerStates[sym];
                    if (quotes.Count(x => x.Date > ((long)currentTaker.timestamp).UnixTimeStampMinisecondToDateTime()) > 5)
                    {
                        // Reset state nếu quá hạn
                        _takerStates.TryRemove(sym, out _);
                        currentTaker = null;
                    }

                    if (currentTaker != null)
                    {
                        var entry = quotes.GetEntry();
                        if (entry.Item1 == 0)
                        {
                            _takerStates.TryRemove(sym, out _);
                        }
                        else if (entry.Item1 > 0)
                        {
                            _takerStates.TryRemove(sym, out _);
                            var sl_price = Math.Min(last.Open, last.Close) * (1 - SL_RATE / 100);
                            var eEntry = new Pro
                            {
                                entity = entry.Item2,
                                s = sym,
                                ratio = 100,
                                interval = (int)interval,
                                sl_price = sl_price,
                            };
                            
                            // Sử dụng semaphore để đảm bảo PlaceOrder được gọi tuần tự
                            await _placeOrderSemaphore.WaitAsync();
                            try
                            {
                                await PlaceOrder(eEntry, now);
                                _proRepo.InsertOne(eEntry);
                                Console.WriteLine($"====> ENTRY: {entry.Item2.Date.ToString("dd/MM/yyyy HH:mm")}");
                            }
                            finally
                            {
                                _placeOrderSemaphore.Release();
                            }
                            return;
                        }
                    }
                }

                // Logic tìm signal mới nếu chưa có hoặc đã reset
                if (!_takerStates.ContainsKey(sym))
                {
                    var signal = takevolumes.GetSignal(quotes);
                    if (signal != null)
                    {
                        _takerStates.TryAdd(sym, signal);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|MMService.ProcessSymbolEntry|INPUT: {sym}|EXCEPTION| {ex.Message}");
            }
        }

        public async Task Bybit_Signal()
        {
            try
            {
                var now = DateTime.UtcNow;
                var lConfig = _configRepo.GetAll();
                var signal = lConfig.First(x => x.ex == _exchange && x.op == (int)EOption.SignalNotify);
                if (signal != null && signal.status > 0)
                {
                    if (now.Minute % 5 == 0)
                    {
                        await Bybit_ENTRY_SIGNAL(EInterval.M5);
                    }
                    //if (now.Minute % 15 == 0)
                    //{
                    //    await Bybit_ENTRY_SIGNAL(EInterval.M15);
                    //}
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|MMService.Bybit_Signal|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Bybit_ENTRY_SIGNAL(EInterval interval)
        {
            try
            {
                var lSym = new List<string>
                {
                    "SOLUSDT",
                    "SUIUSDT",
                    "BTCUSDT",
                    "ETHUSDT",
                    "BNBUSDT"
                };
                foreach (var sym in lSym)
                {
                    try
                    {
                        //gia
                        var quotes = await _apiService.GetData_Binance(sym, interval);
                        if (quotes is null || !quotes.Any())
                            continue;

                        var last = quotes.Last();
                        if (last.Volume <= 0)
                            continue;

                        var entry = quotes.GetEntry(interval);
                        if (entry == null) continue;
                        var mes = $"{sym}|{interval}|SIGNAL({entry.ratio}%)|{entry.entity.Date.ToString("dd/MM/yyyy HH:mm")}";
                        await _teleService.SendMessage(_idUser, mes);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|MMService.Bybit_ENTRY|INPUT: {sym}|EXCEPTION| {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|MMService.Bybit_ENTRY|EXCEPTION| {ex.Message}");
            }
        }

        private async Task<EError> PlaceOrder(Pro entry, DateTime dt)
        {
            try
            {
                var now = DateTime.Now;
                if((now - dt).TotalSeconds > 60)
                {
                    _logger.LogInformation($"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|MMService.PlaceOrder|SKIP OLD SIGNAL| {JsonConvert.SerializeObject(entry)}");
                    return EError.Error;
                }

                var pos = await StaticTrade.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, settleAsset: "USDT");
                if (pos != null &&
                    pos.Data != null &&
                    pos.Data.List.Count() > 2)
                {
                    _logger.LogInformation($"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|MMService.PlaceOrder|EXIST POSITION| {JsonConvert.SerializeObject(entry)}");
                    return EError.Error;
                }

                Console.WriteLine($"{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}|BYBIT PlaceOrder: {JsonConvert.SerializeObject(entry)}");
                var curTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                var account = await Bybit_GetAccountInfo();
                if (account == null)
                {
                    await _teleService.SendMessage(_idUser, "[ERROR_bybit] Không lấy được thông tin tài khoản");
                    return EError.Error;
                }
                //Lay Unit tu database
                var lConfig = _configRepo.GetAll();
                var lInfo = await StaticTrade.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, entry.s);
                var info = lInfo.Data.List.FirstOrDefault();
                if (info == null) return EError.Error;
                var tronGia = (int)info.PriceScale;
                var tronSL = info.LotSizeFilter.QuantityStep;

                var max = lConfig.First(x => x.ex == _exchange && x.op == (int)EOption.Max);
                var val = account.WalletBalance.Value > (decimal)max.value ? (decimal)max.value : account.WalletBalance.Value;

                var soluong = (val * _margin * entry.ratio / 100) / entry.entity.Close;
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

                if (soluong <= 0)
                    return EError.Error;

                var res = await StaticTrade.ByBitInstance().V5Api.Trading.PlaceOrderAsync(Category.Linear,
                                                                                        entry.s,
                                                                                        side: OrderSide.Buy,
                                                                                        type: NewOrderType.Market,
                                                                                        reduceOnly: false,
                                                                                        quantity: soluong);
                Thread.Sleep(500);
                //nếu lỗi return
                if (!res.Success)
                {
                    if (!StaticVal._lIgnoreCode.Any(x => x == res.Error.Code))
                    {
                        await _teleService.SendMessage(_idUser, $"[ERROR_Bybit] |{entry.s}|{res.Error.Code}:{res.Error.Message}");
                    }
                    return EError.Error;
                }

                var resPosition = await StaticTrade.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, entry.s);
                Thread.Sleep(500);
                if (!resPosition.Success)
                {
                    await _teleService.SendMessage(_idUser, $"[ERROR_Bybit] |{entry.s}|{res.Error.Code}:{res.Error.Message}");
                    return EError.Error;
                }

                if (resPosition.Data.List.Any())
                {
                    decimal sl = Math.Round(entry.sl_price, tronGia);
                    //STOP LOSS
                    var resSL = await StaticTrade.ByBitInstance().V5Api.Trading.SetTradingStopAsync(Category.Linear, entry.s, PositionIdx.OneWayMode, stopLoss: sl);
                    Thread.Sleep(500);
                    if (!resSL.Success)
                    {
                        await _teleService.SendMessage(_idUser, $"[ERROR_Bybit_SL] |{entry.s}|{resSL.Error.Code}:{resSL.Error.Message}");
                    }

                    var mes = $"[ACTION - {OrderSide.Buy.ToString().ToUpper()}|Bybit({(EInterval)entry.interval})] [{entry.s}](https://www.binance.com/vi/futures/{entry.s})|ENTRY(Rate: {entry.ratio}%): {entry.entity.Close}|SL: {sl}";
                    await _teleService.SendMessage(_idUser, mes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|MMService.PlaceOrder|EXCEPTION| {ex.Message}");
                return EError.Error;
            }
            return EError.Success;
        }

        private async Task<bool> PlaceOrderClose(BybitPosition pos, decimal sluong_vithe = 1)
        {
            Console.WriteLine($"[CLOSE] {JsonConvert.SerializeObject(pos)}");
            var side = pos.Side == PositionSide.Sell ? OrderSide.Sell : OrderSide.Buy;
            var CLOSE_side = pos.Side == PositionSide.Sell ? OrderSide.Buy : OrderSide.Sell;

            try
            {
                var soluong = Math.Abs(pos.Quantity) * sluong_vithe;
                if (sluong_vithe != 1)
                {
                    var lInfo = await StaticTrade.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, pos.Symbol);
                    var info = lInfo.Data.List.FirstOrDefault();
                    if (info == null)
                    {
                        _logger.LogInformation($"Khong lay duoc thong tin COIN: {pos.Symbol}");
                        return false;
                    }
                    ;
                    var tronSL = info.LotSizeFilter.QuantityStep;
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
                }

                var res = await StaticTrade.ByBitInstance().V5Api.Trading.PlaceOrderAsync(Category.Linear,
                                                                                        pos.Symbol,
                                                                                        side: CLOSE_side,
                                                                                        type: NewOrderType.Market,
                                                                                        quantity: soluong);
                if (res.Success)
                {
                    //var resCancel = await StaticTrade.ByBitInstance().V5Api.Trading.CancelAllOrderAsync(Category.Linear, pos.Symbol);

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

                    await _teleService.SendMessage(_idUser, $"[CLOSE - {side.ToString().ToUpper()}({winloss}: {rate}%)|Bybit] [{pos.Symbol}](https://www.binance.com/vi/futures/{pos.Symbol})|TP: {pos.MarkPrice}|Entry: {pos.AveragePrice}{balance}");
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
    }
}
