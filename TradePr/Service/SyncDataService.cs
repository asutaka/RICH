using Bybit.Net.Enums;
using CoinUtilsPr;
using MongoDB.Driver;
using Skender.Stock.Indicators;
using TradePr.DAL;
using TradePr.DAL.Entity;
using TradePr.Utils;

namespace TradePr.Service
{
    public interface ISyncDataService
    {
        Task UnitSetting();

        void ClearData();

        Task Binance_LONG();
        Task Bybit_LONG();

        Task Binance_SHORT();
        Task Bybit_SHORT();
    }
    public class SyncDataService : ISyncDataService
    {
        private readonly ILogger<SyncDataService> _logger;
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private readonly IConfigDataRepo _configRepo;
        private readonly ISymbolRepo _symRepo;
        private readonly IBinanceService _binanceService;
        private readonly IBybitService _bybitService;
        private const long _idUser = 1066022551;
        private const int _TAKE = 50;//số bản ghi mỗi loại
        private Dictionary<string, List<Quote>> _dData_Bybit = new Dictionary<string, List<Quote>>();
        public SyncDataService(ILogger<SyncDataService> logger,
                           IAPIService apiService, ITeleService teleService, ISymbolRepo symRepo, IConfigDataRepo configRepo,
                           IBinanceService binanceService, IBybitService bybitService)
        {
            _logger = logger;
            _apiService = apiService;
            _teleService = teleService;
            _symRepo = symRepo;
            _configRepo = configRepo;
            _binanceService = binanceService;
            _bybitService = bybitService;
        }

        private async Task<List<Quote>> GetData_Bybit(string symbol)
        {
            try
            {
                if (_dData_Bybit.ContainsKey(symbol))
                    return _dData_Bybit[symbol].ToList();

                var l15m = new List<Quote>();
                var last = new Quote();

                var lData20 = await _apiService.GetData_Bybit(symbol, EInterval.M15, DateTimeOffset.Now.AddDays(-20).ToUnixTimeMilliseconds());
                Thread.Sleep(100);
                l15m.AddRange(lData20.Where(x => x.Date > last.Date));
                last = l15m.Last();

                var lData10 = await _apiService.GetData_Bybit(symbol, EInterval.M15, DateTimeOffset.Now.AddDays(-10).ToUnixTimeMilliseconds());
                Thread.Sleep(100);
                l15m.AddRange(lData10.Where(x => x.Date > last.Date));
                if (l15m is null || !l15m.Any())
                    return null;

                if (_dData_Bybit.ContainsKey(symbol))
                {
                    _dData_Bybit[symbol] = l15m;
                }
                else
                {
                    _dData_Bybit.Add(symbol, l15m);
                }

                return l15m.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|SyncDataService.GetData_Bybit|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public void ClearData()
        {
            _dData_Bybit.Clear();
        }

        public async Task UnitSetting()
        {
            try
            {
                var dt = DateTime.Now;
                var lConfig = _configRepo.GetAll();
                var lConfigUnit = lConfig.Where(x => x.op == (int)EOption.Max);
                //
                //BINANCE
                var configBinance = lConfigUnit.First(x => x.ex == (int)EExchange.Binance);
                var binance = await _binanceService.Binance_GetAccountInfo();
                if (binance != null)
                {
                    var cur = binance.WalletBalance;
                    var unit = Math.Round(cur / 5) * 10;
                    if(unit > 70)
                    {
                        unit = 70;
                    }
                    configBinance.value = (double)unit;
                    configBinance.t = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                    _configRepo.Update(configBinance);
                }
                //BYBIT
                var configBybit = lConfigUnit.First(x => x.ex == (int)EExchange.Bybit);
                var bybit = await _bybitService.Bybit_GetAccountInfo();
                if (bybit != null)
                {
                    var cur = bybit.WalletBalance.Value;
                    var unit = Math.Round(cur / 5) * 10;
                    if (unit > 70)
                    {
                        unit = 70;
                    }
                    configBybit.value = (double)unit;
                    configBybit.t = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                    _configRepo.Update(configBybit);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SyncDataService.UnitSetting|EXCEPTION| {ex.Message}");
            }
        }

        public async Task Binance_LONG()
        {
            try
            {
                var start = DateTime.Now;
                var exchange = (int)EExchange.Binance;
                var lAll = await StaticVal.BinanceInstance().UsdFuturesApi.CommonFuturesClient.GetSymbolsAsync();
                var lUsdt = lAll.Data.Where(x => x.Name.EndsWith("USDT")).Select(x => x.Name);
                var lTake = lUsdt.ToList();
                decimal SL_RATE = 2.5m;
                int hour = 4;
                decimal rateProfit_Min = 2.5m;
                decimal rateProfit_Max = 7m;

                var lModel = new List<clsData>();
                var lResult = new List<clsResult>();

                var winTotal = 0;
                var lossTotal = 0;

                foreach (var item in lTake)
                {
                    if (item.Contains('-'))
                        continue;

                    var winCount = 0;
                    var lossCount = 0;
                    try
                    {
                        var lData15m = new List<Quote>();
                        var last = new Quote();
                        var lData20 = await _apiService.GetData_Binance(item, EInterval.M15, DateTimeOffset.Now.AddDays(-20).ToUnixTimeMilliseconds());
                        Thread.Sleep(200);
                        lData15m.AddRange(lData20.Where(x => x.Date > last.Date));
                        last = lData15m.Last();

                        var lData10 = await _apiService.GetData_Binance(item, EInterval.M15, DateTimeOffset.Now.AddDays(-10).ToUnixTimeMilliseconds());
                        Thread.Sleep(200);
                        lData15m.AddRange(lData10.Where(x => x.Date > last.Date));
                        var lbb = lData15m.GetBollingerBands();
                        var lrsi = lData15m.GetRsi();
                        var lVol = lData15m.Select(x => new Quote
                        {
                            Date = x.Date,
                            Close = x.Volume
                        }).ToList();
                        var lMaVol = lVol.GetSma(20);

                        DateTime dtFlag = DateTime.MinValue;
                        foreach (var ma20 in lbb)
                        {
                            try
                            {
                                if (ma20.Sma is null
                                    || dtFlag >= ma20.Date)
                                    continue;

                                var entity_Sig = lData15m.First(x => x.Date == ma20.Date);
                                var rsi_Sig = lrsi.First(x => x.Date == ma20.Date);
                                var maVol_Sig = lMaVol.First(x => x.Date == ma20.Date);

                                var entity_Pivot = lData15m.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));
                                var rsi_Pivot = lrsi.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));
                                var bb_Pivot = lbb.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));

                                if (entity_Sig.Close >= entity_Sig.Open
                                    || rsi_Sig.Rsi > 35
                                    || entity_Sig.Low >= (decimal)ma20.LowerBand.Value
                                    || entity_Sig.Close - (decimal)ma20.LowerBand.Value >= (decimal)ma20.Sma.Value - entity_Sig.Close
                                    )
                                    continue;

                                if (entity_Sig.Volume < (decimal)(maVol_Sig.Sma.Value * 1.5))
                                    continue;

                                if (entity_Pivot is null
                                   || rsi_Pivot.Rsi > 35 || rsi_Pivot.Rsi < 25
                                   || entity_Pivot.Low >= (decimal)bb_Pivot.LowerBand.Value
                                   || entity_Pivot.High >= (decimal)bb_Pivot.Sma.Value
                                   || (entity_Pivot.Low >= entity_Sig.Low && entity_Pivot.High <= entity_Sig.High)
                                   )
                                    continue;

                                //độ dài nến hiện tại
                                var rateCur = Math.Abs((entity_Sig.Open - entity_Sig.Close) / (entity_Sig.High - entity_Sig.Low));
                                if (rateCur > (decimal)0.8)
                                {
                                    //check độ dài nến pivot
                                    var isValid = Math.Abs(entity_Pivot.Open - entity_Pivot.Close) >= Math.Abs(entity_Sig.Open - entity_Sig.Close);
                                    if (isValid)
                                        continue;
                                }

                                var rateVol = Math.Round(entity_Pivot.Volume / entity_Sig.Volume, 1);
                                if (rateVol > (decimal)0.6) //Vol hiện tại phải nhỏ hơn hoặc bằng 0.6 lần vol của nến liền trước
                                    continue;

                                var checkTop = lData15m.Where(x => x.Date <= entity_Pivot.Date).ToList().IsExistTopB();
                                if (!checkTop.Item1)
                                    continue;

                                var eClose = lData15m.FirstOrDefault(x => x.Date >= entity_Pivot.Date.AddHours(hour));
                                if (eClose is null)
                                    continue;

                                var rateBB = (decimal)(Math.Round(100 * (-1 + bb_Pivot.UpperBand.Value / bb_Pivot.LowerBand.Value)) - 1);
                                if(rateBB < rateProfit_Min - 1)
                                {
                                    continue;
                                }    
                                else if(rateBB > rateProfit_Max)
                                {
                                    rateBB = rateProfit_Max;
                                }

                                var lClose = lData15m.Where(x => x.Date > entity_Pivot.Date && x.Date <= entity_Pivot.Date.AddHours(hour));
                                var isChotNon = false;
                                foreach (var itemClose in lClose)
                                {
                                    var ma = lbb.First(x => x.Date == itemClose.Date);
                                    if (itemClose.High > (decimal)ma.UpperBand)//do something
                                    {
                                        eClose = itemClose;
                                        break;
                                    }


                                    if (isChotNon
                                        && itemClose.Close < (decimal)ma.Sma.Value
                                        && itemClose.Close <= itemClose.Open
                                        && itemClose.Close >= entity_Pivot.Close)
                                    {
                                        eClose = itemClose;
                                        break;
                                    }

                                    if (itemClose.High >= (decimal)ma.Sma.Value)
                                    {
                                        isChotNon = true;
                                    }

                                    var rateCheck = Math.Round(100 * (-1 + itemClose.High / entity_Pivot.Close), 1); //chốt khi lãi > 10%
                                    if (rateCheck > rateBB)
                                    {
                                        var close = entity_Pivot.Close * (decimal)(1 + rateBB / 100);
                                        itemClose.Close = close;
                                        eClose = itemClose;
                                        break;
                                    }
                                }

                                dtFlag = eClose.Date;
                                var rate = Math.Round(100 * (-1 + eClose.Close / entity_Pivot.Close), 1);
                                var lRange = lData15m.Where(x => x.Date >= entity_Pivot.Date.AddMinutes(15) && x.Date <= eClose.Date);
                                var maxH = lRange.Max(x => x.High);
                                var minL = lRange.Min(x => x.Low);

                                var winloss = "W";
                                if (rate <= (decimal)0)
                                {
                                    winloss = "L";
                                }

                                var maxSL = Math.Round(100 * (-1 + minL / entity_Pivot.Close), 1);
                                if (maxSL <= -SL_RATE)
                                {
                                    rate = -SL_RATE;
                                    winloss = "L";
                                }

                                if (winloss == "W")
                                {
                                    rate = Math.Abs(rate);
                                    winCount++;
                                }
                                else
                                {
                                    rate = -Math.Abs(rate);
                                    lossCount++;
                                }

                                //lRate.Add(rate);
                                lModel.Add(new clsData
                                {
                                    s = item,
                                    Date = entity_Sig.Date,
                                    Rate = rate,
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"SyncDataService.Binance_LONG|EXCEPTION| {ex.Message}");
                            }

                        }

                        if (winCount + lossCount <= 1)
                            continue;

                        var rateRes = Math.Round(((decimal)winCount / (winCount + lossCount)), 2);
                        var sumRate = lModel.Where(x => x.s == item).Sum(x => x.Rate);
                        var count = lModel.Count(x => x.s == item);
                        var items = lModel.Where(x => x.s == item);
                        var perRate = Math.Round((float)sumRate / count, 1);
                        //Special
                        if (rateRes < (decimal)0.5
                          || perRate <= 0.7)
                        {
                            lModel = lModel.Except(items).ToList();
                            continue;
                        }

                        var realWin = 0;
                        foreach (var model in items)
                        {
                            if (model.Rate > (decimal)0.5)
                                realWin++;
                        }

                        winTotal += winCount;
                        lossTotal += lossCount;
                        winCount = 0;
                        lossCount = 0;

                        var winrate = Math.Round((double)realWin / count, 1);

                        var mes = $"{item}\t\t\t| W/Total: {realWin}/{count} = {winrate}%|Rate: {sumRate}%|Per: {perRate}%";
                        //Console.WriteLine(mes);

                        lResult.Add(new clsResult
                        {
                            s = item,
                            Win = realWin,
                            Winrate = winrate,
                            Perate = perRate,
                            Mes = mes
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                }

                var lRes = lResult.OrderByDescending(x => x.Winrate).ThenByDescending(x => x.Win).ThenByDescending(x => x.Perate).Take(_TAKE).ToList();
                if (lRes.Count() < _TAKE - 15)
                    return;

                //Delete
                var builder = Builders<Symbol>.Filter;
                _symRepo.DeleteMany(builder.And(
                    builder.Eq(x => x.ex, exchange),
                    builder.Eq(x => x.ty, (int)Binance.Net.Enums.OrderSide.Buy)
                ));

                var rank = 1;
                foreach (var item in lRes)
                {
                    Console.WriteLine(item.Mes);
                    _symRepo.InsertOne(new Symbol
                    {
                        s = item.s,
                        ex = exchange,
                        ty = (int)Binance.Net.Enums.OrderSide.Buy,
                        rank = rank++
                    });
                }

                Console.WriteLine($"Tong: {lModel.Sum(x => x.Rate)}%|W/L: {winTotal}/{lossTotal}");

                var end = DateTime.Now;
                Console.WriteLine($"TotalTime: {(end - start).TotalSeconds}");
                await _teleService.SendMessage(_idUser, $"[Đã đồng bộ] BINANCE LONG");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SyncDataService.Binance_LONG|EXCEPTION| {ex.Message}");
            }
        }

        public async Task Bybit_LONG()
        {
            try
            {
                var start = DateTime.Now;
                var exchange = (int)EExchange.Bybit;
                var lAll = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, limit: 1000);
                var lUsdt = lAll.Data.List.Where(x => x.QuoteAsset == "USDT" && !x.Name.StartsWith("1000")).Select(x => x.Name);
                var lTake = lUsdt.ToList();
                decimal SL_RATE = 2.5m;
                int hour = 4;
                decimal rateProfit_Min = 2.5m;
                decimal rateProfit_Max = 7m;

                var lModel = new List<clsData>();
                var lResult = new List<clsResult>();

                var winTotal = 0;
                var lossTotal = 0;

                foreach (var item in lTake)
                {
                    if (item.Contains('-'))
                        continue;
                    var winCount = 0;
                    var lossCount = 0;
                    try
                    {
                        var lData15m = await GetData_Bybit(item);
                        var lbb = lData15m.GetBollingerBands();
                        var lrsi = lData15m.GetRsi();
                        var lVol = lData15m.Select(x => new Quote
                        {
                            Date = x.Date,
                            Close = x.Volume
                        }).ToList();
                        var lMaVol = lVol.GetSma(20);

                        DateTime dtFlag = DateTime.MinValue;
                        foreach (var ma20 in lbb)
                        {
                            try
                            {
                                if (ma20.Sma is null
                                    || dtFlag >= ma20.Date)
                                    continue;

                                var entity_Sig = lData15m.First(x => x.Date == ma20.Date);
                                var rsi_Sig = lrsi.First(x => x.Date == ma20.Date);
                                var maVol_Sig = lMaVol.First(x => x.Date == ma20.Date);

                                var entity_Pivot = lData15m.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));
                                var rsi_Pivot = lrsi.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));
                                var bb_Pivot = lbb.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));

                                if (entity_Sig.Close >= entity_Sig.Open
                                    || rsi_Sig.Rsi > 35
                                    || entity_Sig.Low >= (decimal)ma20.LowerBand.Value
                                    || entity_Sig.Close - (decimal)ma20.LowerBand.Value >= (decimal)ma20.Sma.Value - entity_Sig.Close
                                    )
                                    continue;

                                if (entity_Sig.Volume < (decimal)(maVol_Sig.Sma.Value * 1.5))
                                    continue;

                                if (entity_Pivot is null
                                   || rsi_Pivot.Rsi > 35 || rsi_Pivot.Rsi < 25
                                   || entity_Pivot.Low >= (decimal)bb_Pivot.LowerBand.Value
                                   || entity_Pivot.High >= (decimal)bb_Pivot.Sma.Value
                                   //|| (entity_Pivot.Low >= entity_Sig.Low && entity_Pivot.High <= entity_Sig.High)
                                   )
                                    continue;

                                //độ dài nến hiện tại
                                var rateCur = Math.Abs((entity_Sig.Open - entity_Sig.Close) / (entity_Sig.High - entity_Sig.Low));
                                if (rateCur > (decimal)0.8)
                                {
                                    //check độ dài nến pivot
                                    var isValid = Math.Abs(entity_Pivot.Open - entity_Pivot.Close) >= Math.Abs(entity_Sig.Open - entity_Sig.Close);
                                    if (isValid)
                                        continue;
                                }

                                var rateVol = Math.Round(entity_Pivot.Volume / entity_Sig.Volume, 1);
                                if (rateVol > (decimal)0.6) //Vol hiện tại phải nhỏ hơn hoặc bằng 0.6 lần vol của nến liền trước
                                    continue;

                                var checkTop = lData15m.Where(x => x.Date <= entity_Pivot.Date).ToList().IsExistTopB();
                                if (!checkTop.Item1)
                                    continue;

                                #region Thêm xử lý
                                var isPass = false;
                                var lCheck = lData15m.Where(x => x.Date > entity_Pivot.Date).Take(8);
                                foreach (var check in lCheck)
                                {
                                    var rateCheck = Math.Round(100 * (-1 + check.Low / entity_Pivot.Close), 1);
                                    if (rateCheck <= -1.5m)
                                    {
                                        var dodainen = Math.Abs(Math.Round(100 * (-1 + entity_Pivot.Close * 0.985m / check.Open), 1));
                                        if (dodainen >= SL_RATE)
                                            continue;

                                        entity_Pivot = check;
                                        entity_Pivot.Close = entity_Pivot.Close * 0.985m;
                                        isPass = true; break;
                                    }
                                }
                                if (!isPass)
                                    continue;
                                #endregion

                                var eClose = lData15m.FirstOrDefault(x => x.Date >= entity_Pivot.Date.AddHours(hour));
                                if (eClose is null)
                                    continue;

                                var rateBB = (decimal)(Math.Round(100 * (-1 + bb_Pivot.UpperBand.Value / bb_Pivot.LowerBand.Value)) - 1);
                                if (rateBB < rateProfit_Min - 1)
                                {
                                    continue;
                                }
                                else if (rateBB > rateProfit_Max)
                                {
                                    rateBB = rateProfit_Max;
                                }

                                var lClose = lData15m.Where(x => x.Date > entity_Pivot.Date && x.Date <= entity_Pivot.Date.AddHours(hour));
                                var isChotNon = false;
                                foreach (var itemClose in lClose)
                                {
                                    var ma = lbb.First(x => x.Date == itemClose.Date);
                                    if (itemClose.High > (decimal)ma.UpperBand)//do something
                                    {
                                        eClose = itemClose;
                                        break;
                                    }

                                    if(isChotNon
                                        && itemClose.Close < (decimal)ma.Sma.Value
                                        && itemClose.Close <= itemClose.Open
                                        && itemClose.Close >= entity_Pivot.Close)
                                    {
                                        eClose = itemClose;
                                        break;
                                    }

                                    if(itemClose.High >= (decimal)ma.Sma.Value)
                                    {
                                        isChotNon = true; 
                                    }

                                    var rateCheck = Math.Round(100 * (-1 + itemClose.High / entity_Pivot.Close), 1); //chốt khi lãi > 10%
                                    if (rateCheck > rateBB)
                                    {
                                        var close = entity_Pivot.Close * (decimal)(1 + rateBB / 100);
                                        itemClose.Close = close;
                                        eClose = itemClose;
                                        break;
                                    }
                                }

                                dtFlag = eClose.Date;
                                var rate = Math.Round(100 * (-1 + eClose.Close / entity_Pivot.Close), 1);
                                var lRange = lData15m.Where(x => x.Date >= entity_Pivot.Date.AddMinutes(15) && x.Date <= eClose.Date);
                                var maxH = lRange.Max(x => x.High);
                                var minL = lRange.Min(x => x.Low);

                                var winloss = "W";
                                if (rate <= (decimal)0)
                                {
                                    winloss = "L";
                                }

                                var maxSL = Math.Round(100 * (-1 + minL / entity_Pivot.Close), 1);
                                if (maxSL <= -SL_RATE)
                                {
                                    rate = -SL_RATE;
                                    winloss = "L";
                                }

                                if (winloss == "W")
                                {
                                    rate = Math.Abs(rate);
                                    winCount++;
                                }
                                else
                                {
                                    rate = -Math.Abs(rate);
                                    lossCount++;
                                }

                                //var mesItem = $"{item}|{winloss}|ENTRY: {entity_Pivot.Date.ToString("dd/MM/yyyy HH:mm")}|CLOSE: {eClose.Date.ToString("dd/MM/yyyy HH:mm")}";
                                //Console.WriteLine(mesItem);

                                //lRate.Add(rate);
                                lModel.Add(new clsData
                                {
                                    s = item,
                                    Date = entity_Sig.Date,
                                    Rate = rate,
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"SyncDataService.Bybit_LONG|EXCEPTION| {ex.Message}");
                            }

                        }

                        if (winCount + lossCount <= 1)
                            continue;

                        var rateRes = Math.Round(((decimal)winCount / (winCount + lossCount)), 2);
                        var sumRate = lModel.Where(x => x.s == item).Sum(x => x.Rate);
                        var count = lModel.Count(x => x.s == item);
                        var items = lModel.Where(x => x.s == item);
                        var perRate = Math.Round((float)sumRate / count, 1);
                        //Special
                        if (rateRes < (decimal)0.5
                          || perRate <= 0.7)
                        {
                            lModel = lModel.Except(items).ToList();
                            continue;
                        }

                        var realWin = 0;
                        foreach (var model in items)
                        {
                            if (model.Rate > (decimal)0.5)
                                realWin++;
                        }

                        winTotal += winCount;
                        lossTotal += lossCount;
                        winCount = 0;
                        lossCount = 0;

                        var winrate = Math.Round((double)realWin / count, 1);

                        var mes = $"{item}\t\t\t| W/Total: {realWin}/{count} = {winrate}%|Rate: {sumRate}%|Per: {perRate}%";
                        //Console.WriteLine(mes);

                        lResult.Add(new clsResult
                        {
                            s = item,
                            Win = realWin,
                            Winrate = winrate,
                            Perate = perRate,
                            Mes = mes
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                }

                var lResultOrder = lResult.OrderByDescending(x => x.Winrate).ThenByDescending(x => x.Win).ThenByDescending(x => x.Perate).ToList();
                var lRes = lResultOrder.Where(x => x.Winrate >= 0.8).ToList();
                if (lRes.Count() < _TAKE)
                {
                    lRes = lResultOrder.Take(_TAKE).ToList();
                }

                if (lRes.Count() < _TAKE - 15)
                    return;

                //Delete
                var builder = Builders<Symbol>.Filter;
                _symRepo.DeleteMany(builder.And(
                    builder.Eq(x => x.ex, exchange),
                    builder.Eq(x => x.ty, (int)Binance.Net.Enums.OrderSide.Buy)
                ));

                var rank = 1;
                foreach (var item in lRes)
                {
                    Console.WriteLine(item.Mes);
                    _symRepo.InsertOne(new Symbol
                    {
                        s = item.s,
                        ex = exchange,
                        ty = (int)Binance.Net.Enums.OrderSide.Buy,
                        rank = rank++
                    });
                }

                Console.WriteLine($"Tong: {lModel.Sum(x => x.Rate)}%|W/L: {winTotal}/{lossTotal}");

                var end = DateTime.Now;
                Console.WriteLine($"TotalTime: {(end - start).TotalSeconds}");
                await _teleService.SendMessage(_idUser, $"[Đã đồng bộ] BYBIT LONG");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SyncDataService.Bybit_LONG|EXCEPTION| {ex.Message}");
            }
        }

        public async Task Binance_SHORT()
        {
            try
            {
                var start = DateTime.Now;
                var exchange = (int)EExchange.Binance;
                var lAll = await StaticVal.BinanceInstance().UsdFuturesApi.CommonFuturesClient.GetSymbolsAsync();
                var lUsdt = lAll.Data.Where(x => x.Name.EndsWith("USDT")).Select(x => x.Name);
                var lTake = lUsdt.ToList();
                decimal SL_RATE = 2.5m;
                int hour = 4;
                decimal rateProfit_Min = 2.5m;
                decimal rateProfit_Max = 7m;

                var lModel = new List<clsData>();
                var lResult = new List<clsResult>();

                var winTotal = 0;
                var lossTotal = 0;

                foreach (var item in lTake)
                {
                    if (item.Contains('-'))
                        continue;

                    var winCount = 0;
                    var lossCount = 0;
                    try
                    {
                        var lData15m = new List<Quote>();
                        var last = new Quote();
                        var lData20 = await _apiService.GetData_Binance(item, EInterval.M15, DateTimeOffset.Now.AddDays(-20).ToUnixTimeMilliseconds());
                        Thread.Sleep(200);
                        lData15m.AddRange(lData20.Where(x => x.Date > last.Date));
                        last = lData15m.Last();

                        var lData10 = await _apiService.GetData_Binance(item, EInterval.M15, DateTimeOffset.Now.AddDays(-10).ToUnixTimeMilliseconds());
                        Thread.Sleep(200);
                        lData15m.AddRange(lData10.Where(x => x.Date > last.Date));
                        var lbb = lData15m.GetBollingerBands();
                        var lrsi = lData15m.GetRsi();
                        var lVol = lData15m.Select(x => new Quote
                        {
                            Date = x.Date,
                            Close = x.Volume
                        }).ToList();
                        var lMaVol = lVol.GetSma(20);

                        DateTime dtFlag = DateTime.MinValue;
                        //var count = 0;
                        foreach (var ma20 in lbb)
                        {
                            try
                            {
                                if (ma20.Sma is null
                                    || dtFlag >= ma20.Date)
                                    continue;

                                var entity_Sig = lData15m.First(x => x.Date == ma20.Date);
                                var rsi_Sig = lrsi.First(x => x.Date == ma20.Date);
                                var maVol_Sig = lMaVol.First(x => x.Date == ma20.Date);

                                var entity_Pivot = lData15m.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));
                                var rsi_Pivot = lrsi.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));
                                var bb_Pivot = lbb.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));

                                if (
                                    entity_Sig.Close <= entity_Sig.Open
                                   || rsi_Sig.Rsi < 65
                                   || entity_Sig.High <= (decimal)ma20.UpperBand.Value
                                   || Math.Abs(entity_Sig.Close - (decimal)ma20.UpperBand.Value) > Math.Abs((decimal)ma20.Sma.Value - entity_Sig.Close)
                                   )
                                    continue;

                                if (entity_Sig.Volume < (decimal)(maVol_Sig.Sma.Value * 1.5))
                                    continue;

                                if (entity_Pivot is null
                                  || rsi_Pivot.Rsi > 80 || rsi_Pivot.Rsi < 65
                                  || entity_Pivot.Low <= (decimal)bb_Pivot.Sma.Value
                                  || entity_Pivot.High <= (decimal)bb_Pivot.UpperBand.Value
                                  )
                                    continue;

                                var rateVol = Math.Round(entity_Pivot.Volume / entity_Sig.Volume, 1);
                                if (rateVol > (decimal)0.6) //Vol hiện tại phải nhỏ hơn hoặc bằng 0.6 lần vol của nến liền trước
                                    continue;

                                //check div by zero
                                if (entity_Sig.High == entity_Sig.Low
                                    || entity_Pivot.High == entity_Pivot.Low
                                    || Math.Min(entity_Pivot.Open, entity_Pivot.Close) == entity_Pivot.Low)
                                    continue;

                                var rateCur = Math.Abs((entity_Sig.Open - entity_Sig.Close) / (entity_Sig.High - entity_Sig.Low));  //độ dài nến hiện tại
                                var ratePivot = Math.Abs((entity_Pivot.Open - entity_Pivot.Close) / (entity_Pivot.High - entity_Pivot.Low));  //độ dài nến pivot
                                var isHammer = (entity_Sig.High - entity_Sig.Close) >= (decimal)1.2 * (entity_Sig.Close - entity_Sig.Low);

                                if (isHammer)
                                {

                                }
                                else if (ratePivot < (decimal)0.2)
                                {
                                    var checkDoji = (entity_Pivot.High - Math.Max(entity_Pivot.Open, entity_Pivot.Close)) / (Math.Min(entity_Pivot.Open, entity_Pivot.Close) - entity_Pivot.Low);
                                    if (checkDoji >= (decimal)0.75 && checkDoji <= (decimal)1.25)
                                    {
                                        continue;
                                    }
                                }
                                else if (rateCur > (decimal)0.8)
                                {
                                    //check độ dài nến pivot
                                    var isValid = Math.Abs(entity_Pivot.Open - entity_Pivot.Close) >= Math.Abs(entity_Sig.Open - entity_Sig.Close);
                                    if (isValid)
                                        continue;
                                }

                                var eClose = lData15m.FirstOrDefault(x => x.Date >= entity_Pivot.Date.AddHours(hour));
                                if (eClose is null)
                                    continue;

                                var rateBB = (decimal)(Math.Round(100 * (-1 + bb_Pivot.UpperBand.Value / bb_Pivot.LowerBand.Value)) - 1);
                                if (rateBB < rateProfit_Min - 1)
                                {
                                    continue;
                                }
                                else if (rateBB > rateProfit_Max)
                                {
                                    rateBB = rateProfit_Max;
                                }

                                var lClose = lData15m.Where(x => x.Date > entity_Pivot.Date && x.Date <= entity_Pivot.Date.AddHours(hour));
                                var isChotNon = false;
                                foreach (var itemClose in lClose)
                                {
                                    var ma = lbb.First(x => x.Date == itemClose.Date);
                                    if (itemClose.Low < (decimal)ma.LowerBand)
                                    {
                                        eClose = itemClose;
                                        break;
                                    }

                                    if (isChotNon
                                     && itemClose.Close > (decimal)ma.Sma.Value
                                     && itemClose.Close >= itemClose.Open
                                     && itemClose.Close <= entity_Pivot.Close)
                                    {
                                        eClose = itemClose;
                                        break;
                                    }

                                    if (itemClose.Low <= (decimal)ma.Sma.Value)
                                    {
                                        isChotNon = true;
                                    }

                                    var rateCheck = Math.Round(100 * (-1 + entity_Pivot.Close / itemClose.Low), 1);
                                    if (rateCheck > rateBB)
                                    {
                                        var close = entity_Pivot.Close * (1 - rateBB / 100);
                                        itemClose.Close = close;
                                        eClose = itemClose;
                                        break;
                                    }
                                }

                                dtFlag = eClose.Date;
                                var rate = Math.Round(100 * (-1 + entity_Pivot.Close / eClose.Close), 1);
                                var lRange = lData15m.Where(x => x.Date >= entity_Pivot.Date.AddMinutes(15) && x.Date <= eClose.Date);
                                var maxH = lRange.Max(x => x.High);
                                var minL = lRange.Min(x => x.Low);

                                var winloss = "W";
                                if (rate <= (decimal)0)
                                {
                                    winloss = "L";
                                }

                                var maxSL = Math.Round(100 * (-1 + entity_Pivot.Close / maxH), 1);
                                if (maxSL <= -SL_RATE)
                                {
                                    rate = -SL_RATE;
                                    winloss = "L";
                                }

                                if (winloss == "W")
                                {
                                    rate = Math.Abs(rate);
                                    winCount++;
                                }
                                else
                                {
                                    rate = -Math.Abs(rate);
                                    lossCount++;
                                }

                                //lRate.Add(rate);
                                lModel.Add(new clsData
                                {
                                    s = item,
                                    Date = entity_Sig.Date,
                                    Rate = rate
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"SyncDataService.Binance_SHORT|EXCEPTION| {ex.Message}");
                            }

                        }

                        if (winCount + lossCount <= 1)
                            continue;

                        var rateRes = Math.Round(((decimal)winCount / (winCount + lossCount)), 2);
                        var sumRate = lModel.Where(x => x.s == item).Sum(x => x.Rate);
                        var count = lModel.Count(x => x.s == item);
                        var items = lModel.Where(x => x.s == item);
                        var perRate = Math.Round((float)sumRate / count, 1);

                        //Special 
                        if (rateRes < (decimal)0.5
                          || perRate <= 0.7)
                        {
                            lModel = lModel.Except(items).ToList();
                            continue;
                        }

                        var realWin = 0;
                        foreach (var model in lModel.Where(x => x.s == item))
                        {
                            if (model.Rate > (decimal)0)
                                realWin++;
                        }

                        winTotal += winCount;
                        lossTotal += lossCount;
                        winCount = 0;
                        lossCount = 0;

                        var winrate = Math.Round((double)realWin / count, 1);

                        var mes = $"{item}\t\t\t| W/Total: {realWin}/{count} = {winrate}%|Rate: {sumRate}%|Per: {perRate}%";
                        //Console.WriteLine(mes);

                        lResult.Add(new clsResult
                        {
                            s = item,
                            Win = realWin,
                            Winrate = winrate,
                            Perate = perRate,
                            Mes = mes
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                }

                var lRes = lResult.OrderByDescending(x => x.Winrate).ThenByDescending(x => x.Win).ThenByDescending(x => x.Perate).Take(_TAKE).ToList();
                if (lRes.Count() < _TAKE - 15)
                    return;

                //Delete
                var builder = Builders<Symbol>.Filter;
                _symRepo.DeleteMany(builder.And(
                    builder.Eq(x => x.ex, exchange),
                    builder.Eq(x => x.ty, (int)Binance.Net.Enums.OrderSide.Sell)
                ));

                var rank = 1;
                foreach (var item in lRes)
                {
                    Console.WriteLine(item.Mes);
                    _symRepo.InsertOne(new Symbol
                    {
                        s = item.s,
                        ex = exchange,
                        ty = (int)Binance.Net.Enums.OrderSide.Sell,
                        rank = rank++
                    });
                }

                Console.WriteLine($"Tong: {lModel.Sum(x => x.Rate)}%|W/L: {winTotal}/{lossTotal}");

                var end = DateTime.Now;
                Console.WriteLine($"TotalTime: {(end - start).TotalSeconds}");
                await _teleService.SendMessage(_idUser, $"[Đã đồng bộ] BINANCE SHORT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SyncDataService.Binance_SHORT|EXCEPTION| {ex.Message}");
            }
        }

        public async Task Bybit_SHORT()
        {
            try
            {
                var start = DateTime.Now;
                var exchange = (int)EExchange.Bybit;
                var lAll = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, limit: 1000);
                var lUsdt = lAll.Data.List.Where(x => x.QuoteAsset == "USDT" && !x.Name.StartsWith("1000")).Select(x => x.Name);
                var lTake = lUsdt.ToList();
                decimal SL_RATE = 2.5m;
                int hour = 4;
                decimal rateProfit_Min = 2.5m;
                decimal rateProfit_Max = 7m;

                var lModel = new List<clsData>();
                var lResult = new List<clsResult>();

                var winTotal = 0;
                var lossTotal = 0;

                foreach (var item in lTake)
                {
                    if (item.Contains('-'))
                        continue;

                    var winCount = 0;
                    var lossCount = 0;
                    try
                    {
                        var lData15m = await GetData_Bybit(item);
                        var lbb = lData15m.GetBollingerBands();
                        var lrsi = lData15m.GetRsi();
                        var lVol = lData15m.Select(x => new Quote
                        {
                            Date = x.Date,
                            Close = x.Volume
                        }).ToList();
                        var lMaVol = lVol.GetSma(20);

                        DateTime dtFlag = DateTime.MinValue;
                        //var count = 0;
                        foreach (var ma20 in lbb)
                        {
                            try
                            {
                                if (ma20.Sma is null
                                    || dtFlag >= ma20.Date)
                                    continue;

                                var entity_Sig = lData15m.First(x => x.Date == ma20.Date);
                                var rsi_Sig = lrsi.First(x => x.Date == ma20.Date);
                                var maVol_Sig = lMaVol.First(x => x.Date == ma20.Date);

                                var entity_Pivot = lData15m.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));
                                var rsi_Pivot = lrsi.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));
                                var bb_Pivot = lbb.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));

                                if (
                                    entity_Sig.Close <= entity_Sig.Open
                                   || rsi_Sig.Rsi < 65
                                   || entity_Sig.High <= (decimal)ma20.UpperBand.Value
                                   || Math.Abs(entity_Sig.Close - (decimal)ma20.UpperBand.Value) > Math.Abs((decimal)ma20.Sma.Value - entity_Sig.Close)
                                   )
                                    continue;

                                if (entity_Sig.Volume < (decimal)(maVol_Sig.Sma.Value * 1.5))
                                    continue;

                                if (entity_Pivot is null
                                  || rsi_Pivot.Rsi > 80 || rsi_Pivot.Rsi < 65
                                  || entity_Pivot.Low <= (decimal)bb_Pivot.Sma.Value
                                  || entity_Pivot.High <= (decimal)bb_Pivot.UpperBand.Value
                                  )
                                    continue;

                                var rateVol = Math.Round(entity_Pivot.Volume / entity_Sig.Volume, 1);
                                if (rateVol > (decimal)0.6) //Vol hiện tại phải nhỏ hơn hoặc bằng 0.6 lần vol của nến liền trước
                                    continue;

                                //check div by zero
                                if (entity_Sig.High == entity_Sig.Low
                                    || entity_Pivot.High == entity_Pivot.Low
                                    || Math.Min(entity_Pivot.Open, entity_Pivot.Close) == entity_Pivot.Low)
                                    continue;

                                var rateCur = Math.Abs((entity_Sig.Open - entity_Sig.Close) / (entity_Sig.High - entity_Sig.Low));  //độ dài nến hiện tại
                                var ratePivot = Math.Abs((entity_Pivot.Open - entity_Pivot.Close) / (entity_Pivot.High - entity_Pivot.Low));  //độ dài nến pivot
                                var isHammer = (entity_Sig.High - entity_Sig.Close) >= (decimal)1.2 * (entity_Sig.Close - entity_Sig.Low);

                                if (isHammer)
                                {

                                }
                                else if (ratePivot < (decimal)0.2)
                                {
                                    var checkDoji = (entity_Pivot.High - Math.Max(entity_Pivot.Open, entity_Pivot.Close)) / (Math.Min(entity_Pivot.Open, entity_Pivot.Close) - entity_Pivot.Low);
                                    if (checkDoji >= (decimal)0.75 && checkDoji <= (decimal)1.25)
                                    {
                                        continue;
                                    }
                                }
                                else if (rateCur > (decimal)0.8)
                                {
                                    //check độ dài nến pivot
                                    var isValid = Math.Abs(entity_Pivot.Open - entity_Pivot.Close) >= Math.Abs(entity_Sig.Open - entity_Sig.Close);
                                    if (isValid)
                                        continue;
                                }

                                #region Thêm xử lý
                                var isPass = false;
                                var lCheck = lData15m.Where(x => x.Date > entity_Pivot.Date).Take(8).Skip(1);
                                foreach (var check in lCheck)
                                {
                                    var rateCheck = Math.Round(100 * (-1 + check.High / entity_Pivot.Close), 1);
                                    if (rateCheck >= 1m)
                                    {
                                        entity_Pivot = check;
                                        entity_Pivot.Close = entity_Pivot.Close * 1.01m;
                                        isPass = true; break;
                                    }
                                }
                                if (!isPass)
                                    continue;
                                #endregion

                                var eClose = lData15m.FirstOrDefault(x => x.Date >= entity_Pivot.Date.AddHours(hour));
                                if (eClose is null)
                                    continue;

                                var rateBB = (decimal)(Math.Round(100 * (-1 + bb_Pivot.UpperBand.Value / bb_Pivot.LowerBand.Value)) - 1);
                                if (rateBB < rateProfit_Min - 1)
                                {
                                    continue;
                                }
                                else if (rateBB > rateProfit_Max)
                                {
                                    rateBB = rateProfit_Max;
                                }

                                var lClose = lData15m.Where(x => x.Date > entity_Pivot.Date && x.Date <= entity_Pivot.Date.AddHours(hour));
                                var isChotNon = false;
                                foreach (var itemClose in lClose)
                                {
                                    var ma = lbb.First(x => x.Date == itemClose.Date);
                                    if (itemClose.Low < (decimal)ma.LowerBand)
                                    {
                                        eClose = itemClose;
                                        break;
                                    }

                                    if (isChotNon
                                      && itemClose.Close > (decimal)ma.Sma.Value
                                      && itemClose.Close >= itemClose.Open
                                      && itemClose.Close <= entity_Pivot.Close)
                                    {
                                        eClose = itemClose;
                                        break;
                                    }

                                    if (itemClose.Low <= (decimal)ma.Sma.Value)
                                    {
                                        isChotNon = true;
                                    }

                                    var rateCheck = Math.Round(100 * (-1 + entity_Pivot.Close / itemClose.Low), 1);
                                    if (rateCheck > rateBB)
                                    {
                                        var close = entity_Pivot.Close * (1 - rateBB / 100);
                                        itemClose.Close = close;
                                        eClose = itemClose;
                                        break;
                                    }
                                }

                                dtFlag = eClose.Date;
                                var rate = Math.Round(100 * (-1 + entity_Pivot.Close / eClose.Close), 1);
                                var lRange = lData15m.Where(x => x.Date >= entity_Pivot.Date.AddMinutes(15) && x.Date <= eClose.Date);
                                var maxH = lRange.Max(x => x.High);
                                var minL = lRange.Min(x => x.Low);

                                var winloss = "W";
                                if (rate <= (decimal)0)
                                {
                                    winloss = "L";
                                }

                                var maxSL = Math.Round(100 * (-1 + entity_Pivot.Close / maxH), 1);
                                if (maxSL <= -SL_RATE)
                                {
                                    rate = -SL_RATE;
                                    winloss = "L";
                                }

                                if (winloss == "W")
                                {
                                    rate = Math.Abs(rate);
                                    winCount++;
                                }
                                else
                                {
                                    rate = -Math.Abs(rate);
                                    lossCount++;
                                }

                                //lRate.Add(rate);
                                lModel.Add(new clsData
                                {
                                    s = item,
                                    Date = entity_Sig.Date,
                                    Rate = rate
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"SyncDataService.Bybit_SHORT|EXCEPTION| {ex.Message}");
                            }

                        }

                        if (winCount + lossCount <= 1)
                            continue;

                        var rateRes = Math.Round(((decimal)winCount / (winCount + lossCount)), 2);
                        var sumRate = lModel.Where(x => x.s == item).Sum(x => x.Rate);
                        var count = lModel.Count(x => x.s == item);
                        var items = lModel.Where(x => x.s == item);
                        var perRate = Math.Round((float)sumRate / count, 1);

                        //Special 
                        if (rateRes < (decimal)0.5
                          || perRate <= 0.7)
                        {
                            lModel = lModel.Except(items).ToList();
                            continue;
                        }

                        var realWin = 0;
                        foreach (var model in lModel.Where(x => x.s == item))
                        {
                            if (model.Rate > (decimal)0)
                                realWin++;
                        }

                        winTotal += winCount;
                        lossTotal += lossCount;
                        winCount = 0;
                        lossCount = 0;

                        var winrate = Math.Round((double)realWin / count, 1);

                        var mes = $"{item}\t\t\t| W/Total: {realWin}/{count} = {winrate}%|Rate: {sumRate}%|Per: {perRate}%";
                        //Console.WriteLine(mes);

                        lResult.Add(new clsResult
                        {
                            s = item,
                            Win = realWin,
                            Winrate = winrate,
                            Perate = perRate,
                            Mes = mes
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                }

                var lResultOrder = lResult.OrderByDescending(x => x.Winrate).ThenByDescending(x => x.Win).ThenByDescending(x => x.Perate).ToList();
                var lRes = lResultOrder.Where(x => x.Winrate >= 0.8).ToList();
                if(lRes.Count() < _TAKE)
                {
                    lRes = lResultOrder.Take(_TAKE).ToList();
                }

                if (lRes.Count() < _TAKE - 15)
                    return;

                //Delete
                var builder = Builders<Symbol>.Filter;
                _symRepo.DeleteMany(builder.And(
                    builder.Eq(x => x.ex, exchange),
                    builder.Eq(x => x.ty, (int)Binance.Net.Enums.OrderSide.Sell)
                ));

                var rank = 1;
                foreach (var item in lRes)
                {
                    Console.WriteLine(item.Mes);
                    _symRepo.InsertOne(new Symbol
                    {
                        s = item.s,
                        ex = exchange,
                        ty = (int)Binance.Net.Enums.OrderSide.Sell,
                        rank = rank++
                    });
                }

                Console.WriteLine($"Tong: {lModel.Sum(x => x.Rate)}%|W/L: {winTotal}/{lossTotal}");
                var end = DateTime.Now;
                Console.WriteLine($"TotalTime: {(end - start).TotalSeconds}");
                await _teleService.SendMessage(_idUser, $"[Đã đồng bộ] BYBIT SHORT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SyncDataService.Bybit_SHORT|EXCEPTION| {ex.Message}");
            }
        }
    }

    public class clsData
    {
        public string s { get; set; }
        public DateTime Date { get; set; }
        public decimal Rate { get; set; }
    }

    public class clsResult
    {
        public string s { get; set; }
        public int Win { get; set; }
        public double Winrate { get; set; }
        public double Perate { get; set; }
        public string Mes { get; set; }
    }
}
