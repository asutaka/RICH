using Bybit.Net.Enums;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Skender.Stock.Indicators;
using TestPr.DAL;
using TestPr.Utils;

namespace TestPr.Service
{
    public interface ITestService
    {
        Task CheckAllBINANCE_LONG(bool isNear = false);
        Task CheckAllBINANCE_SHORT(bool isNear = false);
        Task CheckAllBYBIT_LONG(bool isNear = false);
        Task CheckAllBYBIT_SHORT(bool isNear = false);
    }
    public class TestService : ITestService
    {
        private readonly ILogger<TestService> _logger;
        private readonly IAPIService _apiService;
        private readonly ISymbolRepo _symRepo;
        public TestService(ILogger<TestService> logger, IAPIService apiService, ISymbolRepo symRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _symRepo = symRepo;
        }

        //LONG RSI Tong(55): 883.0%|W/L: 468/225
        public async Task CheckAllBINANCE_LONG(bool isNear = false)
        {
            try
            {
                var lAll = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, limit: 1000);
                var lUsdt = lAll.Data.List.Where(x => x.QuoteAsset == "USDT" && !x.Name.StartsWith("1000")).Select(x => x.Name);
                var countUSDT = lUsdt.Count();//461
                var lTake = lUsdt.ToList();
                //var lTake = lUsdt.Skip(0).Take(50).ToList();
                //2x1.7 best
                //decimal SL_RATE = 1.7m;//1.5,1.6,1.8,1.9,2
                decimal SL_RATE = 2.5m;//1.5,1.6,1.8,1.9,2
                int hour = 4;//1h,2h,3h,4h
                decimal rateProfit = 10;

                var lModel = new List<LongMa20>();

                var winTotal = 0;
                var lossTotal = 0;

                #region comment
                lTake.Clear();
                var lTmp = new List<string>
                {
                    "MUBARAKUSDT",
                    "1000BONKUSDT",
                    "1000WHYUSDT",
                    "B3USDT",
                    "SAFEUSDT",
                    "PLUMEUSDT",
                    "GRTUSDT",
                    "QTUMUSDT",
                    "RAREUSDT",
                    "LUMIAUSDT",
                    "1000CHEEMSUSDT",
                    "VTHOUSDT",
                    "INJUSDT",
                    "ZRXUSDT",
                    "ALTUSDT",
                    "CETUSUSDT",
                    "PHBUSDT",
                    "DODOXUSDT",
                    "BADGERUSDT",
                    "GUNUSDT",
                    "STOUSDT",
                    "EOSUSDT",
                    "NFPUSDT",
                    "BSWUSDT",
                    "RAYSOLUSDT",
                    "ALICEUSDT",
                    "OXTUSDT",
                    "UMAUSDT",
                    "BIOUSDT",
                    "SEIUSDT",
                    "ONDOUSDT",
                    "LPTUSDT",
                    "CFXUSDT",
                    "PHAUSDT",
                    "IOSTUSDT",
                    "RENDERUSDT",
                    "ALGOUSDT",
                    "KSMUSDT",
                    "GLMUSDT",
                    "ARKMUSDT",
                    "TOKENUSDT",
                    "ACXUSDT",
                    "ANKRUSDT",
                    "ORDIUSDT",
                    "MTLUSDT",
                    "THEUSDT",
                    "CATIUSDT",
                    "EPICUSDT",
                    "IOTAUSDT",
                    "STORJUSDT",
                    "LQTYUSDT",
                    "AGLDUSDT",
                    "ATAUSDT",
                    "BANUSDT",
                    "ICXUSDT",
                    "MOODENGUSDT",
                    "ETCUSDT",
                    "BBUSDT",
                    "VIRTUALUSDT",
                    "BRETTUSDT",
                };
                lTake.AddRange(lTmp);
                #endregion
                foreach (var item in lTake)
                {

                    var winCount = 0;
                    var lossCount = 0;
                    try
                    {
                        //if (item != "BTCUSDT")
                        //    continue;
                        var lData15m = new List<Quote>();
                        var last = new Quote();

                        if(isNear)
                        {
                            var lData50 = await _apiService.GetData_Binance(item, EInterval.M15, DateTimeOffset.Now.AddDays(-50).ToUnixTimeMilliseconds());
                            if (lData50 == null || !lData50.Any())
                                continue;
                            lData15m.AddRange(lData50.Where(x => x.Date > last.Date));
                            last = lData15m.Last();
                            Thread.Sleep(200);

                            var lData40 = await _apiService.GetData_Binance(item, EInterval.M15, DateTimeOffset.Now.AddDays(-40).ToUnixTimeMilliseconds());
                            Thread.Sleep(200);
                            lData15m.AddRange(lData40.Where(x => x.Date > last.Date));
                            last = lData15m.Last();

                            var lData30 = await _apiService.GetData_Binance(item, EInterval.M15, DateTimeOffset.Now.AddDays(-30).ToUnixTimeMilliseconds());
                            Thread.Sleep(200);
                            lData15m.AddRange(lData30.Where(x => x.Date > last.Date));
                            last = lData15m.Last();

                            var lData20 = await _apiService.GetData_Binance(item, EInterval.M15, DateTimeOffset.Now.AddDays(-20).ToUnixTimeMilliseconds());
                            Thread.Sleep(200);
                            lData15m.AddRange(lData20.Where(x => x.Date > last.Date));
                            last = lData15m.Last();
                        }    

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

                                //if (ma20.Date.Month == 2 && ma20.Date.Day == 28 && ma20.Date.Hour == 1 && ma20.Date.Minute == 30)
                                //{
                                //    var z = 1;
                                //}

                                var entity_Sig = lData15m.First(x => x.Date == ma20.Date);
                                var rsi_Sig = lrsi.First(x => x.Date == ma20.Date);
                                var maVol_Sig = lMaVol.First(x => x.Date == ma20.Date);
                                //var minOpenClose = Math.Min(entity_Sig.Open, entity_Sig.Close);

                                var entity_Pivot = lData15m.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));
                                var rsi_Pivot = lrsi.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));
                                var bb_Pivot = lbb.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));

                                if (entity_Sig.Close >= entity_Sig.Open
                                    || rsi_Sig.Rsi > 35
                                    || entity_Sig.Low >= (decimal)ma20.LowerBand.Value
                                    || entity_Sig.Close - (decimal)ma20.LowerBand.Value >= (decimal)ma20.Sma.Value - entity_Sig.Close
                                    )
                                    continue;

                                if (!StaticVal._lCoinSpecial.Contains(item))
                                {
                                    if (entity_Sig.Volume < (decimal)(maVol_Sig.Sma.Value * 1.5))
                                        continue;
                                }

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

                                var next = lData15m.FirstOrDefault(x => x.Date == entity_Pivot.Date.AddMinutes(15));
                                if (next is null)
                                    continue;
                                var rateEntry = Math.Round(100 * (-1 + next.Low / entity_Pivot.Close), 1);// tỉ lệ từ entry đến giá thấp nhất

                                var eEntry = entity_Pivot;
                                var eClose = lData15m.FirstOrDefault(x => x.Date >= eEntry.Date.AddHours(hour));
                                if (eClose is null)
                                    continue;

                                var lClose = lData15m.Where(x => x.Date > eEntry.Date && x.Date <= eEntry.Date.AddHours(hour));
                                foreach (var itemClose in lClose)
                                {
                                    var ma = lbb.First(x => x.Date == itemClose.Date);
                                    if (itemClose.Close > (decimal)ma.UpperBand)//do something
                                    {
                                        eClose = itemClose;
                                        break;
                                    }

                                    var rateCheck = Math.Round(100 * (-1 + itemClose.High / eEntry.Close), 1); //chốt khi lãi > 10%
                                    if (rateCheck > rateProfit)
                                    {
                                        var close = eEntry.Close * (decimal)(1 + rateProfit / 100);
                                        itemClose.Close = close;
                                        eClose = itemClose;
                                        break;
                                    }
                                }

                                dtFlag = eClose.Date;
                                var rate = Math.Round(100 * (-1 + eClose.Close / eEntry.Close), 1);
                                var lRange = lData15m.Where(x => x.Date >= eEntry.Date.AddMinutes(15) && x.Date <= eClose.Date);
                                var maxH = lRange.Max(x => x.High);
                                var minL = lRange.Min(x => x.Low);

                                var winloss = "W";
                                if (rate <= (decimal)0.5)
                                {
                                    winloss = "L";
                                }

                                decimal maxTP = 0, maxSL = 0;
                                maxTP = Math.Round(100 * (-1 + maxH / eEntry.Close), 1);
                                maxSL = Math.Round(100 * (-1 + minL / eEntry.Close), 1);
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
                                lModel.Add(new LongMa20
                                {
                                    s = item,
                                    IsWin = winloss == "W",
                                    Date = entity_Sig.Date,
                                    Rate = rate,
                                    MaxTP = maxTP,
                                    MaxSL = maxSL,
                                    RateEntry = rateEntry,
                                });
                                //Console.WriteLine($"{item}|{winloss}|BUY|{entity_Sig.Date.ToString("dd/MM/yyyy HH:mm")}|{rate}%|TPMax: {maxTP}%|SLMax: {maxSL}%|RateEntry: {rateEntry}%|RSI: {rsi_Pivot.Rsi}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
                            }

                        }

                        if (winCount + lossCount <= 0)
                            continue;

                        var rateRes = Math.Round(((decimal)winCount / (winCount + lossCount)), 2);
                        var sumRate = lModel.Where(x => x.s == item).Sum(x => x.Rate);
                        var count = lModel.Count(x => x.s == item);
                        var items = lModel.Where(x => x.s == item);
                        var perRate = Math.Round((float)sumRate / count, 1);
                        //Special
                        //if (perRate <= 0.7)
                        if (rateRes <= (decimal)0.5
                          || sumRate <= 1
                          || perRate <= 0.7)
                        {
                            lModel = lModel.Except(items).ToList();
                            continue;
                        }

                        var realWin = 0;
                        foreach (var model in items)
                        {
                            if (model.Rate > (decimal)0)
                                realWin++;
                        }

                        winTotal += winCount;
                        lossTotal += lossCount;
                        winCount = 0;
                        lossCount = 0;

                        Console.WriteLine($"{item}\t\t\t| W/Total: {realWin}/{count} = {Math.Round((double)realWin / count, 1)}%|Rate: {sumRate}%|Per: {perRate}%");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                }

                Console.WriteLine($"Tong: {lModel.Sum(x => x.Rate)}%|W/L: {winTotal}/{lossTotal}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
            }
        }

        //SHORT RSI Tong(52): 654.5%|W/L: 344/167
        public async Task CheckAllBINANCE_SHORT(bool isNear = false)
        {
            try
            {
                var lAll = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, limit: 1000);
                var lUsdt = lAll.Data.List.Where(x => x.QuoteAsset == "USDT" && !x.Name.StartsWith("1000")).Select(x => x.Name);
                var countUSDT = lUsdt.Count();//461
                var lTake = lUsdt.ToList();
                //var lTake = lUsdt.Skip(0).Take(50).ToList();
                //2x1.7 best
                //decimal SL_RATE = 1.7m;//1.5,1.6,1.8,1.9,2
                decimal SL_RATE = 2.5m;//1.5,1.6,1.8,1.9,2
                int hour = 4;//1h,2h,3h,4h
                decimal rateProfit = 10;

                var lModel = new List<LongMa20>();

                var winTotal = 0;
                var lossTotal = 0;

                #region comment
                lTake.Clear();
                var lTmp = new List<string>
                {
                    "BROCCOLI714USDT",
                    "NILUSDT",
                    "1000XUSDT",
                    "DUSKUSDT",
                    "BADGERUSDT",
                    "AVAAIUSDT",
                    "HEIUSDT",
                    "LINKUSDT",
                    "HOOKUSDT",
                    "ORDIUSDT",
                    "AI16ZUSDT",
                    "CATIUSDT",
                    "APEUSDT",
                    "API3USDT",
                    "BIOUSDT",
                    "CHESSUSDT",
                    "1MBABYDOGEUSDT",
                    "LISTAUSDT",
                    "DEXEUSDT",
                    "QTUMUSDT",
                    "ANKRUSDT",
                    "NFPUSDT",
                    "OMUSDT",
                    "XVGUSDT",
                    "PENGUUSDT",
                    "ONDOUSDT",
                    "SAFEUSDT",
                    "RUNEUSDT",
                    "BAKEUSDT",
                    "DYDXUSDT",
                    "BERAUSDT",
                    "CTSIUSDT",
                    "VETUSDT",
                    "IOUSDT",
                };
                lTake.AddRange(lTmp);
                #endregion
                foreach (var item in lTake)
                {

                    var winCount = 0;
                    var lossCount = 0;
                    try
                    {
                        //if (item != "EOSUSDT")
                        //    continue;

                        var lData15m = new List<Quote>();
                        var last = new Quote();

                        if (isNear)
                        {
                            var lData50 = await _apiService.GetData_Binance(item, EInterval.M15, DateTimeOffset.Now.AddDays(-50).ToUnixTimeMilliseconds());
                            if (lData50 == null || !lData50.Any())
                                continue;
                            lData15m.AddRange(lData50.Where(x => x.Date > last.Date));
                            last = lData15m.Last();
                            if (last.Volume <= 0)
                                continue;
                            Thread.Sleep(200);

                            var lData40 = await _apiService.GetData_Binance(item, EInterval.M15, DateTimeOffset.Now.AddDays(-40).ToUnixTimeMilliseconds());
                            Thread.Sleep(200);
                            lData15m.AddRange(lData40.Where(x => x.Date > last.Date));
                            last = lData15m.Last();

                            var lData30 = await _apiService.GetData_Binance(item, EInterval.M15, DateTimeOffset.Now.AddDays(-30).ToUnixTimeMilliseconds());
                            Thread.Sleep(200);
                            lData15m.AddRange(lData30.Where(x => x.Date > last.Date));
                            last = lData15m.Last();

                            var lData20 = await _apiService.GetData_Binance(item, EInterval.M15, DateTimeOffset.Now.AddDays(-20).ToUnixTimeMilliseconds());
                            Thread.Sleep(200);
                            lData15m.AddRange(lData20.Where(x => x.Date > last.Date));
                            last = lData15m.Last();
                        }

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

                                //if (ma20.Date.Month == 3 && ma20.Date.Day == 17 && ma20.Date.Hour == 18 && ma20.Date.Minute == 00)
                                //{
                                //    var z = 1;
                                //}

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

                                if (!StaticVal._lCoinSpecial.Contains(item))
                                {
                                    if (entity_Sig.Volume < (decimal)(maVol_Sig.Sma.Value * 1.5))
                                        continue;
                                }

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


                                var next = lData15m.FirstOrDefault(x => x.Date == entity_Pivot.Date.AddMinutes(15));
                                if (next is null)
                                    continue;
                                var rateEntry = Math.Round(100 * (-1 + entity_Pivot.Close / next.High), 1);// tỉ lệ từ entry đến giá thấp nhất

                                var eClose = lData15m.FirstOrDefault(x => x.Date >= entity_Pivot.Date.AddHours(hour));
                                if (eClose is null)
                                    continue;

                                var lClose = lData15m.Where(x => x.Date > entity_Pivot.Date && x.Date <= entity_Pivot.Date.AddHours(hour));
                                foreach (var itemClose in lClose)
                                {
                                    var ma = lbb.First(x => x.Date == itemClose.Date);
                                    if (itemClose.Close < (decimal)ma.LowerBand)
                                    {
                                        eClose = itemClose;
                                        break;
                                    }

                                    var rateCheck = Math.Round(100 * (-1 + entity_Pivot.Close / itemClose.Low), 1);
                                    if (rateCheck > rateProfit)
                                    {
                                        var close = entity_Pivot.Close * (1 - rateProfit / 100);
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
                                if (rate <= (decimal)0.5)
                                {
                                    winloss = "L";
                                }

                                decimal maxTP = 0, maxSL = 0;
                                maxTP = Math.Round(100 * (-1 + entity_Pivot.Close / minL), 1);
                                maxSL = Math.Round(100 * (-1 + entity_Pivot.Close / maxH), 1);

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
                                lModel.Add(new LongMa20
                                {
                                    s = item,
                                    IsWin = winloss == "W",
                                    Date = entity_Sig.Date,
                                    Rate = rate,
                                    MaxTP = maxTP,
                                    MaxSL = maxSL,
                                    RateEntry = rateEntry,
                                });
                                //Console.WriteLine($"{item}|{winloss}|SELL|{entity_Sig.Date.ToString("dd/MM/yyyy HH:mm")}|{rate}%|TPMax: {maxTP}%|SLMax: {maxSL}%|RateEntry: {rateEntry}%|RSI: {rsi_Pivot.Rsi}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
                            }

                        }

                        if (winCount + lossCount <= 0)
                            continue;

                        var rateRes = Math.Round(((decimal)winCount / (winCount + lossCount)), 2);
                        var sumRate = lModel.Where(x => x.s == item).Sum(x => x.Rate);
                        var count = lModel.Count(x => x.s == item);
                        var items = lModel.Where(x => x.s == item);
                        var perRate = Math.Round((float)sumRate / count, 1);

                        //Special 
                        if (perRate <= 0.7)
                        //if (rateRes <= (decimal)0.5
                        //  || sumRate <= 1
                        //  || perRate <= 0.7)
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

                        Console.WriteLine($"{item}\t\t\t| W/Total: {realWin}/{count} = {Math.Round((double)realWin / count, 1)}%|Rate: {sumRate}%|Per: {perRate}%");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                }

                Console.WriteLine($"Tong: {lModel.Sum(x => x.Rate)}%|W/L: {winTotal}/{lossTotal}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
            }
        }

        //LONG RSI Tong(55): 918.2%|W/L: 507/282
        public async Task CheckAllBYBIT_LONG(bool isNear = false)
        {
            try
            {
                var lAll = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, limit: 1000);
                var lUsdt = lAll.Data.List.Where(x => x.QuoteAsset == "USDT" && !x.Name.StartsWith("1000")).Select(x => x.Name);
                var countUSDT = lUsdt.Count();//461
                var lTake = lUsdt.ToList();
                //var lTake = lUsdt.Skip(0).Take(50).ToList();
                //2x1.7 best
                //decimal SL_RATE = 1.7m;//1.5,1.6,1.8,1.9,2
                decimal SL_RATE = 2.5m;//1.5,1.6,1.8,1.9,2
                int hour = 4;//1h,2h,3h,4h
                decimal rateProfit = 4;

                var lModel = new List<LongMa20>();

                var winTotal = 0;
                var lossTotal = 0;

                #region comment
                lTake.Clear();
                var lTmp = new List<string>
                {
                    "DGBUSDT",
                    "SERAPHUSDT",
                    "ZBCNUSDT",
                    "GMTUSDT",
                    "AUDIOUSDT",
                    "A8USDT",
                    "MAGICUSDT",
                    "TLMUSDT",
                    "BANANAS31USDT",
                    "PHBUSDT",
                    "FLRUSDT",
                    "RAREUSDT",
                    "ZILUSDT",
                    "GUNUSDT",
                    "STOUSDT",
                    "RAYDIUMUSDT",
                    "FLOCKUSDT",
                    "KOMAUSDT",
                    "ZENTUSDT",
                    "HEIUSDT",
                    "ALTUSDT",
                    "ARCUSDT",
                    "DATAUSDT",
                    "GLMRUSDT",
                    "KNCUSDT",
                    "MAXUSDT",
                    "MOVRUSDT",
                    "QUICKUSDT",
                    "ORCAUSDT",
                    "PYTHUSDT",
                    "ALICEUSDT",
                    "ANKRUSDT",
                    "FIDAUSDT",
                    "LPTUSDT",
                    "PARTIUSDT",
                    "SPXUSDT",
                    "RLCUSDT",
                    "VIRTUALUSDT",
                    "BSWUSDT",
                    "CARVUSDT",
                    "CELRUSDT",
                    "CFXUSDT",
                    "MAVUSDT",
                    "MERLUSDT",
                    "GNOUSDT",
                    "NTRNUSDT",
                    "OXTUSDT",
                    "PEAQUSDT",
                    "POPCATUSDT",
                    "QTUMUSDT",
                    "TAIUSDT",
                    "TRUUSDT",
                };
                lTake.AddRange(lTmp);
                #endregion
                foreach (var item in lTake)
                {

                    var winCount = 0;
                    var lossCount = 0;
                    try
                    {
                        //if (item != "BTCUSDT")
                        //    continue;

                        var lData15m = new List<Quote>();
                        var last = new Quote();

                        if (isNear)
                        {
                            var lData50 = await _apiService.GetData_Bybit(item, EInterval.M15, DateTimeOffset.Now.AddDays(-50).ToUnixTimeMilliseconds());
                            if (lData50 == null || !lData50.Any())
                                continue;
                            lData15m.AddRange(lData50.Where(x => x.Date > last.Date));
                            last = lData15m.Last();
                            Thread.Sleep(200);

                            var lData40 = await _apiService.GetData_Bybit(item, EInterval.M15, DateTimeOffset.Now.AddDays(-40).ToUnixTimeMilliseconds());
                            Thread.Sleep(200);
                            lData15m.AddRange(lData40.Where(x => x.Date > last.Date));
                            last = lData15m.Last();

                            var lData30 = await _apiService.GetData_Bybit(item, EInterval.M15, DateTimeOffset.Now.AddDays(-30).ToUnixTimeMilliseconds());
                            Thread.Sleep(200);
                            lData15m.AddRange(lData30.Where(x => x.Date > last.Date));
                            last = lData15m.Last();

                            var lData20 = await _apiService.GetData_Bybit(item, EInterval.M15, DateTimeOffset.Now.AddDays(-20).ToUnixTimeMilliseconds());
                            Thread.Sleep(200);
                            lData15m.AddRange(lData20.Where(x => x.Date > last.Date));
                            last = lData15m.Last();
                        }

                        var lData10 = await _apiService.GetData_Bybit(item, EInterval.M15, DateTimeOffset.Now.AddDays(-10).ToUnixTimeMilliseconds());
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

                                //if (ma20.Date.Month == 2 && ma20.Date.Day == 28 && ma20.Date.Hour == 1 && ma20.Date.Minute == 30)
                                //{
                                //    var z = 1;
                                //}

                                var entity_Sig = lData15m.First(x => x.Date == ma20.Date);
                                var rsi_Sig = lrsi.First(x => x.Date == ma20.Date);
                                var maVol_Sig = lMaVol.First(x => x.Date == ma20.Date);
                                //var minOpenClose = Math.Min(entity_Sig.Open, entity_Sig.Close);

                                var entity_Pivot = lData15m.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));
                                var rsi_Pivot = lrsi.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));
                                var bb_Pivot = lbb.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));

                                if (entity_Sig.Close >= entity_Sig.Open
                                    || rsi_Sig.Rsi > 35
                                    || entity_Sig.Low >= (decimal)ma20.LowerBand.Value
                                    || entity_Sig.Close - (decimal)ma20.LowerBand.Value >= (decimal)ma20.Sma.Value - entity_Sig.Close
                                    )
                                    continue;

                                if (!StaticVal._lCoinSpecial.Contains(item))
                                {
                                    if (entity_Sig.Volume < (decimal)(maVol_Sig.Sma.Value * 1.5))
                                        continue;
                                }

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

                                var next = lData15m.FirstOrDefault(x => x.Date == entity_Pivot.Date.AddMinutes(15));
                                if (next is null)
                                    continue;
                                var rateEntry = Math.Round(100 * (-1 + next.Low / entity_Pivot.Close), 1);// tỉ lệ từ entry đến giá thấp nhất

                                var eEntry = entity_Pivot;
                                var eClose = lData15m.FirstOrDefault(x => x.Date >= eEntry.Date.AddHours(hour));
                                if (eClose is null)
                                    continue;

                                var lClose = lData15m.Where(x => x.Date > eEntry.Date && x.Date <= eEntry.Date.AddHours(hour));
                                foreach (var itemClose in lClose)
                                {
                                    var ma = lbb.First(x => x.Date == itemClose.Date);
                                    if (itemClose.Close > (decimal)ma.UpperBand)//do something
                                    {
                                        eClose = itemClose;
                                        break;
                                    }

                                    var rateCheck = Math.Round(100 * (-1 + itemClose.High / eEntry.Close), 1); //chốt khi lãi > 10%
                                    if(rateCheck > rateProfit)
                                    {
                                        var close = eEntry.Close * (decimal)(1 + rateProfit / 100);
                                        itemClose.Close = close;
                                        eClose = itemClose;
                                        break;
                                    }
                                }

                                dtFlag = eClose.Date;
                                var rate = Math.Round(100 * (-1 + eClose.Close / eEntry.Close), 1);
                                var lRange = lData15m.Where(x => x.Date >= eEntry.Date.AddMinutes(15) && x.Date <= eClose.Date);
                                var maxH = lRange.Max(x => x.High);
                                var minL = lRange.Min(x => x.Low);

                                var winloss = "W";
                                if (rate <= (decimal)0.5)
                                {
                                    winloss = "L";
                                }

                                decimal maxTP = 0, maxSL = 0;
                                maxTP = Math.Round(100 * (-1 + maxH / eEntry.Close), 1);
                                maxSL = Math.Round(100 * (-1 + minL / eEntry.Close), 1);
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
                                lModel.Add(new LongMa20
                                {
                                    s = item,
                                    IsWin = winloss == "W",
                                    Date = entity_Sig.Date,
                                    Rate = rate,
                                    MaxTP = maxTP,
                                    MaxSL = maxSL,
                                    RateEntry = rateEntry,
                                });
                                //Console.WriteLine($"{item}|{winloss}|BUY|{entity_Sig.Date.ToString("dd/MM/yyyy HH:mm")}|{rate}%|TPMax: {maxTP}%|SLMax: {maxSL}%|RateEntry: {rateEntry}%|RSI: {rsi_Pivot.Rsi}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
                            }

                        }

                        if (winCount + lossCount <= 4)
                            continue;

                        var rateRes = Math.Round(((decimal)winCount / (winCount + lossCount)), 2);
                        var sumRate = lModel.Where(x => x.s == item).Sum(x => x.Rate);
                        var count = lModel.Count(x => x.s == item);
                        var items = lModel.Where(x => x.s == item);
                        var perRate = Math.Round((float)sumRate / count, 1);
                        //Special 
                        ////if (perRate <= 0.7)
                        //if (rateRes <= (decimal)0.5
                        //  || sumRate <= 1
                        //  || perRate <= 0.7)
                        //{
                        //    lModel = lModel.Except(items).ToList();
                        //    continue;
                        //}

                        var realWin = 0;
                        foreach (var model in items)
                        {
                            if (model.Rate > (decimal)0)
                                realWin++;
                        }

                        winTotal += winCount;
                        lossTotal += lossCount;
                        winCount = 0;
                        lossCount = 0;

                        Console.WriteLine($"{item}\t\t\t| W/Total: {realWin}/{count} = {Math.Round((double)realWin / count, 1)}%|Rate: {sumRate}%|Per: {perRate}%");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                }

                Console.WriteLine($"Tong: {lModel.Sum(x => x.Rate)}%|W/L: {winTotal}/{lossTotal}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
            }
        }
        //SHORT RSI Tong(50): 837.0%|W/L: 429/236
        public async Task CheckAllBYBIT_SHORT(bool isNear = false)
        {
            try
            {
                var lAll = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, limit: 1000);
                var lUsdt = lAll.Data.List.Where(x => x.QuoteAsset == "USDT" && !x.Name.StartsWith("1000")).Select(x => x.Name);
                var countUSDT = lUsdt.Count();//461
                var lTake = lUsdt.ToList();
                //var lTake = lUsdt.Skip(0).Take(50).ToList();
                //2x1.7 best
                //decimal SL_RATE = 1.7m;//1.5,1.6,1.8,1.9,2
                decimal SL_RATE = 2.5m;//1.5,1.6,1.8,1.9,2
                int hour = 4;//1h,2h,3h,4h
                decimal rateProfit = 5;

                var lModel = new List<LongMa20>();

                var winTotal = 0;
                var lossTotal = 0;

                #region comment
                lTake.Clear();
                var lTmp = new List<string>
                {
                    "KERNELUSDT",
                    "SPELLUSDT",
                    "MAVIAUSDT",
                    "NILUSDT",
                    "JUSDT",
                    "XCHUSDT",
                    "XVGUSDT",
                    "MORPHOUSDT",
                    "ALUUSDT",
                    "ANKRUSDT",
                    "BMTUSDT",
                    "CATIUSDT",
                    "EGLDUSDT",
                    "FLUXUSDT",
                    "RDNTUSDT",
                    "SNTUSDT",
                    "ZENUSDT",
                    "XVSUSDT",
                    "FOXYUSDT",
                    "HEIUSDT",
                    "IDEXUSDT",
                    "PARTIUSDT",
                    "LUCEUSDT",
                    "ZBCNUSDT",
                    "ROSEUSDT",
                    "CRVUSDT",
                    "ETHWUSDT",
                    "MYROUSDT",
                    "SOLOUSDT",
                    "FWOGUSDT",
                    "AIOZUSDT",
                    "FLOCKUSDT",
                    "HIFIUSDT",
                    "MEMEFIUSDT",
                    "VTHOUSDT",
                    "POPCATUSDT",
                    "MVLUSDT",
                    "NCUSDT",
                    "VETUSDT",
                    "NEARUSDT",
                    "MAJORUSDT",
                    "ORCAUSDT",
                    "PRIMEUSDT",
                    "XNOUSDT",
                    "PORTALUSDT",
                    "TOKENUSDT",
                    "FLRUSDT",
                    "CVCUSDT",
                    "KNCUSDT",
                    "PERPUSDT",
                };
                lTake.AddRange(lTmp);
                #endregion
                foreach (var item in lTake)
                {

                    var winCount = 0;
                    var lossCount = 0;
                    try
                    {
                        //if (item != "EOSUSDT")
                        //    continue;

                        var lData15m = new List<Quote>();
                        var last = new Quote();

                        if (isNear)
                        {
                            var lData50 = await _apiService.GetData_Bybit(item, EInterval.M15, DateTimeOffset.Now.AddDays(-50).ToUnixTimeMilliseconds());
                            if (lData50 == null || !lData50.Any())
                                continue;
                            lData15m.AddRange(lData50.Where(x => x.Date > last.Date));
                            last = lData15m.Last();
                            if (last.Volume <= 0)
                                continue;
                            Thread.Sleep(200);

                            var lData40 = await _apiService.GetData_Bybit(item, EInterval.M15, DateTimeOffset.Now.AddDays(-40).ToUnixTimeMilliseconds());
                            Thread.Sleep(200);
                            lData15m.AddRange(lData40.Where(x => x.Date > last.Date));
                            last = lData15m.Last();

                            var lData30 = await _apiService.GetData_Bybit(item, EInterval.M15, DateTimeOffset.Now.AddDays(-30).ToUnixTimeMilliseconds());
                            Thread.Sleep(200);
                            lData15m.AddRange(lData30.Where(x => x.Date > last.Date));
                            last = lData15m.Last();

                            var lData20 = await _apiService.GetData_Bybit(item, EInterval.M15, DateTimeOffset.Now.AddDays(-20).ToUnixTimeMilliseconds());
                            Thread.Sleep(200);
                            lData15m.AddRange(lData20.Where(x => x.Date > last.Date));
                            last = lData15m.Last();
                        }

                        var lData10 = await _apiService.GetData_Bybit(item, EInterval.M15, DateTimeOffset.Now.AddDays(-10).ToUnixTimeMilliseconds());
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

                                //if (ma20.Date.Month == 3 && ma20.Date.Day == 17 && ma20.Date.Hour == 18 && ma20.Date.Minute == 00)
                                //{
                                //    var z = 1;
                                //}

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

                                if (!StaticVal._lCoinSpecial.Contains(item))
                                {
                                    if (entity_Sig.Volume < (decimal)(maVol_Sig.Sma.Value * 1.5))
                                        continue;
                                }

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


                                var next = lData15m.FirstOrDefault(x => x.Date == entity_Pivot.Date.AddMinutes(15));
                                if (next is null)
                                    continue;
                                var rateEntry = Math.Round(100 * (-1 + entity_Pivot.Close / next.High), 1);// tỉ lệ từ entry đến giá thấp nhất

                                var eClose = lData15m.FirstOrDefault(x => x.Date >= entity_Pivot.Date.AddHours(hour));
                                if (eClose is null)
                                    continue;

                                var lClose = lData15m.Where(x => x.Date > entity_Pivot.Date && x.Date <= entity_Pivot.Date.AddHours(hour));
                                foreach (var itemClose in lClose)
                                {
                                    var ma = lbb.First(x => x.Date == itemClose.Date);
                                    if (itemClose.Close < (decimal)ma.LowerBand)
                                    {
                                        eClose = itemClose;
                                        break;
                                    }

                                    var rateCheck = Math.Round(100 * (-1 + entity_Pivot.Close / itemClose.Low), 1);
                                    if (rateCheck > rateProfit)
                                    {
                                        var close = entity_Pivot.Close * (1 - rateProfit / 100);
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
                                if (rate <= (decimal)0.5)
                                {
                                    winloss = "L";
                                }

                                decimal maxTP = 0, maxSL = 0;
                                maxTP = Math.Round(100 * (-1 + entity_Pivot.Close / minL), 1);
                                maxSL = Math.Round(100 * (-1 + entity_Pivot.Close / maxH), 1);

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
                                lModel.Add(new LongMa20
                                {
                                    s = item,
                                    IsWin = winloss == "W",
                                    Date = entity_Sig.Date,
                                    Rate = rate,
                                    MaxTP = maxTP,
                                    MaxSL = maxSL,
                                    RateEntry = rateEntry,
                                });
                                //Console.WriteLine($"{item}|{winloss}|SELL|{entity_Sig.Date.ToString("dd/MM/yyyy HH:mm")}|{rate}%|TPMax: {maxTP}%|SLMax: {maxSL}%|RateEntry: {rateEntry}%|RSI: {rsi_Pivot.Rsi}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
                            }

                        }

                        if (winCount + lossCount <= 0)
                            continue;

                        var rateRes = Math.Round(((decimal)winCount / (winCount + lossCount)), 2);
                        var sumRate = lModel.Where(x => x.s == item).Sum(x => x.Rate);
                        var count = lModel.Count(x => x.s == item);
                        var items = lModel.Where(x => x.s == item);
                        var perRate = Math.Round((float)sumRate / count, 1);

                        //Special 
                        if (perRate <= 0.7)
                        //if (rateRes <= (decimal)0.5
                        //  || sumRate <= 1
                        //  || perRate <= 0.7)
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
                        
                        Console.WriteLine($"{item}\t\t\t| W/Total: {realWin}/{count} = {Math.Round((double)realWin / count, 1)}%|Rate: {sumRate}%|Per: {perRate}%");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                }

                Console.WriteLine($"Tong: {lModel.Sum(x => x.Rate)}%|W/L: {winTotal}/{lossTotal}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
            }
        }

        public class LongMa20
        {
            public string s { get; set; }
            public bool IsWin { get; set; }
            public DateTime Date { get; set; }
            public decimal Rate { get; set; }
            public decimal MaxTP { get; set; }
            public decimal MaxSL { get; set; }
            public decimal RateEntry { get; set; }
        }
    }
}



