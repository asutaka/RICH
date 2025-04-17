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
        Task LongMA20();
        Task ShortMA20();
        Task CheckAllBINANCE_LONG();
        Task CheckAllBINANCE_SHORT();
        Task CheckAllBYBIT_LONG();
        Task CheckAllBYBIT_SHORT();
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

        //ma20 - Long 56.7%|W/L: 106/34
        //ma20 - Long - Bybit 43.7%|W/L: 97/39
        public async Task LongMA20()
        {
            try
            {
                //2x1.7 best
                decimal SL_RATE = 1.7m;//1.5,1.6,1.8,1.9,2
                //decimal SL_RATE = 100m;//1.5,1.6,1.8,1.9,2
                int hour = 2;//1h,2h,3h,4h

                var lMesAll = new List<string>();
                var lModel = new List<LongMa20>();
                var lRate = new List<decimal>();
                var winCount = 0;
                var lossCount = 0;
                foreach (var item in StaticVal._lMa20)
                {
                    //if (item.Key != "1INCHUSDT")
                    //    continue;
                    var lMes = new List<string>();
                    
                    var lData15m = await _apiService.GetData(item, EInterval.M15, DateTimeOffset.Now.AddDays(-50).ToUnixTimeMilliseconds());
                    if (!lData15m.Any())
                        continue;
                    var last = lData15m.Last();
                    Thread.Sleep(200);

                    var lData40 = await _apiService.GetData(item, EInterval.M15, DateTimeOffset.Now.AddDays(-40).ToUnixTimeMilliseconds());
                    Thread.Sleep(200);
                    lData15m.AddRange(lData40.Where(x => x.Date > last.Date));
                    last = lData15m.Last();

                    var lData30 = await _apiService.GetData(item, EInterval.M15, DateTimeOffset.Now.AddDays(-30).ToUnixTimeMilliseconds());
                    Thread.Sleep(200);
                    lData15m.AddRange(lData30.Where(x => x.Date > last.Date));
                    last = lData15m.Last();

                    var lData20 = await _apiService.GetData(item, EInterval.M15, DateTimeOffset.Now.AddDays(-20).ToUnixTimeMilliseconds());
                    Thread.Sleep(200);
                    lData15m.AddRange(lData20.Where(x => x.Date > last.Date));
                    last = lData15m.Last();

                    var lData10 = await _apiService.GetData(item, EInterval.M15, DateTimeOffset.Now.AddDays(-10).ToUnixTimeMilliseconds());
                    Thread.Sleep(200);
                    lData15m.AddRange(lData10.Where(x => x.Date > last.Date));
                    var lbb = lData15m.GetBollingerBands();
                    Quote close = null;
                    foreach (var ma20 in lbb)
                    {
                        try
                        {
                            if (close != null && close.Date >= ma20.Date)
                                continue;
                            var side = (int)Binance.Net.Enums.OrderSide.Buy;
                            var cur = lData15m.First(x => x.Date == ma20.Date);
                            if (ma20.Sma is null 
                                || cur.Open >= cur.Close
                                || cur.Close <= (decimal)ma20.Sma.Value
                                || cur.Open >= (decimal)ma20.Sma.Value)
                                continue;

                            var prev = lData15m.First(x => x.Date == ma20.Date.AddMinutes(-15));
                            var ma20_Prev = lbb.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(-15));
                            if (ma20_Prev.Sma is null || prev.High > (decimal)ma20_Prev.Sma.Value)
                                continue;

                            var prev2 = lData15m.First(x => x.Date == ma20.Date.AddMinutes(-30));
                            var ma20_Prev2 = lbb.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(-30));
                            if (ma20_Prev2.Sma is null || prev2.High > (decimal)ma20_Prev2.Sma.Value)
                                continue;

                            //var prev3 = lData15m.First(x => x.Date == ma20.Date.AddMinutes(-45));
                            //var ma20_Prev3 = lbb.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(-45));
                            //if (ma20_Prev3.Sma is null || prev3.High > (decimal)ma20_Prev3.Sma.Value)
                            //    continue;

                            var next = lData15m.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));
                            if (next is null || next.Low >= cur.Close)
                                continue;

                            var rateEntry = Math.Round(100 * (-1 + next.Low / cur.Close), 1);// tỉ lệ từ entry đến giá thấp nhất

                            var lPrev = lData15m.Where(x => x.Date < cur.Date).TakeLast(5);
                            var indexPrev = 0;
                            decimal totalPrev = 0;
                            foreach (var itemPrev in lPrev)
                            {
                                if (itemPrev.High == itemPrev.Low)
                                {
                                    continue;
                                }
                                var prevRate = Math.Round(Math.Abs(itemPrev.Open - itemPrev.Close) * 100 / Math.Abs(itemPrev.High - itemPrev.Low));
                                if(prevRate > 10)
                                {
                                    indexPrev++;
                                    totalPrev += Math.Abs(itemPrev.Open - itemPrev.Close);
                                }
                            }
                            if (indexPrev <= 0)
                                continue;

                            var avgPrev = totalPrev / indexPrev;
                            if (Math.Abs(cur.Open - cur.Close) <= 2 * avgPrev
                                || Math.Abs(cur.Open - cur.Close) > avgPrev * 4)
                                continue;

                            //var curCO = cur.Close - cur.Open;
                            //var curHC = cur.High - cur.Close;

                            //if (curHC > curCO)
                            //{
                            //        continue;
                            //}

                            //var checkBB = 2 * (cur.High - cur.Close) > (decimal)ma20.UpperBand - cur.High;
                            //if (checkBB)
                            //    continue;

                            var checkBB = (cur.Close - (decimal)ma20.Sma) > (decimal)ma20.UpperBand - cur.Close;
                            if (checkBB)
                                continue;

                            var checkMa20 = (cur.Close - (decimal)ma20.Sma) * 2 < ((decimal)ma20.Sma - cur.Open);
                            if (checkMa20)
                                continue;

                            var eEntry = cur;
                            var eClose = lData15m.FirstOrDefault(x => x.Date >= eEntry.Date.AddHours(hour));
                            if (eClose is null)
                                continue;

                            var lClose = lData15m.Where(x => x.Date > eEntry.Date && x.Date <= eEntry.Date.AddHours(hour));
                            foreach (var itemClose in lClose)
                            {
                                var ma = lbb.First(x => x.Date == itemClose.Date);
                                if(itemClose.Close > (decimal)ma.UpperBand)
                                {
                                    eClose = itemClose;
                                    break;
                                }
                            }

                            close = eClose;
                            var rate = Math.Round(100 * (-1 + eClose.Close / eEntry.Close), 1);
                            var lRange = lData15m.Where(x => x.Date >= eEntry.Date.AddMinutes(15) && x.Date <= eClose.Date);
                            var maxH = lRange.Max(x => x.High);
                            var minL = lRange.Min(x => x.Low);

                            var winloss = "W";
                            if ((side == (int)Binance.Net.Enums.OrderSide.Buy && rate <= 0)
                                || (side == (int)Binance.Net.Enums.OrderSide.Sell && rate >= 0))
                            {
                                winloss = "L";
                            }

                            decimal maxTP = 0, maxSL = 0;
                            if (side == (int)Binance.Net.Enums.OrderSide.Buy)
                            {
                                maxTP = Math.Round(100 * (-1 + maxH / eEntry.Close), 1);
                                maxSL = Math.Round(100 * (-1 + minL / eEntry.Close), 1);
                            }
                            else
                            {
                                maxTP = Math.Round(100 * (-1 + eEntry.Close / minL), 1);
                                maxSL = Math.Round(100 * (-1 + eEntry.Close / maxH), 1);
                            }
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

                            lRate.Add(rate);
                            lModel.Add(new LongMa20
                            {
                                s = item,
                                IsWin = winloss == "W",
                                Date = cur.Date,
                                Rate = rate,
                                MaxTP = maxTP,
                                MaxSL = maxSL,
                                RateEntry = rateEntry,
                            });
                            var mes = $"{item}|{winloss}|{((Binance.Net.Enums.OrderSide)side).ToString()}|{cur.Date.ToString("dd/MM/yyyy HH:mm")}|{rate}%|TPMax: {maxTP}%|SLMax: {maxSL}%|RateEntry: {rateEntry}%";
                            lMes.Add(mes);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
                        }

                    }

                    lMesAll.AddRange(lMes);
                }

                foreach (var mes in lMesAll)
                {
                    Console.WriteLine(mes);
                }
                Console.WriteLine($"Tong: {lRate.Sum()}%|W/L: {winCount}/{lossCount}");

                var tmp = lModel.Select(x => x.Date).Distinct();
                var tmp1 = 1;

                // Note:
                // + Nến xanh cắt lên MA20
                // + 2 nến ngay phía trước đều nằm dưới MA20
                // + Vol nến hiện tại > ít nhất 8/9 nến trước đó
                // + Giữ 2 tiếng? hoặc nến chạm BB trên
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
            }
        }
        //ma20 - Short 110.1%|W/L: 304/165
        //ma20 - Short - Bybit 183.6%|W/L: 420/236
        public async Task ShortMA20()
        {
            try
            {
                //2x1.7 best
                decimal SL_RATE = 1.7m;//1.5,1.6,1.8,1.9,2
                //decimal SL_RATE = 100m;//1.5,1.6,1.8,1.9,2
                int hour = 2;//1h,2h,3h,4h

                var lMesAll = new List<string>();
                var lModel = new List<LongMa20>();
                var lRate = new List<decimal>();
                var winCount = 0;
                var lossCount = 0;
                var lDat = _symRepo.GetAll();

                foreach (var item in lDat.Where(x => x.ty == (int)OrderSide.Sell).Select(x => x.s))
                {
                    //if (item != "HIGHUSDT")
                    //    continue;
                    try
                    {
                        var lMes = new List<string>();

                        var lData15m = await _apiService.GetData_Bybit(item, EInterval.M15, DateTimeOffset.Now.AddDays(-50).ToUnixTimeMilliseconds());
                        if (!lData15m.Any())
                            continue;
                        var last = lData15m.Last();
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

                        var lData10 = await _apiService.GetData_Bybit(item, EInterval.M15, DateTimeOffset.Now.AddDays(-10).ToUnixTimeMilliseconds());
                        Thread.Sleep(200);
                        lData15m.AddRange(lData10.Where(x => x.Date > last.Date));
                        var lbb = lData15m.GetBollingerBands();
                        Quote close = null;
                        foreach (var ma20 in lbb)
                        {
                            try
                            {
                                if (close != null && close.Date >= ma20.Date)
                                    continue;
                                var side = (int)Binance.Net.Enums.OrderSide.Buy;
                                var cur = lData15m.First(x => x.Date == ma20.Date);
                                if (ma20.Sma is null
                                    || cur.Open <= cur.Close
                                    || cur.Close >= (decimal)ma20.Sma.Value
                                    || cur.Open <= (decimal)ma20.Sma.Value)
                                    continue;

                                var prev = lData15m.First(x => x.Date == ma20.Date.AddMinutes(-15));
                                var ma20_Prev = lbb.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(-15));
                                if (ma20_Prev.Sma is null || prev.Low < (decimal)ma20_Prev.Sma.Value)
                                    continue;

                                var prev2 = lData15m.First(x => x.Date == ma20.Date.AddMinutes(-30));
                                var ma20_Prev2 = lbb.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(-30));
                                if (ma20_Prev2.Sma is null || prev2.Low < (decimal)ma20_Prev2.Sma.Value)
                                    continue;

                                //var prev3 = lData15m.First(x => x.Date == ma20.Date.AddMinutes(-45));
                                //var ma20_Prev3 = lbb.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(-45));
                                //if (ma20_Prev3.Sma is null || prev3.High > (decimal)ma20_Prev3.Sma.Value)
                                //    continue;

                                var next = lData15m.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));
                                if (next is null || next.High <= cur.Close)
                                    continue;

                                var rateEntry = -Math.Round(100 * (-1 + next.High / cur.Close), 1);// tỉ lệ từ entry đến giá thấp nhất

                                var lPrev = lData15m.Where(x => x.Date < cur.Date).TakeLast(5);
                                var indexPrev = 0;
                                decimal totalPrev = 0;
                                foreach (var itemPrev in lPrev)
                                {
                                    if (itemPrev.High == itemPrev.Low)
                                    {
                                        continue;
                                    }
                                    var prevRate = Math.Round(Math.Abs(itemPrev.Open - itemPrev.Close) * 100 / Math.Abs(itemPrev.High - itemPrev.Low));
                                    if (prevRate > 10)
                                    {
                                        indexPrev++;
                                        totalPrev += Math.Abs(itemPrev.Open - itemPrev.Close);
                                    }
                                }
                                if (indexPrev <= 0)
                                    continue;

                                var avgPrev = totalPrev / indexPrev;
                                if (Math.Abs(cur.Open - cur.Close) <= 2 * avgPrev
                                    || Math.Abs(cur.Open - cur.Close) > avgPrev * 4)
                                    continue;

                                var checkBB = ((decimal)ma20.Sma) - cur.Close > cur.Close - (decimal)ma20.LowerBand;
                                if (checkBB)
                                    continue;

                                //var checkMa20 = ((decimal)ma20.Sma - cur.Close) * 2 < (cur.Open - (decimal)ma20.Sma);
                                //if (checkMa20)
                                //    continue;

                                var eEntry = cur;
                                var eClose = lData15m.FirstOrDefault(x => x.Date >= eEntry.Date.AddHours(hour));
                                if (eClose is null)
                                    continue;

                                var lClose = lData15m.Where(x => x.Date > eEntry.Date && x.Date <= eEntry.Date.AddHours(hour));
                                foreach (var itemClose in lClose)
                                {
                                    var ma = lbb.First(x => x.Date == itemClose.Date);
                                    if (itemClose.Close < (decimal)ma.LowerBand)
                                    {
                                        eClose = itemClose;
                                        break;
                                    }
                                }

                                close = eClose;
                                var rate = Math.Round(100 * (-1 + eEntry.Close / eClose.Close), 1);
                                var lRange = lData15m.Where(x => x.Date >= eEntry.Date.AddMinutes(15) && x.Date <= eClose.Date);
                                var maxH = lRange.Max(x => x.High);
                                var minL = lRange.Min(x => x.Low);

                                var winloss = "W";
                                if (rate <= 0)
                                {
                                    winloss = "L";
                                }

                                decimal maxTP = 0, maxSL = 0;
                                maxTP = Math.Round(100 * (-1 + eEntry.Close / minL), 1);
                                maxSL = Math.Round(100 * (-1 + eEntry.Close / maxH), 1);
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

                                lRate.Add(rate);
                                lModel.Add(new LongMa20
                                {
                                    s = item,
                                    IsWin = winloss == "W",
                                    Date = cur.Date,
                                    Rate = rate,
                                    MaxTP = maxTP,
                                    MaxSL = maxSL,
                                    RateEntry = rateEntry,
                                });
                                var mes = $"{item}|{winloss}|{((Binance.Net.Enums.OrderSide)side).ToString()}|{cur.Date.ToString("dd/MM/yyyy HH:mm")}|{rate}%|TPMax: {maxTP}%|SLMax: {maxSL}%|RateEntry: {rateEntry}%";
                                lMes.Add(mes);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
                            }

                        }

                        lMesAll.AddRange(lMes);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                   
                }

                foreach (var mes in lMesAll)
                {
                    Console.WriteLine(mes);
                }
                Console.WriteLine($"Tong: {lRate.Sum()}%|W/L: {winCount}/{lossCount}");

                var tmp = lModel.Select(x => x.Date).Distinct();
                var tmp1 = 1;

                // Note:
                // + Nến xanh cắt lên MA20
                // + 2 nến ngay phía trước đều nằm dưới MA20
                // + Vol nến hiện tại > ít nhất 8/9 nến trước đó
                // + Giữ 2 tiếng? hoặc nến chạm BB trên
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
            }
        }

        //LONG RSI Tong(54): 915.9%|W/L: 573/285
        public async Task CheckAllBINANCE_LONG()
        {
            try
            {
                var lAll = await StaticVal.BinanceInstance().UsdFuturesApi.CommonFuturesClient.GetSymbolsAsync();
                var lUsdt = lAll.Data.Where(x => x.Name.EndsWith("USDT")).Select(x => x.Name);
                var countUSDT = lUsdt.Count();//461
                var lTake = lUsdt.ToList();
                //var lTake = lUsdt.Skip(0).Take(50).ToList();
                //2x1.7 best
                //decimal SL_RATE = 1.7m;//1.5,1.6,1.8,1.9,2
                decimal SL_RATE = 2.5m;//1.5,1.6,1.8,1.9,2
                int hour = 4;//1h,2h,3h,4h

                var lMesAll = new List<string>();
                var lModel = new List<LongMa20>();
               
                var winTotal = 0;
                var lossTotal = 0;

                #region comment
                lTake.Clear();
                var lTmp = new List<string>
                {
                    "MUBARAKUSDT",
                    "1000WHYUSDT",
                    "B3USDT",
                    "LUMIAUSDT",
                    "1000BONKUSDT",
                    "EPICUSDT",
                    "INJUSDT",
                    "GRTUSDT",
                    "ORDIUSDT",
                    "ALTUSDT",
                    "ARKMUSDT",
                    "QTUMUSDT",
                    "PLUMEUSDT",
                    "VINEUSDT",
                    "PHBUSDT",
                    "SAFEUSDT",
                    "ZRXUSDT",
                    "CETUSUSDT",
                    "ATAUSDT",
                    "THEUSDT",
                    "DODOXUSDT",
                    "ADAUSDT",
                    "DFUSDT",
                    "TOKENUSDT",
                    "KSMUSDT",
                    "PROMUSDT",
                    "BSWUSDT",
                    "MTLUSDT",
                    "ONDOUSDT",
                    "1000FLOKIUSDT",
                    "ETHFIUSDT",
                    "SEIUSDT",
                    "RAREUSDT",
                    "AXLUSDT",
                    "VIRTUALUSDT",
                    "PHAUSDT",
                    "JOEUSDT",
                    "BTCUSDT",
                    "RAYSOLUSDT",
                    "VTHOUSDT",
                    "OMUSDT",
                    "DEGENUSDT",
                    "ZILUSDT",
                    "SANDUSDT",
                    "MASKUSDT",
                    "BEAMXUSDT",
                    "RENDERUSDT",
                    "IOTAUSDT",
                    "ANKRUSDT",
                    "BIOUSDT",
                    "ETCUSDT",
                    "CAKEUSDT",
                    "ALICEUSDT",
                    "CATIUSDT"
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
                        var lMes = new List<string>();

                        var lData15m = await _apiService.GetData_Binance(item, EInterval.M15, DateTimeOffset.Now.AddDays(-50).ToUnixTimeMilliseconds());
                        if (lData15m == null || !lData15m.Any())
                            continue;
                        var last = lData15m.Last();
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

                        var lData10 = await _apiService.GetData_Binance(item, EInterval.M15, DateTimeOffset.Now.AddDays(-10).ToUnixTimeMilliseconds());
                        Thread.Sleep(200);
                        lData15m.AddRange(lData10.Where(x => x.Date > last.Date));
                        var lbb = lData15m.GetBollingerBands();
                        var lrsi = lData15m.GetRsi();

                        DateTime dtFlag = DateTime.MinValue;
                        //var count = 0;
                        foreach (var ma20 in lbb)
                        {
                            try
                            {
                                if (dtFlag >= ma20.Date)
                                    continue;

                                //if (ma20.Date.Month == 2 && ma20.Date.Day == 28 && ma20.Date.Hour == 1 && ma20.Date.Minute == 30)
                                //{
                                //    var z = 1;
                                //}

                                var side = (int)Binance.Net.Enums.OrderSide.Buy;
                                var cur = lData15m.First(x => x.Date == ma20.Date);
                                var rsi = lrsi.First(x => x.Date == ma20.Date);
                                var maxOpenClose = Math.Max(cur.Open, cur.Close);
                                var minOpenClose = Math.Min(cur.Open, cur.Close);

                                if (cur.Close >= cur.Open
                                    || ma20.Sma is null
                                    || rsi.Rsi > 35
                                    || cur.Low >= (decimal)ma20.LowerBand.Value
                                    //|| cur.High >= (decimal)ma20.Sma.Value
                                    || Math.Abs(minOpenClose - (decimal)ma20.LowerBand.Value) > Math.Abs((decimal)ma20.Sma.Value - minOpenClose)
                                    )
                                    continue;

                                var rsiPivot = lrsi.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));
                                if (rsiPivot is null || rsiPivot.Rsi > 35 || rsiPivot.Rsi < 25)
                                    continue;

                                var pivot = lData15m.First(x => x.Date == ma20.Date.AddMinutes(15));
                                var bbPivot = lbb.First(x => x.Date == ma20.Date.AddMinutes(15));
                                if (pivot.Low >= (decimal)bbPivot.LowerBand.Value
                                    || pivot.High >= (decimal)bbPivot.Sma.Value
                                    || (pivot.Low >= cur.Low && pivot.High <= cur.High))
                                    continue;

                                var rateVol = Math.Round(pivot.Volume / cur.Volume, 1);
                                //if (rateVol > (decimal)0.6 || rateVol < (decimal)0.4) //Vol hiện tại phải nhỏ hơn hoặc bằng 0.6 lần vol của nến liền trước
                                if (rateVol > (decimal)0.6) //Vol hiện tại phải nhỏ hơn hoặc bằng 0.6 lần vol của nến liền trước
                                    continue;

                                //độ dài nến hiện tại
                                var rateCur = Math.Abs((cur.Open - cur.Close) / (cur.High - cur.Low));
                                if(rateCur > (decimal)0.8)
                                {
                                    //check độ dài nến pivot
                                    var isValid = Math.Abs(pivot.Open - pivot.Close) >= Math.Abs(cur.Open - cur.Close);
                                    if (isValid)
                                        continue;
                                }

                                cur = pivot;
                                
                                var next = lData15m.FirstOrDefault(x => x.Date == cur.Date.AddMinutes(15));
                                if (next is null)
                                    continue;
                                var rateEntry = Math.Round(100 * (-1 + next.Low / cur.Close), 1);// tỉ lệ từ entry đến giá thấp nhất

                                var eEntry = cur;
                                var eClose = lData15m.FirstOrDefault(x => x.Date >= eEntry.Date.AddHours(hour));
                                if (eClose is null)
                                    continue;

                                var lClose = lData15m.Where(x => x.Date > eEntry.Date && x.Date <= eEntry.Date.AddHours(hour));
                                foreach (var itemClose in lClose)
                                {
                                    var ma = lbb.First(x => x.Date == itemClose.Date);
                                    if (itemClose.Close > (decimal)ma.UpperBand)
                                    {
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
                                if (rate <= (decimal)0)
                                {
                                    winloss = "L";
                                }

                                decimal maxTP = 0, maxSL = 0;
                                if (side == (int)Binance.Net.Enums.OrderSide.Buy)
                                {
                                    maxTP = Math.Round(100 * (-1 + maxH / eEntry.Close), 1);
                                    maxSL = Math.Round(100 * (-1 + minL / eEntry.Close), 1);
                                }
                                else
                                {
                                    maxTP = Math.Round(100 * (-1 + eEntry.Close / minL), 1);
                                    maxSL = Math.Round(100 * (-1 + eEntry.Close / maxH), 1);
                                }
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
                                    Date = cur.Date,
                                    Rate = rate,
                                    MaxTP = maxTP,
                                    MaxSL = maxSL,
                                    RateEntry = rateEntry,
                                });
                                var mes = $"{item}|{winloss}|{((Binance.Net.Enums.OrderSide)side).ToString()}|{cur.Date.ToString("dd/MM/yyyy HH:mm")}|{rate}%|TPMax: {maxTP}%|SLMax: {maxSL}%|RateEntry: {rateEntry}%|RSI: {rsiPivot.Rsi}";
                                lMes.Add(mes);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
                            }

                        }

                        //Console.WriteLine(count);
                        //return;

                        //foreach (var mes in lMes)
                        //{
                        //    Console.WriteLine(mes);
                        //}
                        //
                        if (winCount <= lossCount)
                            continue;
                        var rateRes = Math.Round(((decimal)winCount / (winCount + lossCount)), 2);
                        if (rateRes > (decimal)0.5 && winCount > 3)
                        {
                            var sumRate = lModel.Where(x => x.s == item).Sum(x => x.Rate);
                            if (sumRate <= 1)
                            {
                                var lRemove = lModel.Where(x => x.s == item);
                                lModel = lModel.Except(lRemove).ToList();
                                continue;
                            }
                            //Console.WriteLine($"{item}: {rateRes}({winCount}/{lossCount})");
                            lMesAll.AddRange(lMes);
                            //foreach (var mes in lMes)
                            //{
                            //    Console.WriteLine(mes);
                            //}
                            var realWin = 0;
                            foreach (var model in lModel.Where(x => x.s == item))
                            {
                                if(model.Rate > (decimal)0)
                                    realWin++;
                            }
                            var count = lModel.Count(x => x.s == item);
                            
                            if (sumRate / count <= (decimal)0.5)
                            {
                                var lRemove = lModel.Where(x => x.s == item);
                                lModel = lModel.Except(lRemove).ToList();
                                continue;
                            }
                            var rate = Math.Round((double)realWin / count, 1);
                            var perRate = Math.Round((float)sumRate / count, 1);
                            if (perRate < 0.7)
                            {
                                var lRemove = lModel.Where(x => x.s == item);
                                lModel = lModel.Except(lRemove).ToList();
                                continue;
                            }
                            Console.WriteLine($"{item}| W/Total: {realWin}/{lModel.Count(x => x.s == item)} = {rate}%|Rate: {sumRate}%|Per: {perRate}%");

                            winTotal += winCount;
                            lossTotal += lossCount;
                            winCount = 0;
                            lossCount = 0;
                        }

                      
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                }

                //foreach (var mes in lMesAll)
                //{
                //    Console.WriteLine(mes);
                //}
                Console.WriteLine($"Tong: {lModel.Sum(x => x.Rate)}%|W/L: {winTotal}/{lossTotal}");

                // Note:
                // + Nến xanh cắt lên MA20
                // + 2 nến ngay phía trước đều nằm dưới MA20
                // + Vol nến hiện tại > ít nhất 8/9 nến trước đó
                // + Giữ 2 tiếng? hoặc nến chạm BB trên
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
            }
        }
        //SHORT RSI Tong(52): 850.7%|W/L: 561/313
        public async Task CheckAllBINANCE_SHORT()
        {
            try
            {
                var lAll = await StaticVal.BinanceInstance().UsdFuturesApi.CommonFuturesClient.GetSymbolsAsync();
                var lUsdt = lAll.Data.Where(x => x.Name.EndsWith("USDT")).Select(x => x.Name);
                var countUSDT = lUsdt.Count();//461
                var lTake = lUsdt.ToList();
                //var lTake = lUsdt.Skip(0).Take(50).ToList();
                //2x1.7 best
                //decimal SL_RATE = 1.7m;//1.5,1.6,1.8,1.9,2
                decimal SL_RATE = 2.5m;//1.5,1.6,1.8,1.9,2
                int hour = 4;//1h,2h,3h,4h

                var lMesAll = new List<string>();
                var lModel = new List<LongMa20>();

                var winTotal = 0;
                var lossTotal = 0;

                #region comment
                lTake.Clear();
                var lTmp = new List<string>
                {
                    "BROCCOLI714USDT",
                    "NILUSDT",
                    "HEIUSDT",
                    "OMUSDT",
                    "CATIUSDT",
                    "GRIFFAINUSDT",
                    "GPSUSDT",
                    "DUSKUSDT",
                    "NEIROUSDT",
                    "PONKEUSDT",
                    "LINKUSDT",
                    "MORPHOUSDT",
                    "AVAAIUSDT",
                    "VICUSDT",
                    "KASUSDT",
                    "DYDXUSDT",
                    "BADGERUSDT",
                    "BAKEUSDT",
                    "CAKEUSDT",
                    "ONDOUSDT",
                    "OMNIUSDT",
                    "1MBABYDOGEUSDT",
                    "SAFEUSDT",
                    "1000000MOGUSDT",
                    "RUNEUSDT",
                    "BERAUSDT",
                    "MANTAUSDT",
                    "AGLDUSDT",
                    "CRVUSDT",
                    "TRUUSDT",
                    "TWTUSDT",
                    "DEXEUSDT",
                    "IPUSDT",
                    "CHESSUSDT",
                    "PROMUSDT",
                    "AXSUSDT",
                    "ANKRUSDT",
                    "CTSIUSDT",
                    "ACHUSDT",
                    "EDUUSDT",
                    "ETHWUSDT",
                    "AIUSDT",
                    "XAIUSDT",
                    "FIOUSDT",
                    "MOODENGUSDT",
                    "QTUMUSDT",
                    "ALPHAUSDT",
                    "APEUSDT",
                    "ORDIUSDT",
                    "LSKUSDT",
                    "LISTAUSDT",
                    "EIGENUSDT",
                    "MOVEUSDT",
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
                        var lMes = new List<string>();

                        var lData15m = await _apiService.GetData_Binance(item, EInterval.M15, DateTimeOffset.Now.AddDays(-50).ToUnixTimeMilliseconds());
                        if (lData15m == null || !lData15m.Any())
                            continue;
                        var last = lData15m.Last();
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

                        var lData10 = await _apiService.GetData_Binance(item, EInterval.M15, DateTimeOffset.Now.AddDays(-10).ToUnixTimeMilliseconds());
                        Thread.Sleep(200);
                        lData15m.AddRange(lData10.Where(x => x.Date > last.Date));
                        var lbb = lData15m.GetBollingerBands();
                        var lrsi = lData15m.GetRsi();

                        DateTime dtFlag = DateTime.MinValue;
                        //var count = 0;
                        foreach (var ma20 in lbb)
                        {
                            try
                            {
                                if (dtFlag >= ma20.Date)
                                    continue;

                                //if (ma20.Date.Month == 3 && ma20.Date.Day == 17 && ma20.Date.Hour == 18 && ma20.Date.Minute == 00)
                                //{
                                //    var z = 1;
                                //}

                                var side = (int)Binance.Net.Enums.OrderSide.Sell;
                                var cur = lData15m.First(x => x.Date == ma20.Date);
                                var rsi = lrsi.First(x => x.Date == ma20.Date);
                                var maxOpenClose = Math.Max(cur.Open, cur.Close);

                                if (
                                    cur.Close <= cur.Open
                                   || ma20.Sma is null
                                   || rsi.Rsi < 65
                                   || cur.High <= (decimal)ma20.UpperBand.Value
                                   //|| cur.High >= (decimal)ma20.Sma.Value
                                   || Math.Abs(maxOpenClose - (decimal)ma20.UpperBand.Value) > Math.Abs((decimal)ma20.Sma.Value - maxOpenClose)
                                   )
                                    continue;

                                var rsiPivot = lrsi.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));
                                if (rsiPivot is null || rsiPivot.Rsi > 80 || rsiPivot.Rsi < 65)
                                    continue;

                                var pivot = lData15m.First(x => x.Date == ma20.Date.AddMinutes(15));
                                var bbPivot = lbb.First(x => x.Date == ma20.Date.AddMinutes(15));
                                if (pivot.High <= (decimal)bbPivot.UpperBand.Value
                                    || pivot.Low <= (decimal)bbPivot.Sma.Value
                                    //|| (pivot.Low >= cur.Low && pivot.High <= cur.High)
                                    )
                                    continue;

                                var rateVol = Math.Round(pivot.Volume / cur.Volume, 1);
                                //if (rateVol > (decimal)0.6 || rateVol < (decimal)0.4) //Vol hiện tại phải nhỏ hơn hoặc bằng 0.6 lần vol của nến liền trước
                                if (rateVol > (decimal)0.6) //Vol hiện tại phải nhỏ hơn hoặc bằng 0.6 lần vol của nến liền trước
                                    continue;

                                //check div by zero
                                if (cur.High == cur.Low
                                    || pivot.High == pivot.Low
                                    || Math.Min(pivot.Open, pivot.Close) == pivot.Low)
                                    continue;

                                var rateCur = Math.Abs((cur.Open - cur.Close) / (cur.High - cur.Low));  //độ dài nến hiện tại
                                var ratePivot = Math.Abs((pivot.Open - pivot.Close) / (pivot.High - pivot.Low));  //độ dài nến pivot
                                var isHammer = (cur.High - cur.Close) >= (decimal)1.2 * (cur.Close - cur.Low);

                                if (isHammer)
                                {

                                }
                                else if(ratePivot < (decimal)0.2)
                                {
                                    var checkDoji = (pivot.High - Math.Max(pivot.Open, pivot.Close)) / (Math.Min(pivot.Open, pivot.Close) - pivot.Low);
                                    if (checkDoji >= (decimal)0.75 && checkDoji <= (decimal)1.25)
                                    {
                                        continue;
                                    }
                                }
                                else if (rateCur > (decimal)0.8)
                                {
                                    //check độ dài nến pivot
                                    var isValid = Math.Abs(pivot.Open - pivot.Close) >= Math.Abs(cur.Open - cur.Close);
                                    if (isValid)
                                        continue;
                                }
                                

                                cur = pivot;

                                var next = lData15m.FirstOrDefault(x => x.Date == cur.Date.AddMinutes(15));
                                if (next is null)
                                    continue;
                                var rateEntry = Math.Round(100 * (-1 + cur.Close / next.High), 1);// tỉ lệ từ entry đến giá thấp nhất

                                var eEntry = cur;
                                var eClose = lData15m.FirstOrDefault(x => x.Date >= eEntry.Date.AddHours(hour));
                                if (eClose is null)
                                    continue;

                                var lClose = lData15m.Where(x => x.Date > eEntry.Date && x.Date <= eEntry.Date.AddHours(hour));
                                foreach (var itemClose in lClose)
                                {
                                    var ma = lbb.First(x => x.Date == itemClose.Date);
                                    if (itemClose.Close < (decimal)ma.LowerBand)
                                    {
                                        eClose = itemClose;
                                        break;
                                    }
                                }

                                dtFlag = eClose.Date;
                                var rate = Math.Round(100 * (-1 + eEntry.Close / eClose.Close ), 1);
                                var lRange = lData15m.Where(x => x.Date >= eEntry.Date.AddMinutes(15) && x.Date <= eClose.Date);
                                var maxH = lRange.Max(x => x.High);
                                var minL = lRange.Min(x => x.Low);

                                var winloss = "W";
                                if (rate <= (decimal)0)
                                {
                                    winloss = "L";
                                }

                                decimal maxTP = 0, maxSL = 0;
                                if (side == (int)Binance.Net.Enums.OrderSide.Buy)
                                {
                                    maxTP = Math.Round(100 * (-1 + maxH / eEntry.Close), 1);
                                    maxSL = Math.Round(100 * (-1 + minL / eEntry.Close), 1);
                                }
                                else
                                {
                                    maxTP = Math.Round(100 * (-1 + eEntry.Close / minL), 1);
                                    maxSL = Math.Round(100 * (-1 + eEntry.Close / maxH), 1);
                                }
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
                                    Date = cur.Date,
                                    Rate = rate,
                                    MaxTP = maxTP,
                                    MaxSL = maxSL,
                                    RateEntry = rateEntry,
                                });
                                var mes = $"{item}|{winloss}|{((Binance.Net.Enums.OrderSide)side).ToString()}|{cur.Date.ToString("dd/MM/yyyy HH:mm")}|{rate}%|TPMax: {maxTP}%|SLMax: {maxSL}%|RateEntry: {rateEntry}%|RSI: {rsiPivot.Rsi}";
                                lMes.Add(mes);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
                            }

                        }

                        //Console.WriteLine(count);
                        //return;

                        //foreach (var mes in lMes)
                        //{
                        //    Console.WriteLine(mes);
                        //}
                        //
                        if (winCount <= lossCount)
                            continue;
                        var rateRes = Math.Round(((decimal)winCount / (winCount + lossCount)), 2);
                        if (rateRes > (decimal)0.5 && winCount > 3)
                        {
                            var sumRate = lModel.Where(x => x.s == item).Sum(x => x.Rate);
                            if (sumRate <= 1)
                            {
                                var lRemove = lModel.Where(x => x.s == item);
                                lModel = lModel.Except(lRemove).ToList();
                                continue;
                            }
                                
                            //Console.WriteLine($"{item}: {rateRes}({winCount}/{lossCount})");
                            lMesAll.AddRange(lMes);
                            //foreach (var mes in lMes)
                            //{
                            //    Console.WriteLine(mes);
                            //}
                            var realWin = 0;
                            foreach (var model in lModel.Where(x => x.s == item))
                            {
                                if (model.Rate > (decimal)0)
                                    realWin++;
                            }
                            var count = lModel.Count(x => x.s == item);
                            if (sumRate / count <= (decimal)0.5)
                            {
                                var lRemove = lModel.Where(x => x.s == item);
                                lModel = lModel.Except(lRemove).ToList();
                                continue;
                            }
                            var rate = Math.Round((double)realWin / count, 1);
                            var perRate = Math.Round((float)sumRate / count, 1);
                            if(perRate < 0.7)
                            {
                                var lRemove = lModel.Where(x => x.s == item);
                                lModel = lModel.Except(lRemove).ToList();
                                continue;
                            }

                            Console.WriteLine($"{item}| W/Total: {realWin}/{lModel.Count(x => x.s == item)} = {rate}%|Rate: {sumRate}%|Per: {perRate}%");

                            winTotal += winCount;
                            lossTotal += lossCount;
                            winCount = 0;
                            lossCount = 0;
                        }


                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                }

                //foreach (var mes in lMesAll)
                //{
                //    Console.WriteLine(mes);
                //}
                Console.WriteLine($"Tong: {lModel.Sum(x => x.Rate)}%|W/L: {winTotal}/{lossTotal}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
            }
        }

        //LONG RSI Tong(60): 1045.5%|W/L: 731/476
        public async Task CheckAllBYBIT_LONG()
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

                var lMesAll = new List<string>();
                var lModel = new List<LongMa20>();

                var winTotal = 0;
                var lossTotal = 0;

                //#region comment
                //lTake.Clear();
                //var lTmp = new List<string>
                //{
                //    //Tier 1
                //    "VIDTUSDT",
                //    "EPICUSDT",
                //    "DGBUSDT",
                //    "MASAUSDT",
                //    "BRUSDT",//
                //    "BEAMUSDT",//
                //    "MEMEUSDT",//
                //    "PENDLEUSDT",
                //    "NEIROETHUSDT",
                //    "GMTUSDT",//
                //    "GPSUSDT",
                //    "CFXUSDT",
                //    "MAGICUSDT",
                //    "SLFUSDT",
                //    "SANDUSDT",
                //    "FIDAUSDT",
                //    "XCHUSDT",
                //    "VIRTUALUSDT",
                //    "BIGTIMEUSDT",
                //    "BNBUSDT",
                //    "ETHFIUSDT",
                //    "LUCEUSDT",
                //    "RAREUSDT",
                //    "AERGOUSDT",
                //    "ALICEUSDT",
                //    "PHBUSDT",
                //    ////Tire 2
                //    "MASKUSDT",
                //    "NEOUSDT",
                //    "VRUSDT",
                //    "ENJUSDT",
                //    "SEIUSDT",
                //    "SNXUSDT",
                //    "PROMUSDT",
                //    "SOLOUSDT",
                //    "TOKENUSDT",
                //    "WUSDT",
                //    "ZBCNUSDT",
                //    "ZILUSDT",
                //    "ADAUSDT",
                //    "ALTUSDT",
                //    "ANKRUSDT",
                //    "ARKMUSDT",
                //    "BATUSDT",
                //    "BTCUSDT",
                //    "DOGSUSDT",
                //    "FILUSDT",
                //    "FLOCKUSDT",
                //    "FLUXUSDT",
                //    "GLMRUSDT",
                //    "GLMUSDT",
                //    "INJUSDT",
                //    "KAVAUSDT",
                //    "LQTYUSDT",
                //    "MEMEFIUSDT",
                //    "NCUSDT",
                //    "PLUMEUSDT",
                //    "PORTALUSDT",
                //    "QTUMUSDT",
                //    "RAYDIUMUSDT",
                //};
                //lTake.AddRange(lTmp);
                //#endregion
                foreach (var item in lTake)
                {

                    var winCount = 0;
                    var lossCount = 0;
                    try
                    {
                        //if (item != "BTCUSDT")
                        //    continue;
                        var lMes = new List<string>();

                        var lData15m = await _apiService.GetData_Bybit(item, EInterval.M15, DateTimeOffset.Now.AddDays(-50).ToUnixTimeMilliseconds());
                        if (lData15m == null || !lData15m.Any())
                            continue;
                        var last = lData15m.Last();
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

                        var lData10 = await _apiService.GetData_Bybit(item, EInterval.M15, DateTimeOffset.Now.AddDays(-10).ToUnixTimeMilliseconds());
                        Thread.Sleep(200);
                        lData15m.AddRange(lData10.Where(x => x.Date > last.Date));
                        var lbb = lData15m.GetBollingerBands();
                        var lrsi = lData15m.GetRsi();

                        DateTime dtFlag = DateTime.MinValue;
                        //var count = 0;
                        foreach (var ma20 in lbb)
                        {
                            try
                            {
                                if (dtFlag >= ma20.Date)
                                    continue;

                                //if (ma20.Date.Month == 2 && ma20.Date.Day == 28 && ma20.Date.Hour == 1 && ma20.Date.Minute == 30)
                                //{
                                //    var z = 1;
                                //}

                                var side = (int)Binance.Net.Enums.OrderSide.Buy;
                                var cur = lData15m.First(x => x.Date == ma20.Date);
                                var rsi = lrsi.First(x => x.Date == ma20.Date);
                                var maxOpenClose = Math.Max(cur.Open, cur.Close);
                                var minOpenClose = Math.Min(cur.Open, cur.Close);

                                if (cur.Close >= cur.Open
                                    || ma20.Sma is null
                                    || rsi.Rsi > 35
                                    || cur.Low >= (decimal)ma20.LowerBand.Value
                                    //|| cur.High >= (decimal)ma20.Sma.Value
                                    || Math.Abs(minOpenClose - (decimal)ma20.LowerBand.Value) > Math.Abs((decimal)ma20.Sma.Value - minOpenClose)
                                    )
                                    continue;

                                var rsiPivot = lrsi.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));
                                if (rsiPivot is null || rsiPivot.Rsi > 35 || rsiPivot.Rsi < 25)
                                    continue;

                                var pivot = lData15m.First(x => x.Date == ma20.Date.AddMinutes(15));
                                var bbPivot = lbb.First(x => x.Date == ma20.Date.AddMinutes(15));
                                if (pivot.Low >= (decimal)bbPivot.LowerBand.Value
                                    || pivot.High >= (decimal)bbPivot.Sma.Value
                                    || (pivot.Low >= cur.Low && pivot.High <= cur.High))
                                    continue;

                                var rateVol = Math.Round(pivot.Volume / cur.Volume, 1);
                                //if (rateVol > (decimal)0.6 || rateVol < (decimal)0.4) //Vol hiện tại phải nhỏ hơn hoặc bằng 0.6 lần vol của nến liền trước
                                if (rateVol > (decimal)0.6) //Vol hiện tại phải nhỏ hơn hoặc bằng 0.6 lần vol của nến liền trước
                                    continue;

                                //độ dài nến hiện tại
                                var rateCur = Math.Abs((cur.Open - cur.Close) / (cur.High - cur.Low));
                                if (rateCur > (decimal)0.8)
                                {
                                    //check độ dài nến pivot
                                    var isValid = Math.Abs(pivot.Open - pivot.Close) >= Math.Abs(cur.Open - cur.Close);
                                    if (isValid)
                                        continue;
                                }

                                cur = pivot;

                                var next = lData15m.FirstOrDefault(x => x.Date == cur.Date.AddMinutes(15));
                                if (next is null)
                                    continue;
                                var rateEntry = Math.Round(100 * (-1 + next.Low / cur.Close), 1);// tỉ lệ từ entry đến giá thấp nhất

                                var eEntry = cur;
                                var eClose = lData15m.FirstOrDefault(x => x.Date >= eEntry.Date.AddHours(hour));
                                if (eClose is null)
                                    continue;

                                var lClose = lData15m.Where(x => x.Date > eEntry.Date && x.Date <= eEntry.Date.AddHours(hour));
                                foreach (var itemClose in lClose)
                                {
                                    var ma = lbb.First(x => x.Date == itemClose.Date);
                                    if (itemClose.Close > (decimal)ma.UpperBand)
                                    {
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
                                if (rate <= (decimal)0)
                                {
                                    winloss = "L";
                                }

                                decimal maxTP = 0, maxSL = 0;
                                if (side == (int)Binance.Net.Enums.OrderSide.Buy)
                                {
                                    maxTP = Math.Round(100 * (-1 + maxH / eEntry.Close), 1);
                                    maxSL = Math.Round(100 * (-1 + minL / eEntry.Close), 1);
                                }
                                else
                                {
                                    maxTP = Math.Round(100 * (-1 + eEntry.Close / minL), 1);
                                    maxSL = Math.Round(100 * (-1 + eEntry.Close / maxH), 1);
                                }
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
                                    Date = cur.Date,
                                    Rate = rate,
                                    MaxTP = maxTP,
                                    MaxSL = maxSL,
                                    RateEntry = rateEntry,
                                });
                                var mes = $"{item}|{winloss}|{((Binance.Net.Enums.OrderSide)side).ToString()}|{cur.Date.ToString("dd/MM/yyyy HH:mm")}|{rate}%|TPMax: {maxTP}%|SLMax: {maxSL}%|RateEntry: {rateEntry}%|RSI: {rsiPivot.Rsi}";
                                lMes.Add(mes);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
                            }

                        }

                        //Console.WriteLine(count);
                        //return;

                        //foreach (var mes in lMes)
                        //{
                        //    Console.WriteLine(mes);
                        //}
                        //
                        if (winCount <= lossCount)
                            continue;
                        var rateRes = Math.Round(((decimal)winCount / (winCount + lossCount)), 2);
                        if (rateRes > (decimal)0.5 && winCount > 3)
                        {
                            var sumRate = lModel.Where(x => x.s == item).Sum(x => x.Rate);
                            if (sumRate <= 1)
                            {
                                var lRemove = lModel.Where(x => x.s == item);
                                lModel = lModel.Except(lRemove).ToList();
                                continue;
                            }
                            //Console.WriteLine($"{item}: {rateRes}({winCount}/{lossCount})");
                            lMesAll.AddRange(lMes);
                            //foreach (var mes in lMes)
                            //{
                            //    Console.WriteLine(mes);
                            //}
                            var realWin = 0;
                            foreach (var model in lModel.Where(x => x.s == item))
                            {
                                if (model.Rate > (decimal)0)
                                    realWin++;
                            }
                            var count = lModel.Count(x => x.s == item);
                            if (sumRate / count <= (decimal)0.5)
                            {
                                var lRemove = lModel.Where(x => x.s == item);
                                lModel = lModel.Except(lRemove).ToList();
                                continue;
                            }

                            var rate = Math.Round((double)realWin / count, 1);
                            var perRate = Math.Round((float)sumRate / count, 1);
                            if (perRate < 0.7)
                            {
                                var lRemove = lModel.Where(x => x.s == item);
                                lModel = lModel.Except(lRemove).ToList();
                                continue;
                            }
                            Console.WriteLine($"{item}| W/Total: {realWin}/{lModel.Count(x => x.s == item)} = {rate}%|Rate: {sumRate}%|Per: {perRate}%");

                            winTotal += winCount;
                            lossTotal += lossCount;
                            winCount = 0;
                            lossCount = 0;
                        }


                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                }

                //foreach (var mes in lMesAll)
                //{
                //    Console.WriteLine(mes);
                //}
                Console.WriteLine($"Tong: {lModel.Sum(x => x.Rate)}%|W/L: {winTotal}/{lossTotal}");

                // Note:
                // + Nến xanh cắt lên MA20
                // + 2 nến ngay phía trước đều nằm dưới MA20
                // + Vol nến hiện tại > ít nhất 8/9 nến trước đó
                // + Giữ 2 tiếng? hoặc nến chạm BB trên
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
            }
        }
        //SHORT RSI Tong(49): 979.3%|W/L: 577/333
        public async Task CheckAllBYBIT_SHORT()
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

                var lMesAll = new List<string>();
                var lModel = new List<LongMa20>();

                var winTotal = 0;
                var lossTotal = 0;

                #region comment
                lTake.Clear();
                var lTmp = new List<string>
                {
                    "NILUSDT",
                    "HEIUSDT",
                    "JUSDT",
                    "CATIUSDT",
                    "OLUSDT",
                    "MORPHOUSDT",
                    "NCUSDT",
                    "OMUSDT",
                    "IDEXUSDT",
                    "XVGUSDT",
                    "XVSUSDT",
                    "ZENUSDT",
                    "FWOGUSDT",
                    "KOMAUSDT",
                    "CAKEUSDT",
                    "FLUXUSDT",
                    "MANTAUSDT",
                    "VICUSDT",
                    "CPOOLUSDT",
                    "PENGUUSDT",
                    "DYDXUSDT",
                    "OMNIUSDT",
                    "TROYUSDT",
                    "XCHUSDT",
                    "PERPUSDT",
                    "ACHUSDT",
                    "ENSUSDT",
                    "SPECUSDT",
                    "XEMUSDT",
                    "KASUSDT",
                    "MAVIAUSDT",
                    "MOODENGUSDT",
                    "SPELLUSDT",
                    "BLASTUSDT",
                    "STGUSDT",
                    "ZBCNUSDT",
                    "AIOZUSDT",
                    "ALUUSDT",
                    "AXLUSDT",
                    "BLURUSDT",
                    "LSKUSDT",
                    "MOVRUSDT",
                    "SERAPHUSDT",
                    "TUSDT",
                    "VTHOUSDT",
                    "MEMEUSDT",
                    "ANKRUSDT",
                    "IPUSDT",
                    "LISTAUSDT",
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
                        var lMes = new List<string>();

                        var lData15m = await _apiService.GetData_Bybit(item, EInterval.M15, DateTimeOffset.Now.AddDays(-50).ToUnixTimeMilliseconds());
                        if (lData15m == null || !lData15m.Any())
                            continue;
                        var last = lData15m.Last();
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

                        var lData10 = await _apiService.GetData_Bybit(item, EInterval.M15, DateTimeOffset.Now.AddDays(-10).ToUnixTimeMilliseconds());
                        Thread.Sleep(200);
                        lData15m.AddRange(lData10.Where(x => x.Date > last.Date));
                        var lbb = lData15m.GetBollingerBands();
                        var lrsi = lData15m.GetRsi();

                        DateTime dtFlag = DateTime.MinValue;
                        //var count = 0;
                        foreach (var ma20 in lbb)
                        {
                            try
                            {
                                if (dtFlag >= ma20.Date)
                                    continue;

                                //if (ma20.Date.Month == 3 && ma20.Date.Day == 17 && ma20.Date.Hour == 18 && ma20.Date.Minute == 00)
                                //{
                                //    var z = 1;
                                //}

                                var side = (int)Binance.Net.Enums.OrderSide.Sell;
                                var cur = lData15m.First(x => x.Date == ma20.Date);
                                var rsi = lrsi.First(x => x.Date == ma20.Date);
                                var maxOpenClose = Math.Max(cur.Open, cur.Close);

                                if (
                                    cur.Close <= cur.Open
                                   || ma20.Sma is null
                                   || rsi.Rsi < 65
                                   || cur.High <= (decimal)ma20.UpperBand.Value
                                   //|| cur.High >= (decimal)ma20.Sma.Value
                                   || Math.Abs(maxOpenClose - (decimal)ma20.UpperBand.Value) > Math.Abs((decimal)ma20.Sma.Value - maxOpenClose)
                                   )
                                    continue;

                                var rsiPivot = lrsi.FirstOrDefault(x => x.Date == ma20.Date.AddMinutes(15));
                                if (rsiPivot is null || rsiPivot.Rsi > 80 || rsiPivot.Rsi < 65)
                                    continue;

                                var pivot = lData15m.First(x => x.Date == ma20.Date.AddMinutes(15));
                                var bbPivot = lbb.First(x => x.Date == ma20.Date.AddMinutes(15));
                                if (pivot.High <= (decimal)bbPivot.UpperBand.Value
                                    || pivot.Low <= (decimal)bbPivot.Sma.Value
                                    //|| (pivot.Low >= cur.Low && pivot.High <= cur.High)
                                    )
                                    continue;

                                var rateVol = Math.Round(pivot.Volume / cur.Volume, 1);
                                //if (rateVol > (decimal)0.6 || rateVol < (decimal)0.4) //Vol hiện tại phải nhỏ hơn hoặc bằng 0.6 lần vol của nến liền trước
                                if (rateVol > (decimal)0.6) //Vol hiện tại phải nhỏ hơn hoặc bằng 0.6 lần vol của nến liền trước
                                    continue;

                                //check div by zero
                                if (cur.High == cur.Low
                                    || pivot.High == pivot.Low
                                    || Math.Min(pivot.Open, pivot.Close) == pivot.Low)
                                    continue;

                                var rateCur = Math.Abs((cur.Open - cur.Close) / (cur.High - cur.Low));  //độ dài nến hiện tại
                                var ratePivot = Math.Abs((pivot.Open - pivot.Close) / (pivot.High - pivot.Low));  //độ dài nến pivot
                                var isHammer = (cur.High - cur.Close) >= (decimal)1.2 * (cur.Close - cur.Low);

                                if (isHammer)
                                {

                                }
                                else if (ratePivot < (decimal)0.2)
                                {
                                    var checkDoji = (pivot.High - Math.Max(pivot.Open, pivot.Close)) / (Math.Min(pivot.Open, pivot.Close) - pivot.Low);
                                    if (checkDoji >= (decimal)0.75 && checkDoji <= (decimal)1.25)
                                    {
                                        continue;
                                    }
                                }
                                else if (rateCur > (decimal)0.8)
                                {
                                    //check độ dài nến pivot
                                    var isValid = Math.Abs(pivot.Open - pivot.Close) >= Math.Abs(cur.Open - cur.Close);
                                    if (isValid)
                                        continue;
                                }


                                cur = pivot;

                                var next = lData15m.FirstOrDefault(x => x.Date == cur.Date.AddMinutes(15));
                                if (next is null)
                                    continue;
                                var rateEntry = Math.Round(100 * (-1 + cur.Close / next.High), 1);// tỉ lệ từ entry đến giá thấp nhất

                                var eEntry = cur;
                                var eClose = lData15m.FirstOrDefault(x => x.Date >= eEntry.Date.AddHours(hour));
                                if (eClose is null)
                                    continue;

                                var lClose = lData15m.Where(x => x.Date > eEntry.Date && x.Date <= eEntry.Date.AddHours(hour));
                                foreach (var itemClose in lClose)
                                {
                                    var ma = lbb.First(x => x.Date == itemClose.Date);
                                    if (itemClose.Close < (decimal)ma.LowerBand)
                                    {
                                        eClose = itemClose;
                                        break;
                                    }
                                }

                                dtFlag = eClose.Date;
                                var rate = Math.Round(100 * (-1 + eEntry.Close / eClose.Close), 1);
                                var lRange = lData15m.Where(x => x.Date >= eEntry.Date.AddMinutes(15) && x.Date <= eClose.Date);
                                var maxH = lRange.Max(x => x.High);
                                var minL = lRange.Min(x => x.Low);

                                var winloss = "W";
                                if (rate <= (decimal)0)
                                {
                                    winloss = "L";
                                }

                                decimal maxTP = 0, maxSL = 0;
                                if (side == (int)Binance.Net.Enums.OrderSide.Buy)
                                {
                                    maxTP = Math.Round(100 * (-1 + maxH / eEntry.Close), 1);
                                    maxSL = Math.Round(100 * (-1 + minL / eEntry.Close), 1);
                                }
                                else
                                {
                                    maxTP = Math.Round(100 * (-1 + eEntry.Close / minL), 1);
                                    maxSL = Math.Round(100 * (-1 + eEntry.Close / maxH), 1);
                                }
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
                                    Date = cur.Date,
                                    Rate = rate,
                                    MaxTP = maxTP,
                                    MaxSL = maxSL,
                                    RateEntry = rateEntry,
                                });
                                var mes = $"{item}|{winloss}|{((Binance.Net.Enums.OrderSide)side).ToString()}|{cur.Date.ToString("dd/MM/yyyy HH:mm")}|{rate}%|TPMax: {maxTP}%|SLMax: {maxSL}%|RateEntry: {rateEntry}%|RSI: {rsiPivot.Rsi}";
                                lMes.Add(mes);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
                            }

                        }

                        //Console.WriteLine(count);
                        //return;

                        //foreach (var mes in lMes)
                        //{
                        //    Console.WriteLine(mes);
                        //}
                        //
                        if (winCount <= lossCount)
                            continue;
                        var rateRes = Math.Round(((decimal)winCount / (winCount + lossCount)), 2);
                        if (rateRes > (decimal)0.5 && winCount > 3)
                        {
                            var sumRate = lModel.Where(x => x.s == item).Sum(x => x.Rate);
                            if (sumRate <= 1)
                            {
                                var lRemove = lModel.Where(x => x.s == item);
                                lModel = lModel.Except(lRemove).ToList();
                                continue;
                            }

                            //Console.WriteLine($"{item}: {rateRes}({winCount}/{lossCount})");
                            lMesAll.AddRange(lMes);
                            //foreach (var mes in lMes)
                            //{
                            //    Console.WriteLine(mes);
                            //}
                            var realWin = 0;
                            foreach (var model in lModel.Where(x => x.s == item))
                            {
                                if (model.Rate > (decimal)0)
                                    realWin++;
                            }
                            var count = lModel.Count(x => x.s == item);

                            if (sumRate / count <= (decimal)0.5)
                            {
                                var lRemove = lModel.Where(x => x.s == item);
                                lModel = lModel.Except(lRemove).ToList();
                                continue;
                            }

                            var rate = Math.Round((double)realWin / count, 1);
                            var perRate = Math.Round((float)sumRate / count, 1);
                            if (perRate < 0.7)
                            {
                                var lRemove = lModel.Where(x => x.s == item);
                                lModel = lModel.Except(lRemove).ToList();
                                continue;
                            }
                            Console.WriteLine($"{item}| W/Total: {realWin}/{count} = {rate}%|Rate: {sumRate}%|Per: {perRate}%");

                            winTotal += winCount;
                            lossTotal += lossCount;
                            winCount = 0;
                            lossCount = 0;
                        }


                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                }

                //foreach (var mes in lMesAll)
                //{
                //    Console.WriteLine(mes);
                //}
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



//Tier 1
"VIDTUSDT",
"EPICUSDT",
"MASAUSDT",
"NEIROETHUSDT",
"MAGICUSDT",
"LUCEUSDT",
////Tire 2
"MASKUSDT",
"SEIUSDT",
"SNXUSDT",
"TOKENUSDT",
"ANKRUSDT",
"BATUSDT",
"DOGSUSDT",
"FILUSDT",
"GLMUSDT",
"NCUSDT",
"QTUMUSDT",


DGBUSDT | W / Total: 10 / 13 = 0.8 %| Rate: 24.3 %| Per: 1.9 %
AERGOUSDT | W / Total: 12 / 20 = 0.6 %| Rate: 25.9 %| Per: 1.3 %
GMTUSDT | W / Total: 10 / 14 = 0.7 %| Rate: 18.8 %| Per: 1.3 %
GPSUSDT | W / Total: 7 / 11 = 0.6 %| Rate: 13.9 %| Per: 1.3 %
MEMEUSDT | W / Total: 13 / 21 = 0.6 %| Rate: 27.0 %| Per: 1.3 %
XCHUSDT | W / Total: 11 / 19 = 0.6 %| Rate: 24.1 %| Per: 1.3 %
VIRTUALUSDT | W / Total: 16 / 23 = 0.7 %| Rate: 27.6 %| Per: 1.2 %
PENDLEUSDT | W / Total: 14 / 24 = 0.6 %| Rate: 29.1 %| Per: 1.2 %
RAYDIUMUSDT | W / Total: 14 / 22 = 0.6 %| Rate: 27.4 %| Per: 1.2 %
BEAMUSDT | W / Total: 14 / 21 = 0.7 %| Rate: 23.9 %| Per: 1.1 %
PHBUSDT | W / Total: 20 / 31 = 0.6 %| Rate: 35.4 %| Per: 1.1 %
ZBCNUSDT | W / Total: 9 / 14 = 0.6 %| Rate: 13.9 %| Per: 1 %
GOATUSDT | W / Total: 10 / 18 = 0.6 %| Rate: 18.8 %| Per: 1 %//
MANEKIUSDT | W / Total: 12 / 18 = 0.7 %| Rate: 18.0 %| Per: 1 %//
PYTHUSDT | W / Total: 13 / 25 = 0.5 %| Rate: 23.8 %| Per: 1 %
SLFUSDT | W / Total: 10 / 18 = 0.6 %| Rate: 17.4 %| Per: 1 %
BIGTIMEUSDT | W / Total: 20 / 34 = 0.6 %| Rate: 30.5 %| Per: 0.9 %
BNBUSDT | W / Total: 9 / 12 = 0.8 %| Rate: 10.6 %| Per: 0.9 %
CFXUSDT | W / Total: 11 / 21 = 0.5 %| Rate: 18.3 %| Per: 0.9 %
FIDAUSDT | W / Total: 12 / 21 = 0.6 %| Rate: 18.6 %| Per: 0.9 %
LQTYUSDT | W / Total: 18 / 27 = 0.7 %| Rate: 24.2 %| Per: 0.9 %
NEOUSDT | W / Total: 11 / 16 = 0.7 %| Rate: 14.6 %| Per: 0.9 %
SANDUSDT | W / Total: 11 / 18 = 0.6 %| Rate: 17.0 %| Per: 0.9 %
RAREUSDT | W / Total: 16 / 24 = 0.7 %| Rate: 22.0 %| Per: 0.9 %
ZILUSDT | W / Total: 15 / 24 = 0.6 %| Rate: 21.8 %| Per: 0.9 %
WUSDT | W / Total: 16 / 25 = 0.6 %| Rate: 22.6 %| Per: 0.9 %
ADAUSDT | W / Total: 18 / 27 = 0.7 %| Rate: 22.0 %| Per: 0.8 %
ALICEUSDT | W / Total: 15 / 26 = 0.6 %| Rate: 20.9 %| Per: 0.8 %
ALTUSDT | W / Total: 17 / 29 = 0.6 %| Rate: 23.6 %| Per: 0.8 %
ARKMUSDT | W / Total: 14 / 23 = 0.6 %| Rate: 17.3 %| Per: 0.8 %
BTCUSDT | W / Total: 9 / 14 = 0.6 %| Rate: 11.4 %| Per: 0.8 %
COMPUSDT | W / Total: 13 / 20 = 0.6 %| Rate: 16.3 %| Per: 0.8 %//
DATAUSDT | W / Total: 8 / 14 = 0.6 %| Rate: 10.5 %| Per: 0.8 %
ENJUSDT | W / Total: 22 / 34 = 0.6 %| Rate: 26.2 %| Per: 0.8 %
ETHFIUSDT | W / Total: 13 / 21 = 0.6 %| Rate: 16.8 %| Per: 0.8 %
FLUXUSDT | W / Total: 17 / 29 = 0.6 %| Rate: 23.6 %| Per: 0.8 %
GLMRUSDT | W / Total: 16 / 27 = 0.6 %| Rate: 22.0 %| Per: 0.8 %
PLUMEUSDT | W / Total: 7 / 13 = 0.5 %| Rate: 10.4 %| Per: 0.8 %
PROMUSDT | W / Total: 13 / 18 = 0.7 %| Rate: 15.2 %| Per: 0.8 %
RLCUSDT | W / Total: 11 / 19 = 0.6 %| Rate: 14.5 %| Per: 0.8 %
SOLOUSDT | W / Total: 9 / 17 = 0.5 %| Rate: 13.6 %| Per: 0.8 %
VRUSDT | W / Total: 11 / 17 = 0.6 %| Rate: 13.5 %| Per: 0.8 %
BRUSDT | W / Total: 4 / 7 = 0.6 %| Rate: 4.6 %| Per: 0.7 %
DENTUSDT | W / Total: 16 / 29 = 0.6 %| Rate: 19.9 %| Per: 0.7 %//
DOGEUSDT | W / Total: 17 / 25 = 0.7 %| Rate: 16.7 %| Per: 0.7 %
FLOCKUSDT | W / Total: 11 / 18 = 0.6 %| Rate: 12.8 %| Per: 0.7 %
INJUSDT | W / Total: 7 / 12 = 0.6 %| Rate: 8.8 %| Per: 0.7 %
IOSTUSDT | W / Total: 12 / 23 = 0.5 %| Rate: 16.5 %| Per: 0.7 %//
KAVAUSDT | W / Total: 7 / 12 = 0.6 %| Rate: 8.6 %| Per: 0.7 %
MEMEFIUSDT | W / Total: 10 / 17 = 0.6 %| Rate: 12.5 %| Per: 0.7 %
NSUSDT | W / Total: 12 / 19 = 0.6 %| Rate: 13.3 %| Per: 0.7 %//
POPCATUSDT | W / Total: 12 / 22 = 0.5 %| Rate: 15.4 %| Per: 0.7 %//
PORTALUSDT | W / Total: 14 / 23 = 0.6 %| Rate: 16.2 %| Per: 0.7 %
STXUSDT | W / Total: 13 / 23 = 0.6 %| Rate: 15.6 %| Per: 0.7 %//
TLMUSDT | W / Total: 11 / 21 = 0.5 %| Rate: 15.1 %| Per: 0.7 %



