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
    public interface ILast_BybitService
    {
        Task Detect_SOS();
        Task Detect_SOSReal();
        Task Detect_Entry();
        Task Detect_TakeProfit();
    }
    public class Last_BybitService : ILast_BybitService
    {
        private readonly ILogger<Last_BybitService> _logger;
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private readonly ISymbolRepo _symRepo;
        private readonly IConfigDataRepo _configRepo;
        private readonly ILast_SOSRepo _sosRepo;
        private readonly int _exchange = (int)EExchange.Bybit;
        private const long _idUser = 1066022551;
        private readonly int _HOUR = 30;
        private readonly decimal _SL_RATE = 0.05m;
        private const decimal _margin = 10;
        public Last_BybitService(ILogger<Last_BybitService> logger,
                            IAPIService apiService, ITeleService teleService, ISymbolRepo symRepo,
                            IConfigDataRepo configRepo, ITradingRepo tradeRepo, IPrepareRepo preRepo, ILast_SOSRepo sosRepo, IEntrySOSRepo entryRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _teleService = teleService;
            _symRepo = symRepo;
            _configRepo = configRepo;
            _sosRepo = sosRepo;
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
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|Last_BybitService.Bybit_GetAccountInfo|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        //4h 1 lần
        public async Task Detect_SOS()
        {
            try
            {
                var time = (int)DateTimeOffset.UtcNow.AddDays(-4).ToUnixTimeSeconds();
                var builder = Builders<SOSDTO>.Filter;
                var lsos = _sosRepo.GetByFilter(builder.And(
                    builder.Gte(x => x.t, time)
                ));
                var lTake = new List<string>
                {
                    "AAVEUSDT",
"ADAUSDT",
"AERGOUSDT",
"AI16ZUSDT",
"AINUSDT",
"AIUSDT",
"ALICEUSDT",
"APTUSDT",
"ARKMUSDT",
"ARKUSDT",
"ARPAUSDT",
"ASRUSDT",
"ASTERUSDT",
"AUDIOUSDT",
"AVAXUSDT",
"AWEUSDT",
"B2USDT",
"BABYUSDT",
"BANANAUSDT",
"BBUSDT",
"BCHUSDT",
"BERAUSDT",
"BLESSUSDT",
"BMTUSDT",
"BSVUSDT",
"BTCUSDT",
"CATIUSDT",
"CFXUSDT",
"CGPTUSDT",
"CKBUSDT",
"COSUSDT",
"COWUSDT",
"CROSSUSDT",
"CVCUSDT",
"DEGENUSDT",
"DIAUSDT",
"DOGSUSDT",
"DOLOUSDT",
"DRIFTUSDT",
"EGLDUSDT",
"ENJUSDT",
"ENSUSDT",
"ESPORTSUSDT",
"ETHUSDT",
"FIDAUSDT",
"FIOUSDT",
"FLOWUSDT",
"FORTHUSDT",
"GASUSDT",
"GLMUSDT",
"GRIFFAINUSDT",
"GUSDT",
"HAEDALUSDT",
"HFTUSDT",
"HIGHUSDT",
"HIPPOUSDT",
"HIVEUSDT",
"HOOKUSDT",
"HUSDT",
"ICPUSDT",
"IDUSDT",
"INITUSDT",
"INJUSDT",
"INUSDT",
"IOSTUSDT",
"IOTXUSDT",
"IOUSDT",
"JELLYJELLYUSDT",
"JOEUSDT",
"KAVAUSDT",
"KERNELUSDT",
"KNCUSDT",
"LIGHTUSDT",
"LTCUSDT",
"MANAUSDT",
"MANTAUSDT",
"MBOXUSDT",
"MELANIAUSDT",
"MEWUSDT",
"MOCAUSDT",
"MOVEUSDT",
"MOVRUSDT",
"MTLUSDT",
"MUSDT",
"NEARUSDT",
"NEOUSDT",
"NKNUSDT",
"NMRUSDT",
"NXPCUSDT",
"OBOLUSDT",
"OGNUSDT",
"OLUSDT",
"OMUSDT",
"ONEUSDT",
"ONGUSDT",
"OPUSDT",
"ORCAUSDT",
"OXTUSDT",
"PAXGUSDT",
"PENGUUSDT",
"PEOPLEUSDT",
"PERPUSDT",
"PHAUSDT",
"PIPPINUSDT",
"POLYXUSDT",
"POWRUSDT",
"PROMPTUSDT",
"PROMUSDT",
"PUNDIXUSDT",
"QUICKUSDT",
"RESOLVUSDT",
"REZUSDT",
"RLCUSDT",
"RONINUSDT",
"ROSEUSDT",
"RPLUSDT",
"RUNEUSDT",
"SANDUSDT",
"SCRTUSDT",
"SEIUSDT",
"SKLUSDT",
"SKYUSDT",
"SLPUSDT",
"SOLUSDT",
"SOONUSDT",
"SPELLUSDT",
"SPXUSDT",
"STEEMUSDT",
"STORJUSDT",
"SUSHIUSDT",
"SWARMSUSDT",
"SXPUSDT",
"SXTUSDT",
"SYRUPUSDT",
"SYSUSDT",
"TAOUSDT",
"THETAUSDT",
"TOKENUSDT",
"TONUSDT",
"TOWNSUSDT",
"TRUMPUSDT",
"TRUUSDT",
"UMAUSDT",
"VANRYUSDT",
"VELODROMEUSDT",
"VETUSDT",
"VFYUSDT",
"VINEUSDT",
"WCTUSDT",
"YFIUSDT",
"YGGUSDT",
"ZECUSDT",
"ZEREBROUSDT",
                };
                foreach (var item in lTake)
                {
                    try
                    {
                        var lDat = await _apiService.GetData_Binance(item, EInterval.H1);
                        if (lDat is null)
                            continue;

                        var itemSOS = lDat.SkipLast(1).LAST_Mutation();
                        if (itemSOS != null)
                        {
                            if (lsos.Any(x => x.s == item 
                                        && x.sos.Date >= itemSOS.Date))
                            {
                                continue;
                            }
                            _sosRepo.InsertOne(new SOSDTO
                            {
                                s = item,
                                t = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                                sos = itemSOS,
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|Last_BybitService.Detect_SOS|EXCEPTION|INPUT: {item}| {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|Last_BybitService.Detect_SOS|EXCEPTION| {ex.Message}");
            }
        }

        //1h 1 lần
        public async Task Detect_SOSReal()
        {
            try
            {
                var time = (int)DateTimeOffset.UtcNow.AddHours(-10).ToUnixTimeSeconds();
                var builder = Builders<SOSDTO>.Filter;
                var lsos = _sosRepo.GetByFilter(builder.And(
                    builder.Gte(x => x.t, time),
                    builder.Eq(x => x.sos_real, null),
                    builder.Eq(x => x.status, 0)
                ));

                var prev = string.Empty;
                foreach (var itemSOS in lsos.OrderBy(x => x.s).OrderByDescending(x => x.sos.Date))
                {
                    if (itemSOS.s.Equals(prev))
                    {
                        _sosRepo.UpdateOneField("status", -1, builder.And(
                                                                    builder.Eq(x => x.s, itemSOS.s),
                                                                    builder.Gte(x => x.t, time),
                                                                    builder.Eq(x => x.sos_real, null),
                                                                    builder.Eq(x => x.status, 0)
                                                                ));
                        //itemSOS.status = -1;
                        //_sosRepo.Update(itemSOS);
                        continue;
                    }
                    prev = itemSOS.s;

                    var l1H = await _apiService.GetData_Binance(itemSOS.s, EInterval.H1);
                    if (l1H is null)
                        continue;

                    var countCheck = l1H.SkipLast(1).Count(x => x.Date > itemSOS.sos.Date);
                    if(countCheck > 6)
                    {
                        _sosRepo.UpdateOneField("status", -10, builder.And(
                                                                   builder.Eq(x => x.s, itemSOS.s),
                                                                   builder.Gte(x => x.t, time),
                                                                   builder.Eq(x => x.sos_real, null),
                                                                   builder.Eq(x => x.status, 0)
                                                               ));
                        //itemSOS.status = -10;
                        //_sosRepo.Update(itemSOS);
                        continue;
                    }

                    var item3 = l1H.SkipLast(1).Last();
                    var item2 = l1H.SkipLast(2).Last();
                    var item1 = l1H.SkipLast(3).Last();
                    var itemDetect = itemSOS.LAST_DetectTopBOT(item1, item2, item3);
                    if (itemDetect != null)
                    {
                        _sosRepo.UpdateOneField("sos_real", itemDetect, builder.And(
                                                                   builder.Eq(x => x.s, itemSOS.s),
                                                                   builder.Gte(x => x.t, time),
                                                                   builder.Eq(x => x.sos_real, null),
                                                                   builder.Eq(x => x.status, 0)
                                                               ));
                        //itemSOS.sos_real = itemDetect;
                        //_sosRepo.Update(itemSOS);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|Last_BybitService.Detect_SOSReal|EXCEPTION| {ex.Message}");
            }
        }
        //1h 1 lần
        public async Task Detect_Entry()
        {
            try
            {
                var time = (int)DateTimeOffset.UtcNow.AddHours(-20).ToUnixTimeSeconds();
                var builder = Builders<SOSDTO>.Filter;
                var lsos = _sosRepo.GetByFilter(builder.And(
                    builder.Gte(x => x.t, time),
                    builder.Ne(x => x.sos_real, null),
                    builder.Eq(x => x.signal, null),
                    builder.Eq(x => x.status, 0)
                ));

                var lBuy = new List<SOSDTO>();
                var prev = string.Empty;
                foreach (var itemSOS in lsos.OrderBy(x => x.s).OrderByDescending(x => x.sos.Date))
                {
                    if (itemSOS.s.Equals(prev))
                    {
                        _sosRepo.UpdateOneField("status", -2, builder.And(
                                                                    builder.Eq(x => x.s, itemSOS.s),
                                                                    builder.Gte(x => x.t, time),
                                                                    builder.Ne(x => x.sos_real, null),
                                                                    builder.Eq(x => x.signal, null),
                                                                    builder.Eq(x => x.status, 0)
                                                               ));
                        //itemSOS.status = -2;
                        //_sosRepo.Update(itemSOS);
                        continue;
                    }
                    prev = itemSOS.s;

                    var l1H = await _apiService.GetData_Binance(itemSOS.s, EInterval.H1);
                    if (l1H is null)
                        continue;

                    var countCheck = l1H.SkipLast(1).Count(x => x.Date > itemSOS.sos_real.Date);
                    if (countCheck > 15)
                    {
                        _sosRepo.UpdateOneField("status", -20, builder.And(
                                                                  builder.Eq(x => x.s, itemSOS.s),
                                                                  builder.Gte(x => x.t, time),
                                                                  builder.Ne(x => x.sos_real, null),
                                                                  builder.Eq(x => x.signal, null),
                                                                  builder.Eq(x => x.status, 0)
                                                             ));
                        //itemSOS.status = -20;
                        //_sosRepo.Update(itemSOS);
                        continue;
                    }

                    var item3 = l1H.SkipLast(1).Last();
                    var item2 = l1H.SkipLast(2).Last();
                    var item1 = l1H.SkipLast(3).Last();
                    var itemDetect = itemSOS.LAST_DetectEntry(item1, item2, item3);
                    if (itemDetect != null)
                    {
                        itemSOS.signal = itemDetect;
                        itemSOS.entry = item3;
                        
                        //Kiểm tra volume của đáy 2 phải nhỏ hơn 2 lần đáy 1 
                        //Trong khoảng 2 đáy ko được có nến vol đỏ vượt đáy 1
                        //Đáy 2 không được vượt MA20

                        //entry tới upper > 2%
                        //đáy 2 cách đáy 1 tối thiểu 5 nến
                        //đáy 2 <= 1/2 (H + L) đáy 1

                        var max = Math.Max(itemSOS.sos.Volume, itemSOS.sos_real.Volume);
                        var lFilter = l1H.Where(x => x.Open >= x.Close && x.Date > itemSOS.sos_real.Date && x.Date <= itemSOS.signal.Date);
                        if (!lFilter.Any())
                        {
                            _sosRepo.UpdateOneField("status", -200, builder.And(
                                                                 builder.Eq(x => x.s, itemSOS.s),
                                                                 builder.Gte(x => x.t, time),
                                                                 builder.Ne(x => x.sos_real, null),
                                                                 builder.Eq(x => x.signal, null),
                                                                 builder.Eq(x => x.status, 0)
                                                            ));
                            //itemSOS.status = -200;
                            //_sosRepo.Update(itemSOS);
                            continue;
                        }
                        var maxRange = lFilter.Max(x => x.Volume);
                        var lbb = l1H.GetBollingerBands();
                        var bb_signal = lbb.First(x => x.Date == itemSOS.signal.Date);
                        var bb_entry = lbb.First(x => x.Date == itemSOS.entry.Date);
                        var rateEntry = Math.Round(100 * (-1 + (decimal)bb_entry.UpperBand / itemSOS.entry.Close), 2);
                        var count2Day = l1H.Count(x => x.Date >= itemSOS.sos_real.Date && x.Date < itemSOS.signal.Date);
                        var avgPrice1 = 0.5m * (Math.Max(itemSOS.sos.High, itemSOS.sos_real.High) + Math.Min(itemSOS.sos.Low, itemSOS.sos_real.Low));

                        //Kiểm tra nếu 20 nến liền trước không có nến nào vượt MA20 thì bỏ qua
                        var lCheck20 = l1H.Where(x => x.Date < item3.Date).TakeLast(20);
                        var isPassCheck20 = false;
                        foreach (var item in lCheck20)
                        {
                            var bb = lbb.First(x => x.Date == item.Date);
                            if(item.Close > (decimal)bb.Sma)
                            {
                                isPassCheck20 = true;
                                break;
                            }
                        }
                        if(!isPassCheck20)
                        {
                            _sosRepo.UpdateOneField("status", -2000, builder.And(
                                                                builder.Eq(x => x.s, itemSOS.s),
                                                                builder.Gte(x => x.t, time),
                                                                builder.Ne(x => x.sos_real, null),
                                                                builder.Eq(x => x.signal, null),
                                                                builder.Eq(x => x.status, 0)
                                                           ));
                            //itemSOS.status = -200;
                            //_sosRepo.Update(itemSOS);
                            continue;
                        }

                        if (max > 2 * itemSOS.signal.Volume
                            && maxRange < max
                            && itemSOS.signal.Close < (decimal)bb_signal.Sma
                            && itemSOS.entry.Close < (decimal)bb_entry.Sma
                            && rateEntry > 2
                            && count2Day >= 5
                            && itemSOS.signal.Close <= avgPrice1)
                        {
                            itemSOS.status = 1;
                            itemSOS.en = l1H.Last().Close;
                            itemSOS.side = (int)OrderSide.Buy;
                            itemSOS.t = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            _sosRepo.Update(itemSOS);
                            //BUY
                            if(itemSOS.en < itemSOS.sos_real.Close)
                            {
                                await PlaceOrder(itemSOS);
                            }
                            else
                            {
                                lBuy.Add(itemSOS);
                            }
                        }
                    }
                }

                foreach (var item in lBuy.OrderByDescending(x => x.en < x.sos.Open))
                {
                    await PlaceOrder(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|Last_BybitService.Detect_Entry|EXCEPTION| {ex.Message}");
            }
        }

        //1p 1 lần
        public async Task Detect_TakeProfit()
        {
            try
            {
                var pos = await StaticTrade.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, settleAsset: "USDT");
                if (pos is null
                    || pos.Data is null
                    || !pos.Data.List.Any())
                    return;

                var dt = DateTime.UtcNow;
                var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                var lEntry = _sosRepo.GetByFilter(Builders<SOSDTO>.Filter.Eq(x => x.status, 1));

                #region TP
                foreach (var item in pos.Data.List)
                {
                    var side = item.Side == PositionSide.Sell ? OrderSide.Sell : OrderSide.Buy;

                    var curTime = (dt - item.UpdateTime.Value).TotalHours;

                    var place = lEntry.FirstOrDefault(x => x.s == item.Symbol && x.status == 1);
                    //Lỗi gì đó ko lưu lại đc log
                    if (place is null)
                    {
                        Console.WriteLine($"Place null");
                        await PlaceOrderClose(item);
                        continue;
                    }
                    //Hết thời gian
                    var dTime = (now - place.t) / 3600;
                    if (curTime >= _HOUR || dTime >= _HOUR)
                    {
                        Console.WriteLine($"Het thoi gian: curTime: {curTime}, dTime: {dTime}");
                        await PlaceOrderClose(item);
                        continue;
                    }

                    var lDat = await _apiService.GetData_Binance(item.Symbol, EInterval.H1);
                    if (lDat is null)
                        continue;

                    var lbb = lDat.GetBollingerBands();
                    var cur = lDat.Last();
                    var last = lDat.SkipLast(1).Last();

                    if (cur.Close <= (decimal)place.sl)
                    {
                        Console.WriteLine($"last.Close <= (decimal)place.sl|{last.Close}|{place.sl}");
                        await PlaceOrderClose(item);
                        continue;
                    }

                    var bb = lbb.First(x => x.Date == last.Date);
                    if(last.Close > (decimal)bb.UpperBand)
                    {
                        Console.WriteLine($"last.Close > UpperBand|{last.Close}|{bb.Sma}");
                        await PlaceOrderClose(item);
                    }
                }
                #endregion

                #region Clean DB
                var lAll = _sosRepo.GetAll().Where(x => x.status == 1);
                foreach (var item in lAll)
                {
                    var exist = pos.Data.List.FirstOrDefault(x => x.Symbol == item.s);
                    if (exist is null)
                    {
                        item.status = -300;
                        _sosRepo.Update(item);
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TakeProfit|EXCEPTION| {ex.Message}");
            }
        }

        private async Task<EError> PlaceOrder(SOSDTO entity)
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

                ////Nếu trong 4 tiếng gần nhất giảm quá 10% thì không mua mới
                //var lIncome = await StaticTrade.ByBitInstance().V5Api.Account.GetTransactionHistoryAsync(limit: 200);
                //if (lIncome == null || !lIncome.Success)
                //{
                //    await _teleService.SendMessage(_idUser, "[ERROR_bybit] Không lấy được lịch sử thay đổi số dư");
                //    return EError.Error;
                //}
                //var lIncomeCheck = lIncome.Data.List.Where(x => x.TransactionTime >= DateTime.UtcNow.AddHours(-4));
                //if (lIncomeCheck.Count() >= 2)
                //{
                //    var first = lIncomeCheck.First();//giá trị mới nhất
                //    var last = lIncomeCheck.Last();//giá trị cũ nhất
                //    if (first.CashBalance > 0)
                //    {
                //        var rate = 1 - last.CashBalance.Value / first.CashBalance.Value;
                //        var div = last.CashBalance.Value - first.CashBalance.Value;

                //        if ((double)div * 10 > 0.6 * max.value)
                //            return EError.Error;

                //        if (rate <= -0.13m)
                //            return EError.Error;
                //    }
                //}

                var pos = await StaticTrade.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, settleAsset: "USDT");
                if (pos.Data.List.Count() >= thread.value)
                {
                    entity.status = -500;
                    _sosRepo.Update(entity);
                    return EError.MaxThread;
                }    

                if (pos.Data.List.Any(x => x.Symbol == entity.s))
                    return EError.Error;

                var lInfo = await StaticTrade.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, entity.s);
                var info = lInfo.Data.List.FirstOrDefault();
                if (info == null) return EError.Error;
                var tronGia = (int)info.PriceScale;
                var tronSL = info.LotSizeFilter.QuantityStep;

                var side = (OrderSide)entity.side;
                var SL_side = side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
                var direction = side == OrderSide.Buy ? TriggerDirection.Fall : TriggerDirection.Rise;

                var soluong = (decimal)max.value / entity.en;
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
                        sl = Math.Round(first.MarkPrice.Value * (1 - _SL_RATE), tronGia);
                    }
                    else
                    {
                        sl = Math.Round(first.MarkPrice.Value * (1 + _SL_RATE), tronGia);
                    }
                    //STOP LOSS
                    var resSL = await StaticTrade.ByBitInstance().V5Api.Trading.SetTradingStopAsync(Category.Linear, first.Symbol, PositionIdx.OneWayMode, stopLoss: sl);
                    Thread.Sleep(500);
                    if (!resSL.Success)
                    {
                        await _teleService.SendMessage(_idUser, $"[ERROR_Bybit_SL] |{first.Symbol}|{resSL.Error.Code}:{resSL.Error.Message}");
                    }

                    //Save + Print
                    var entry = Math.Round(first.AveragePrice.Value, tronGia);
                    entity.en = entry;
                    entity.sl = sl;
                    _sosRepo.Update(entity);

                    var mes = $"[ACTION - {side.ToString().ToUpper()}|Bybit] {first.Symbol}|ENTRY: {entry}|SL: {entity.sl}";
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
            Console.WriteLine($"[CLOSE] {JsonConvert.SerializeObject(pos)}");
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

                    var builder = Builders<SOSDTO>.Filter;
                    var filter = builder.And(
                        builder.Eq(x => x.s, pos.Symbol),
                        builder.Eq(x => x.status, 1),
                        builder.Gte(x => x.t, (int)DateTimeOffset.UtcNow.AddDays(-4).ToUnixTimeSeconds())
                    );
                    var exists = _sosRepo.GetEntityByFilter(filter);
                    if (exists != null)
                    {
                        exists.rate = rate;
                        exists.status = 2;
                        _sosRepo.Update(exists);
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
