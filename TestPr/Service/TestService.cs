using Bybit.Net.Enums;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using Skender.Stock.Indicators;    
using TestPr.DAL;
using TestPr.DAL.Entity;
using TestPr.Utils;

namespace TestPr.Service
{
    public interface ITestService
    {
        Task ShortRSI();
        Task LongRSI();
        Task ShortTokenUnlock();
        Task LongMA20();
        Task ShortMA20();
        Task CheckAllBYBIT();
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
        private readonly ITokenUnlockRepo _tokenRepo;
        public TestService(ILogger<TestService> logger, IAPIService apiService, ITokenUnlockRepo tokenrepo, ISymbolRepo symRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _tokenRepo = tokenrepo;
            _symRepo = symRepo;
        }

        //rsi - Good for SHORT(rate very high) Tong: 406.7%|W/L: 383/168 short rsi
        public async Task ShortRSI()
        {
            try
            {
                //2x1.7 best
                decimal SL_RATE = 1.7m;//1.5,1.6,1.8,1.9,2
                int hour = 2;//1h,2h,3h,4h

                var lMesAll = new List<string>();
                var lRate = new List<decimal>();
                var winCount = 0;
                var lossCount = 0;
                foreach (var item in StaticVal._lRsiShort)
                {
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
                    var lrsi = lData15m.GetRsi();
                    Quote close = null;
                    foreach (var rsi in lrsi)
                    {
                        try
                        {
                            if (close != null && close.Date >= rsi.Date)
                                continue;
                            if (rsi.Rsi is null || rsi.Rsi >= 80 || rsi.Rsi < 75)
                                continue;
                            var side = (int)Binance.Net.Enums.OrderSide.Sell;
                            var cur = lData15m.First(x => x.Date == rsi.Date);
                            var next = lData15m.FirstOrDefault(x => x.Date == rsi.Date.AddMinutes(15));
                            if (next is null)
                                continue;
                            if (next.High < cur.High)
                                continue;

                            //var prev = lData15m.FirstOrDefault(x => x.Date == rsi.Date.AddMinutes(-15));
                            //if (prev is null)
                            //    continue;
                            //if (prev.Volume * 2m <= cur.Volume)
                            //    continue;

                            next.Close = cur.High;

                            var eEntry = next;
                            var eClose = lData15m.First(x => x.Date >= eEntry.Date.AddHours(hour));
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
                            var mes = $"{item}|{winloss}|{((Binance.Net.Enums.OrderSide)side).ToString()}|{rsi.Date.ToString("dd/MM/yyyy HH:mm")}|{rate}%|TPMax: {maxTP}%|SLMax: {maxSL}%";
                            lMes.Add(mes);
                        }
                        catch(Exception ex)
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
            }
        }

        //rsi - Good for LONG(rate very high) Tong: 281.8%|W/L: 162/121 long rsi
        public async Task LongRSI()
        {
            try
            {
                //2x1.7 best
                decimal SL_RATE = 1.7m;//1.5,1.6,1.8,1.9,2
                int hour = 2;//1h,2h,3h,4h

                var lMesAll = new List<string>();
                var lRate = new List<decimal>();
                var winCount = 0;
                var lossCount = 0;
                foreach (var item in StaticVal._lRsiLong)
                {
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
                    var lrsi = lData15m.GetRsi();
                    var lbb = lData15m.GetBollingerBands();
                    Quote close = null;
                    foreach (var rsi in lrsi)
                    {
                        try
                        {
                            if (close != null && close.Date >= rsi.Date)
                                continue;
                            if (rsi.Rsi is null || rsi.Rsi >= 30 || rsi.Rsi <= 25 )
                                continue;
                            var side = (int)Binance.Net.Enums.OrderSide.Buy;
                            var cur = lData15m.First(x => x.Date == rsi.Date);
                            var bb = lbb.FirstOrDefault(x => x.Date == rsi.Date);
                            if (cur.Low > (decimal)bb.LowerBand.Value)
                                continue;

                            Quote first = null;

                            var curRate = Math.Round(Math.Abs(cur.Open - cur.Close) * 100 / Math.Abs(cur.High - cur.Low));
                            if (curRate >= 40)
                            {
                                continue;
                            }
                            else
                            {
                                if (Math.Abs(cur.Open - cur.Close) > (Math.Min(cur.Open, cur.Close) - cur.Low))
                                    continue;

                                if ((cur.High - Math.Max(cur.Open, cur.Close)) > (Math.Min(cur.Open, cur.Close) - cur.Low))
                                    continue;

                                var low = cur.Low + 0.1m * (cur.High - cur.Low);
                                first = lData15m.FirstOrDefault(x => x.Date == cur.Date.AddMinutes(15) && x.Low <= low);
                                if (first != null)
                                    first.Close = low;
                            }

                            if (first is null)
                            {
                                continue;
                            }

                            var eEntry = first;
                            var eClose = lData15m.FirstOrDefault(x => x.Date >= eEntry.Date.AddHours(hour));
                            if (eClose is null)
                                continue;
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
                            var mes = $"{item}|{winloss}|{((Binance.Net.Enums.OrderSide)side).ToString()}|{first.Date.ToString("dd/MM/yyyy HH:mm")}|{rate}%|TPMax: {maxTP}%|SLMax: {maxSL}%";
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
            }
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

        public async Task CheckAllBYBIT()
        {
            try
            {
                //var lAll = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, limit: 1000);
                //var lUsdt = lAll.Data.List.Where(x => x.QuoteAsset == "USDT" && !x.Name.StartsWith("1000")).Select(x => x.Name);
                //var countUSDT = lUsdt.Count();
                //var lTake = lUsdt.Skip(450).Take(50).ToList();
                //2x1.7 best
                decimal SL_RATE = 1.7m;//1.5,1.6,1.8,1.9,2
                //decimal SL_RATE = 100m;//1.5,1.6,1.8,1.9,2
                int hour = 2;//1h,2h,3h,4h

                var lMesAll = new List<string>();
                var lModel = new List<LongMa20>();
                var lRate = new List<decimal>();
                var winCount = 0;
                var lossCount = 0;

                var lTake = new List<string>();
                lTake.Clear();
                var lTmp = new List<string>
                {
                    //"MORPHOUSDT",
                    //"KAVAUSDT",
                    //"LDOUSDT",
                    //"ARBUSDT",
                    //"BOMEUSDT",
                    //"ARUSDT",
                    //"KNCUSDT",
                    //"DGBUSDT",
                    //"BIGTIMEUSDT",
                    //"AAVEUSDT",
                    //"DATAUSDT",
                    //"ETHFIUSDT",
                    //"AIUSDT",
                    //"BATUSDT",
                    //"PEAQUSDT",
                    //"PEOPLEUSDT",
                    //"POLUSDT",
                    "PENDLEUSDT",
                    //"ZILUSDT",
                    //"LINKUSDT",
                    //"LUNA2USDT",
                    //"NEIROETHUSDT",
                    //"MINAUSDT",
                    //"NILUSDT",
                    //"RPLUSDT",
                    //"SHELLUSDT",


                    //"ALUUSDT",
                    //"BLUEUSDT",
                    "CRVUSDT",
                    //"CTSIUSDT",
                    //"GUSDT",
                    //"NULSUSDT",
                    //"PERPUSDT",
                    //"PIXELUSDT",
                    //"PLUMEUSDT",
                    //"ROAMUSDT",
                    //"RSRUSDT",
                    //"SEIUSDT",
                    //"XIONUSDT",
                };
                lTake.AddRange(lTmp);
                foreach (var item in lTake)
                {
                    try
                    {
                        //if (item.Key != "1INCHUSDT")
                        //    continue;
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
                                    if (itemClose.Close > (decimal)ma.UpperBand)
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
                                if (rate <= (decimal)0.5)
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
                        //
                        var rateRes = Math.Round(((decimal)winCount / (winCount + lossCount)), 2);
                        if(rateRes > (decimal)0.5)
                        {
                            Console.WriteLine($"{item}: {rateRes}({winCount}/{lossCount})");
                        }
                        
                        winCount = 0;
                        lossCount = 0;
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(item);
                    }
                }

                //foreach (var mes in lMesAll)
                //{
                //    Console.WriteLine(mes);
                //}
                //Console.WriteLine($"Tong: {lRate.Sum()}%|W/L: {winCount}/{lossCount}");

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
        //LONG RSI Tong(37): 660.1%|W/L: 429/221
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
                    "GRTUSDT",
                    "B3USDT",
                    "AERGOUSDT",
                    "1000BONKUSDT",
                    "ZRXUSDT",
                    "EPICUSDT",
                    "DODOXUSDT",
                    "INJUSDT",
                    "ALTUSDT",
                    "MTLUSDT",
                    "ETHFIUSDT",
                    "DFUSDT",
                    "PLUMEUSDT",
                    "QTUMUSDT",
                    "TOKENUSDT",
                    "CETUSUSDT",
                    "ONDOUSDT",
                    "OMUSDT",
                    "BTCUSDT",
                    "ATAUSDT",
                    "SEIUSDT",
                    "DEGENUSDT",
                    "THEUSDT",
                    "PHBUSDT",
                    "1000FLOKIUSDT",
                    "KSMUSDT",
                    "PROMUSDT",
                    "JOEUSDT",
                    "SWELLUSDT",
                    "STORJUSDT",
                    "STGUSDT",
                    "RENDERUSDT",
                    "IOTAUSDT",
                    "ANKRUSDT",
                    "NFPUSDT",
                    "BOMEUSDT",
                    "BIOUSDT",
                    "LPTUSDT",
                    "ETHWUSDT",
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
                            var rate = Math.Round((double)realWin/ count, 1);
                            Console.WriteLine($"{item}| W/Total: {realWin}/{lModel.Count(x => x.s == item)} = {rate}%|Rate: {sumRate}%");

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
        //SHORT RSI Tong(40): 745.8%|W/L: 527/293
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
                    "EIGENUSDT",
                    "NILUSDT",
                    "HEIUSDT",
                    "NEIROUSDT",
                    "DUSKUSDT",
                    "KASUSDT",
                    "BROCCOLI714USDT",
                    "CATIUSDT",
                    "GPSUSDT",
                    "BIOUSDT",
                    "HIPPOUSDT",
                    "DYDXUSDT",
                    "VICUSDT",
                    "PONKEUSDT",
                    "MANTAUSDT",
                    "OMUSDT",
                    "MOODENGUSDT",
                    "BERAUSDT",
                    "AGLDUSDT",
                    "LUMIAUSDT",
                    "AXSUSDT",
                    "RSRUSDT",
                    "ZENUSDT",
                    "ALPHAUSDT",
                    "LTCUSDT",
                    "LINKUSDT",
                    "QTUMUSDT",
                    "RLCUSDT",
                    "CRVUSDT",
                    "GALAUSDT",
                    "ARPAUSDT",
                    "CTSIUSDT",
                    "EDUUSDT",
                    "AIUSDT",
                    "BEAMXUSDT",
                    "ETHWUSDT",
                    "XVGUSDT",
                    "TWTUSDT",
                    "CAKEUSDT",
                    "1MBABYDOGEUSDT",
                    "BADGERUSDT",
                    "CHESSUSDT",
                    "PROMUSDT",
                    "LOKAUSDT",
                    "PENGUUSDT",
                    "INJUSDT",
                    "1000PEPEUSDT",
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
                                var rateEntry = Math.Round(100 * (-1 + next.Low / cur.Close), 1);// tỉ lệ từ entry đến giá thấp nhất

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
                            var rate = Math.Round((double)realWin / count, 1);
                            Console.WriteLine($"{item}| W/Total: {realWin}/{lModel.Count(x => x.s == item)} = {rate}%|Rate: {sumRate}%");

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

                #region comment
                lTake.Clear();
                var lTmp = new List<string>
                {
                    //Tier 1
                    "VIDTUSDT",
                    "EPICUSDT",
                    "DGBUSDT",
                    "MASAUSDT",
                    "BRUSDT",//
                    "BEAMUSDT",//
                    "MEMEUSDT",//
                    "PENDLEUSDT",
                    "NEIROETHUSDT",
                    "GMTUSDT",//
                    "GPSUSDT",
                    "CFXUSDT",
                    "MAGICUSDT",
                    "SLFUSDT",
                    "SANDUSDT",
                    "FIDAUSDT",
                    "XCHUSDT",
                    "VIRTUALUSDT",
                    "BIGTIMEUSDT",
                    "BNBUSDT",
                    "ETHFIUSDT",
                    "LUCEUSDT",
                    "RAREUSDT",
                    "AERGOUSDT",
                    "ALICEUSDT",
                    "PHBUSDT",
                    ////Tire 2
                    "MASKUSDT",
                    "NEOUSDT",
                    "VRUSDT",
                    "ENJUSDT",
                    "SEIUSDT",
                    "SNXUSDT",
                    "PROMUSDT",
                    "SOLOUSDT",
                    "TOKENUSDT",
                    "WUSDT",
                    "ZBCNUSDT",
                    "ZILUSDT",
                    "ADAUSDT",
                    "ALTUSDT",
                    "ANKRUSDT",
                    "ARKMUSDT",
                    "BATUSDT",
                    "BTCUSDT",
                    "DOGSUSDT",
                    "FILUSDT",
                    "FLOCKUSDT",
                    "FLUXUSDT",
                    "GLMRUSDT",
                    "GLMUSDT",
                    "INJUSDT",
                    "KAVAUSDT",
                    "LQTYUSDT",
                    "MEMEFIUSDT",
                    "NCUSDT",
                    "PLUMEUSDT",
                    "PORTALUSDT",
                    "QTUMUSDT",
                    "RAYDIUMUSDT",
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
                            var rate = Math.Round((double)realWin / count, 1);
                            Console.WriteLine($"{item}| W/Total: {realWin}/{lModel.Count(x => x.s == item)} = {rate}%|Rate: {sumRate}%");

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

        //SHORT RSI Tong(59): 1020.1%|W/L: 702/410
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
                    //Tier 1
                    "FWOGUSDT",
                    "JUSDT",
                    "XVSUSDT",
                    "CATIUSDT",
                    "HEIUSDT",
                    "KOMAUSDT",
                    "CAKEUSDT",
                    "XEMUSDT",
                    "NILUSDT",
                    "OLUSDT",
                    "DYDXUSDT",
                    "FLUXUSDT",
                    "IDEXUSDT",
                    "KASUSDT",
                    "XVGUSDT",
                    "MAVIAUSDT",
                    "MOODENGUSDT",
                    "NCUSDT",
                    "MAXUSDT",
                    "MBLUSDT",
                    "GUSDT",
                    "OMNIUSDT",
                    "SPELLUSDT",
                    "TROYUSDT",
                    "STGUSDT",
                    "VVVUSDT",
                    "ZBCNUSDT",
                    ////Tier 2
                    //"AGLDUSDT",
                    //"AIOZUSDT",
                    //"AIUSDT",
                    //"ALEOUSDT",
                    //"ALUUSDT",
                    //"AVAILUSDT",
                    //"AXLUSDT",
                    //"BADGERUSDT",
                    //"BELUSDT",
                    //"BLASTUSDT",
                    //"BLURUSDT",
                    //"COOKUSDT",
                    //"CPOOLUSDT",
                    //"EGLDUSDT",
                    //"EIGENUSDT",
                    //"EOSUSDT",
                    //"FLRUSDT",
                    //"GPSUSDT",
                    //"LSKUSDT",
                    //"MBOXUSDT",
                    //"MDTUSDT",
                    //"MORPHOUSDT",
                    //"MOVRUSDT",
                    //"MVLUSDT",
                    //"NTRNUSDT",
                    //"OMUSDT",
                    //"PENGUUSDT",
                    //"ROSEUSDT",
                    //"SERAPHUSDT",
                    //"TUSDT",
                    //"XTZUSDT",
                    //"ZENUSDT",
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
                                var rateEntry = Math.Round(100 * (-1 + next.Low / cur.Close), 1);// tỉ lệ từ entry đến giá thấp nhất

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
                            Console.WriteLine($"{item}| W/Total: {realWin}/{count} = {rate}%|Rate: {sumRate}%");

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

        // Tong: 331.7%|W/L: 45/79 Token Unlock
        public async Task ShortTokenUnlock()
        {
            try
            {
                var lSym = _lTokenUnlock.OrderBy(x => x.release_time);
                decimal asset = 100;
                var step = 15;
                var margin = 10;
                var checkMax = 1;
                long checkTime = 0;
                var lmes = new List<string>();
                foreach (var item in lSym)
                {
                    if (_lBlacKList.Contains(item.s))
                        continue;

                    if (item.release_time == checkTime)
                    {
                        checkMax++;
                    }
                    else
                    {
                        checkMax = 1;
                        checkTime = item.release_time;
                    }
                    if (checkMax > 5)
                    {
                        continue;
                    }

                    var lData = await _apiService.GetData($"{item.s}USDT", EInterval.D1);
                    Thread.Sleep(1000);

                    if (!lData.Any())
                        continue;

                    var timeEnd = item.release_time.longToDateTime();
                    var end = lData.LastOrDefault(x => x.Date <= timeEnd);
                    if (end is null)
                        continue;

                    var checkSL = Math.Abs(Math.Round(100 * (-1 + end.Open / end.High), 1));
                    if (checkSL >= (decimal)1.6)
                    {
                        var valSL = Math.Round(step * margin * 0.016, 1);
                        asset -= (decimal)valSL;
                        totalLoss++;
                        var mesSL = $"{timeEnd}|{item.s}|SL|-1.6%|{valSL}";
                        lmes.Add(mesSL);
                        //Console.WriteLine(mesSL);
                        continue;
                    }

                    var rate = Math.Round(100 * (-1 + end.Open / end.Close), 1);
                    var valTP = Math.Round(step * margin * rate / 100, 1);
                    asset += valTP;
                    totalWin++;
                    var mesTP = $"{timeEnd}|{item.s}|TP|{rate}%|{valTP}";
                    lmes.Add(mesTP);
                    //Console.WriteLine(mesTP);
                    //Open High <1.6 -> short và chốt cuối ngày(margin x10) - start 15usd - ngày 2 lệnh
                }
                foreach (var item in lmes)
                {
                    Console.WriteLine(item);
                }
                Console.WriteLine($"Tong: {asset}%|W/L: {totalWin}/{totalLoss}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTestTokenUnlock|EXCEPTION| {ex.Message}");
            }
        }

        decimal total = 0;
        int totalWin = 0;
        int totalLoss = 0;
        //public async Task MethodTestTokenUnlock()
        //{
        //    try
        //    {
        //        var lSym = _lTokenUnlock.Select(x => x.s).Distinct();
        //        foreach (var item in lSym)
        //        {
        //            if (_lBlacKList.Contains(item))
        //                continue;

        //            var lData = await _apiService.GetData($"{item}USDT", EInterval.D1);
        //            Thread.Sleep(1000);

        //            if (!lData.Any())
        //                continue;

        //            var lVal = _lTokenUnlock.Where(x => x.s == item);
        //            foreach (var itemVal in lVal)
        //            {
        //                var timeEnd = itemVal.release_time.longToDateTime();
        //                var end = lData.LastOrDefault(x => x.Date <= timeEnd);
        //                if (end is null)
        //                    continue;

        //                var checkSL = Math.Abs(Math.Round(100 * (-1 + end.Open / end.High), 1));
        //                if (checkSL >= (decimal)1.6)
        //                {
        //                    var valSL = Math.Round(15 * 10 * 0.016, 1);
        //                    total -= (decimal)valSL;
        //                    totalLoss++;
        //                    var mesSL = $"{item}|SL|-1.6%|{valSL}";
        //                    Console.WriteLine(mesSL);
        //                    continue;
        //                }

        //                var rate = Math.Round(100 * (-1 + end.Open / end.Close), 1);
        //                var valTP = Math.Round(15 * 10 * rate / 100, 1);
        //                total += valTP;
        //                totalWin++;
        //                var mesTP = $"{item}|TP|{rate}%|{valTP}";
        //                Console.WriteLine(mesTP);
        //                //Open High <1.6 -> short và chốt cuối ngày(margin x10) - start 15usd - ngày 2 lệnh
        //            }
        //        }
        //        Console.WriteLine($"Tong: {total}");
        //        Console.WriteLine($"Tong Lenh Win: {totalWin}/{totalLoss}");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"TestService.MethodTestTokenUnlock|EXCEPTION| {ex.Message}");
        //    }
        //}

        private List<string> _lBlacKList = new List<string>
        {
            "TORN",
            "LAZIO",
            "RAD",
            "OGN",
            "DAR",
            "CGPT",
            "VOXEL",
            "SEI",
            "PORTO",
            "JUV",
            "CFX",
            "SANTOS",
            "ATM",
            "ASR",
        };

        private List<TokenUnlock> _lTokenUnlock = new List<TokenUnlock>
        {
            new TokenUnlock{ s = "ZTX", noti_time = 20241012 , release_time = 20241016, cap = 22.23, value = 1 },
            new TokenUnlock{ s = "ZBCN", noti_time = 20241012 , release_time = 20241016, cap = 66.86, value = 1.93 },
            new TokenUnlock{ s = "RARE", noti_time = 20241012 , release_time = 20241017, cap = 82.06, value = 0.82 },
            new TokenUnlock{ s = "APE", noti_time = 20241012 , release_time = 20241017, cap = 436.42, value = 14.76 },
            new TokenUnlock{ s = "NEON", noti_time = 20241012 , release_time = 20241017, cap = 18.19, value = 12.59 },
            new TokenUnlock{ s = "ACE", noti_time = 20241012 , release_time = 20241018, cap = 83.57, value = 1.35 },
            new TokenUnlock{ s = "TORN", noti_time = 20241012 , release_time = 20241018, cap = 11.17 , value =  2},
            new TokenUnlock{ s = "SABAI", noti_time = 20241012 , release_time = 20241018, cap = 14.1 , value =  0.456},
            new TokenUnlock{ s = "ZKJ", noti_time = 20241012 , release_time = 20241019, cap = 75.31, value = 31.93 },
            new TokenUnlock{ s = "KARRAT", noti_time = 20241012 , release_time = 20241019, cap = 61.35 , value = 4.187 },
            new TokenUnlock{ s = "NUM", noti_time = 20241012 , release_time = 20241019, cap = 30.21, value = 0.34 },
            new TokenUnlock{ s = "UNFI", noti_time = 20241012 , release_time = 20241019, cap = 23.36, value =  6.6},
            new TokenUnlock{ s = "PIXEL", noti_time = 20241012 , release_time = 20241019, cap = 149.06, value =  3.69},
            new TokenUnlock{ s = "MERL", noti_time = 20241012 , release_time = 20241019, cap = 129.76, value =  3.876},
            new TokenUnlock{ s = "DEAI", noti_time = 20241013 , release_time = 20241020, cap = 51.99, value =  4.2},
            new TokenUnlock{ s = "HTM", noti_time = 20241013 , release_time = 20241020, cap = 17.27, value =  2.42},
            new TokenUnlock{ s = "MPLX", noti_time = 20241013 , release_time = 20241020, cap = 154.95, value =  3.24},
            new TokenUnlock{ s = "COMBO", noti_time = 20241014 , release_time = 20241021, cap = 30.05, value =  0.328},
            new TokenUnlock{ s = "LAZIO", noti_time = 20241015 , release_time = 20241021, cap = 13.95, value =  4.86},
            new TokenUnlock{ s = "VARA", noti_time = 20241015 , release_time = 20241021, cap = 11.85, value = 4.53 },
            new TokenUnlock{ s = "TAO", noti_time = 20241015 , release_time = 20241021, cap = 4650, value = 136 },
            new TokenUnlock{ s = "ML", noti_time = 20241015 , release_time = 20241021, cap = 10.31, value = 1.08 },
            new TokenUnlock{ s = "VRTX", noti_time = 20241015 , release_time = 20241021, cap = 23.05, value = 1.83 },
            new TokenUnlock{ s = "SCR", noti_time = 20241015 , release_time = 20241022, cap = 219.92, value = 220.46 },
            new TokenUnlock{ s = "C98", noti_time = 20241016 , release_time = 20241023, cap = 106.62, value = 2.07 },
            new TokenUnlock{ s = "FIDA", noti_time = 20241016 , release_time = 20241023, cap = 51.6, value = 6.42 },
            new TokenUnlock{ s = "GTAI", noti_time = 20241018 , release_time = 20241025, cap = 27.39, value = 2.63 },
            new TokenUnlock{ s = "ALT", noti_time = 20241018 , release_time = 20241025, cap = 255.55, value = 21.87 },
            new TokenUnlock{ s = "RAD", noti_time = 20241018 , release_time = 20241025, cap = 56.76, value = 1.7 },
            new TokenUnlock{ s = "AURY", noti_time = 20241019 , release_time = 20241026, cap = 21.63, value = 0.466 },
            new TokenUnlock{ s = "BREED", noti_time = 20241019 , release_time = 20241026, cap = 11.14, value = 0.61 },
            new TokenUnlock{ s = "NFP", noti_time = 20241020 , release_time = 20241027, cap = 66.97, value = 2.38 },
            new TokenUnlock{ s = "EDU", noti_time = 20241021 , release_time = 20241028, cap = 192.55, value = 11.13 },
            new TokenUnlock{ s = "MAV", noti_time = 20241021 , release_time = 20241028, cap = 83.5, value = 10.1 },
            new TokenUnlock{ s = "VIC", noti_time = 20241022 , release_time = 20241029, cap = 35.18, value =  7.27},
            new TokenUnlock{ s = "ACA", noti_time = 20241022 , release_time = 20241029, cap = 67.08, value =  0.7},
            new TokenUnlock{ s = "PORTAL", noti_time = 20241022 , release_time = 20241029, cap = 103.25, value = 8 },
            new TokenUnlock{ s = "WLTH", noti_time = 20241023 , release_time = 20241030, cap = 11.06, value =  1.44},
            new TokenUnlock{ s = "TIA", noti_time = 20241024 , release_time = 20241031, cap = 1300, value = 1040 },
            new TokenUnlock{ s = "HOOK", noti_time = 20241025 , release_time = 20241101, cap = 84.28, value = 3.6 },
            new TokenUnlock{ s = "BICO", noti_time = 20241025 , release_time = 20241101, cap = 173.44, value = 4.39 },
            new TokenUnlock{ s = "IMX", noti_time = 20241025 , release_time = 20241101, cap = 2400, value = 47.67 },
            new TokenUnlock{ s = "AVA", noti_time = 20241025 , release_time = 20241101, cap = 26.38, value = 0.54 },
            new TokenUnlock{ s = "VANRY", noti_time = 20241025 , release_time = 20241101, cap = 136.19, value = 2.45 },
            new TokenUnlock{ s = "ETHDYDX", noti_time = 20241025 , release_time = 20241101, cap = 241.38 , value = 8.33 },
            new TokenUnlock{ s = "OGN", noti_time = 20241025 , release_time = 20241101, cap = 58.29 , value = 1.36 },
            new TokenUnlock{ s = "ORBR", noti_time = 20241026 , release_time = 20241102, cap = 238.83 , value = 8.8 },
            new TokenUnlock{ s = "TAI", noti_time = 20241026 , release_time = 20241102, cap = 64.96 , value = 3.67 },
            new TokenUnlock{ s = "NTRN", noti_time = 20241027 , release_time = 20241103, cap = 101.37 , value = 3.47 },
            new TokenUnlock{ s = "DAR", noti_time = 20241028 , release_time = 20241104, cap = 91.29 , value = 3.1 },
            new TokenUnlock{ s = "AI", noti_time = 20241028 , release_time = 20241104, cap = 87.1 , value = 4.05 },
            new TokenUnlock{ s = "G", noti_time = 20241029 , release_time = 20241105, cap = 234.59 , value = 9.29 },
            new TokenUnlock{ s = "WIFI", noti_time = 20241031 , release_time = 20241107, cap = 10.79, value = 0.4 },
            new TokenUnlock{ s = "BOSON", noti_time = 20241031 , release_time = 20241107, cap = 30.43 , value = 0.55 },
            new TokenUnlock{ s = "MYRIA", noti_time = 20241031 , release_time = 20241107, cap = 52.86 , value = 1.67 },
            new TokenUnlock{ s = "DEVVE", noti_time = 20241031 , release_time = 20241107, cap = 17.67 , value = 2.32 },
            new TokenUnlock{ s = "ACH", noti_time = 20241031 , release_time = 20241107, cap = 176.78 , value = 2.49 },
            new TokenUnlock{ s = "HFT", noti_time = 20241031 , release_time = 20241107, cap = 64.23, value = 2.18 },
            new TokenUnlock{ s = "ATA", noti_time = 20241031 , release_time = 20241107, cap = 46.5, value = 2.18 },
            new TokenUnlock{ s = "SD", noti_time = 20241101 , release_time = 20241108, cap = 13.18, value = 0.446 },
            new TokenUnlock{ s = "CLY", noti_time = 20241101 , release_time = 20241108, cap = 11.27, value = 0.16 },
            new TokenUnlock{ s = "ENS", noti_time = 20241101 , release_time = 20241108, cap = 558.31, value = 24.55 },
            new TokenUnlock{ s = "SCA", noti_time = 20241101 , release_time = 20241108, cap = 16.2, value =  1.48},
            new TokenUnlock{ s = "GMT", noti_time = 20241102 , release_time = 20241109, cap = 367.77, value = 11.64 },
            new TokenUnlock{ s = "CGPT", noti_time = 20241103 , release_time = 20241110, cap = 85.66, value = 2.33 },
            new TokenUnlock{ s = "CHEEL", noti_time = 20241103 , release_time = 20241110, cap = 621.19, value = 127.53 },
            new TokenUnlock{ s = "CETUS", noti_time = 20241103 , release_time = 20241110, cap = 51.59, value = 3.6 },
            new TokenUnlock{ s = "GODS", noti_time = 20241104 , release_time = 20241111, cap = 51.61, value = 1 },
            new TokenUnlock{ s = "GFI", noti_time = 20241104 , release_time = 20241111, cap = 37.47, value = 1.6 },
            new TokenUnlock{ s = "BOBA", noti_time = 20241105 , release_time = 20241112, cap = 28.83, value = 3.78 },
            new TokenUnlock{ s = "APT", noti_time = 20241105 , release_time = 20241112, cap = 4210, value = 91.79 },
            new TokenUnlock{ s = "OAS", noti_time = 20241105 , release_time = 20241112, cap = 87.44, value = 5.07 },
            new TokenUnlock{ s = "GFAL", noti_time = 20241106 , release_time = 20241113, cap = 33.96, value = 1.26 },
            new TokenUnlock{ s = "SWEAT", noti_time = 20241106 , release_time = 20241113, cap = 54.96, value = 2.93 },
            new TokenUnlock{ s = "VOXEL", noti_time = 20241107 , release_time = 20241114, cap = 31.59, value = 0.6 },
            new TokenUnlock{ s = "ALI", noti_time = 20241107 , release_time = 20241114, cap = 69.06, value = 1.12 },
            new TokenUnlock{ s = "RSS3", noti_time = 20241107 , release_time = 20241114, cap = 78.66, value = 2 },
            new TokenUnlock{ s = "HIGH", noti_time = 20241107 , release_time = 20241114, cap = 88.51, value = 1.47 },
            new TokenUnlock{ s = "CYBER", noti_time = 20241108 , release_time = 20241115, cap = 90.98, value = 9.72 },
            new TokenUnlock{ s = "ALICE", noti_time = 20241108 , release_time = 20241115, cap = 73.22, value = 2.21 },
            new TokenUnlock{ s = "SEI", noti_time = 20241108 , release_time = 20241115, cap = 1690, value = 51.97 },
            new TokenUnlock{ s = "ZTX", noti_time = 20241109 , release_time = 20241116, cap = 21.99, value = 1.26 },
            new TokenUnlock{ s = "PORTO", noti_time = 20241109 , release_time = 20241116, cap = 14.8, value = 7.38 },
            new TokenUnlock{ s = "ZBCN", noti_time = 20241109 , release_time = 20241116, cap = 63.19, value = 2.17 },
            new TokenUnlock{ s = "RARE", noti_time = 20241110 , release_time = 20241117, cap = 87.51, value = 1.63 },
            new TokenUnlock{ s = "APE", noti_time = 20241110 , release_time = 20241117, cap = 826.78, value = 16.89 },
            new TokenUnlock{ s = "NEON", noti_time = 20241110 , release_time = 20241117, cap = 25.47, value = 21.99 },
            new TokenUnlock{ s = "ACE", noti_time = 20241111 , release_time = 20241118, cap = 94.79, value = 3.79 },
            new TokenUnlock{ s = "AURORA", noti_time = 20241111 , release_time = 20241118, cap = 76.92, value = 2.55 },
            new TokenUnlock{ s = "ROSE", noti_time = 20241111 , release_time = 20241118, cap = 583.87, value = 14.45 },
            new TokenUnlock{ s = "TORN", noti_time = 20241111 , release_time = 20241118, cap = 13.17, value = 0.43 },
            new TokenUnlock{ s = "SABAI", noti_time = 20241111 , release_time = 20241118, cap = 10.27, value = 1.15 },
            new TokenUnlock{ s = "QI", noti_time = 20241112 , release_time = 20241119, cap = 78.14, value = 1.67 },
            new TokenUnlock{ s = "ZKJ", noti_time = 20241112 , release_time = 20241119, cap = 82.36, value = 18.61 },
            new TokenUnlock{ s = "KARRAT", noti_time = 20241112 , release_time = 20241119, cap = 73.4, value = 6.35 },
            new TokenUnlock{ s = "NUM", noti_time = 20241112 , release_time = 20241119, cap = 33.12, value = 0.5 },
            new TokenUnlock{ s = "UNFI", noti_time = 20241112 , release_time = 20241119, cap = 10.26, value = 0.1 },
            new TokenUnlock{ s = "PIXEL", noti_time = 20241112 , release_time = 20241119, cap = 237, value = 11.09 },
            new TokenUnlock{ s = "MERL", noti_time = 20241112 , release_time = 20241119, cap = 208.45, value = 11.21 },
            new TokenUnlock{ s = "DEAI", noti_time = 20241113 , release_time = 20241120, cap = 53.92, value = 15.56 },
            new TokenUnlock{ s = "HTM", noti_time = 20241113 , release_time = 20241120, cap = 17.95, value = 2 },
            new TokenUnlock{ s = "SHC", noti_time = 20241113 , release_time = 20241120, cap = 10, value = 0.22 },
            new TokenUnlock{ s = "BCUT", noti_time = 20241113 , release_time = 20241120, cap = 14.47, value = 1.58 },
            new TokenUnlock{ s = "VARA", noti_time = 20241114 , release_time = 20241121, cap = 12.09, value = 4.16 },
            new TokenUnlock{ s = "TAO", noti_time = 20241114 , release_time = 20241121, cap = 3970, value = 116.27 },
            new TokenUnlock{ s = "VRTX", noti_time = 20241114 , release_time = 20241121, cap = 21.62, value = 1.65 },
            new TokenUnlock{ s = "C98", noti_time = 20241116 , release_time = 20241123, cap = 117.66, value = 2.2 },
            new TokenUnlock{ s = "FIDA", noti_time = 20241116 , release_time = 20241123, cap = 108.86, value = 5.6 },
            new TokenUnlock{ s = "GTAI", noti_time = 20241118 , release_time = 20241125, cap = 24.92, value = 1.98 },
            new TokenUnlock{ s = "ALT", noti_time = 20241118 , release_time = 20241125, cap = 254.39, value = 21.68 },
            new TokenUnlock{ s = "RAD", noti_time = 20241118 , release_time = 20241125, cap = 64.79, value = 1.95 },
            new TokenUnlock{ s = "AURY", noti_time = 20241119 , release_time = 20241126, cap = 14.72, value = 0.31 },
            new TokenUnlock{ s = "BREED", noti_time = 20241119 , release_time = 20241126, cap = 12.68, value = 0.22 },
            new TokenUnlock{ s = "NFP", noti_time = 20241120 , release_time = 20241127, cap = 70, value = 2.49 },
            new TokenUnlock{ s = "AXL", noti_time = 20241120 , release_time = 20241127, cap = 623, value = 18.95 },
            new TokenUnlock{ s = "GOAL", noti_time = 20241120 , release_time = 20241127, cap = 12.57, value = 0.5 },
            new TokenUnlock{ s = "EDU", noti_time = 20241121 , release_time = 20241127, cap = 167.96, value =  9.44},
            new TokenUnlock{ s = "IMX", noti_time = 20241122 , release_time = 20241128, cap = 2290, value =  33.61},
            new TokenUnlock{ s = "PORTAL", noti_time = 20241122 , release_time = 20241129, cap = 112.72, value = 7.96 },
            new TokenUnlock{ s = "UDS", noti_time = 20241123 , release_time = 20241130, cap = 11.56, value =  0.8},
            new TokenUnlock{ s = "JUV", noti_time = 20241124 , release_time = 20241201, cap = 12.65, value =  0.35},
            new TokenUnlock{ s = "BAR", noti_time = 20241124 , release_time = 20241201, cap = 18.61, value = 0.88 },
            new TokenUnlock{ s = "CFX", noti_time = 20241124 , release_time = 20241201, cap = 839.41, value = 15.86 },
            new TokenUnlock{ s = "HOOK", noti_time = 20241124 , release_time = 20241201, cap = 94.66, value =  3.88},
            new TokenUnlock{ s = "BICO", noti_time = 20241124 , release_time = 20241201, cap = 276.86, value = 6.91 },
            new TokenUnlock{ s = "ACM", noti_time = 20241124 , release_time = 20241201, cap = 11.29, value = 0.35 },
            new TokenUnlock{ s = "SKL", noti_time = 20241124 , release_time = 20241201, cap = 328.47, value = 11.58 },
            new TokenUnlock{ s = "SANTOS", noti_time = 20241124 , release_time = 20241201, cap = 23.79, value = 24.45 },
            new TokenUnlock{ s = "ETHDYDX", noti_time = 20241124 , release_time = 20241201, cap = 335.78, value = 12.74 },
            new TokenUnlock{ s = "1INCH", noti_time = 20241124 , release_time = 20241201, cap = 486.58, value = 38.06 },
            new TokenUnlock{ s = "OGN", noti_time = 20241124 , release_time = 20241201, cap = 80.78, value = 1.87 },
            new TokenUnlock{ s = "ORNJ", noti_time = 20241125 , release_time = 20241129, cap = 10.37, value = 0.4 },
            new TokenUnlock{ s = "ORBR", noti_time = 20241125 , release_time = 20241202, cap = 379.04, value = 13.96 },
            new TokenUnlock{ s = "TAI", noti_time = 20241125 , release_time = 20241202, cap = 129.98, value = 7.35 },
            new TokenUnlock{ s = "NTRN", noti_time = 20241126 , release_time = 20241203, cap = 158.82, value = 5.34 },
            new TokenUnlock{ s = "DAR", noti_time = 20241127 , release_time = 20241204, cap = 107.61, value = 1.57 },
            new TokenUnlock{ s = "AI", noti_time = 20241127 , release_time = 20241204, cap = 131.44, value = 5.83 },
            new TokenUnlock{ s = "G", noti_time = 20241128 , release_time = 20241205, cap = 298.93, value = 4.46 },
            new TokenUnlock{ s = "THL", noti_time = 20241129 , release_time = 20241206, cap = 26.76, value = 1 },
            new TokenUnlock{ s = "NEON", noti_time = 20241130 , release_time = 20241206, cap = 22.87, value = 22.14 },
            new TokenUnlock{ s = "WIFI", noti_time = 20241130 , release_time = 20241207, cap = 17.59, value = 0.64 },
            new TokenUnlock{ s = "BOSON", noti_time = 20241130 , release_time = 20241207, cap = 48.49, value = 0.86 },
            new TokenUnlock{ s = "MYRIA", noti_time = 20241130 , release_time = 20241207, cap = 86.21, value = 2.5 },
            new TokenUnlock{ s = "DEVVE", noti_time = 20241130 , release_time = 20241207, cap = 33.14, value = 3.49 },
            new TokenUnlock{ s = "ACH", noti_time = 20241130 , release_time = 20241207, cap = 247.33, value = 3.44 },
            new TokenUnlock{ s = "JTO", noti_time = 20241130 , release_time = 20241207, cap = 484.64, value = 503.67 },
            new TokenUnlock{ s = "HFT", noti_time = 20241130 , release_time = 20241207, cap = 112.08, value = 3.73 },
            new TokenUnlock{ s = "SLF", noti_time = 20241201 , release_time = 20241208, cap = 49.79, value = 6.13 },
            new TokenUnlock{ s = "SD", noti_time = 20241201 , release_time = 20241208, cap = 42.04, value = 1.42 },
            new TokenUnlock{ s = "CLY", noti_time = 20241201 , release_time = 20241208, cap = 19.13, value = 0.26 },
            new TokenUnlock{ s = "ENS", noti_time = 20241201 , release_time = 20241208, cap = 1450, value = 61.76 },
            new TokenUnlock{ s = "SCA", noti_time = 20241201 , release_time = 20241208, cap = 27.59, value = 2.21 },
            new TokenUnlock{ s = "GMT", noti_time = 20241202 , release_time = 20241209, cap = 576.78, value = 20.83 },
            new TokenUnlock{ s = "G3", noti_time = 20241202 , release_time = 20241209, cap = 10.39, value = 1.82 },
            new TokenUnlock{ s = "CGPT", noti_time = 20241203 , release_time = 20241210, cap = 145.67, value = 3.95 },
            new TokenUnlock{ s = "PDA", noti_time = 20241203 , release_time = 20241210, cap = 33.25, value = 0.45 },
            new TokenUnlock{ s = "CHEEL", noti_time = 20241203 , release_time = 20241210, cap = 535.85, value = 110 },
            new TokenUnlock{ s = "TADA", noti_time = 20241203 , release_time = 20241210, cap = 11.19, value = 1.11 },
            new TokenUnlock{ s = "GODS", noti_time = 20241204 , release_time = 20241211, cap = 92.1, value = 1.72 },
            new TokenUnlock{ s = "GFI", noti_time = 20241204 , release_time = 20241211, cap = 70.21, value = 3 },
            new TokenUnlock{ s = "DUEL", noti_time = 20241205 , release_time = 20241212, cap = 16.85, value = 1.22 },
            new TokenUnlock{ s = "APT", noti_time = 20241205 , release_time = 20241212, cap = 7490, value = 158 },
            new TokenUnlock{ s = "DIMO", noti_time = 20241205 , release_time = 20241212, cap = 60.95, value = 5.13 },
            new TokenUnlock{ s = "OAS", noti_time = 20241205 , release_time = 20241212, cap = 155.24, value = 8.6 },
            new TokenUnlock{ s = "NIBI", noti_time = 20241205 , release_time = 20241212, cap = 12.36, value = 2.93 },
            new TokenUnlock{ s = "SWEAT", noti_time = 20241206 , release_time = 20241213, cap = 58.1, value = 3.08 },
            new TokenUnlock{ s = "GFAL", noti_time = 20241207 , release_time = 20241213, cap = 38.08, value = 1.44 },
            new TokenUnlock{ s = "VOXEL", noti_time = 20241207 , release_time = 20241214, cap = 59.23, value = 1.07 },
            new TokenUnlock{ s = "ALI", noti_time = 20241207 , release_time = 20241214, cap = 164.94, value = 2.68 },
            new TokenUnlock{ s = "RSS3", noti_time = 20241207 , release_time = 20241214, cap = 135.03, value = 3.45 },
            new TokenUnlock{ s = "PUFFER", noti_time = 20241207 , release_time = 20241214, cap = 85.41, value = 9.27 },
            new TokenUnlock{ s = "HIGH", noti_time = 20241207 , release_time = 20241214, cap = 155.48, value = 6.18 },
            new TokenUnlock{ s = "SEI", noti_time = 20241208 , release_time = 20241215, cap = 2760, value = 156 },
            new TokenUnlock{ s = "ZTX", noti_time = 20241209 , release_time = 20241216, cap = 28.67, value = 1.56 },
            new TokenUnlock{ s = "ZBCN", noti_time = 20241209 , release_time = 20241216, cap = 90, value = 3 },
            new TokenUnlock{ s = "RARE", noti_time = 20241210 , release_time = 20241217, cap = 101.44, value = 1.88 },
            new TokenUnlock{ s = "APE", noti_time = 20241210 , release_time = 20241217, cap = 1120, value = 22.83 },
            new TokenUnlock{ s = "ACE", noti_time = 20241211 , release_time = 20241218, cap = 110.49, value = 8.68 },
            new TokenUnlock{ s = "TORN", noti_time = 20241211 , release_time = 20241218, cap = 65.78, value = 2.14 },
            new TokenUnlock{ s = "SABAI", noti_time = 20241211 , release_time = 20241218, cap = 12.2, value = 1.37 },
            new TokenUnlock{ s = "CTRL", noti_time = 20241212 , release_time = 20241218, cap = 15.44, value = 0.26 },
            new TokenUnlock{ s = "ZKJ", noti_time = 20241212 , release_time = 20241219, cap = 152.93, value = 28.36 },
            new TokenUnlock{ s = "KARRAT", noti_time = 20241212 , release_time = 20241219, cap = 99.67, value = 7.48 },
            new TokenUnlock{ s = "NUM", noti_time = 20241212 , release_time = 20241219, cap = 61.22, value = 0.88 },
            new TokenUnlock{ s = "PIXEL", noti_time = 20241212 , release_time = 20241219, cap = 278.61, value = 13 },
            new TokenUnlock{ s = "LVN", noti_time = 20241212 , release_time = 20241219, cap = 17.05, value = 1 },
            new TokenUnlock{ s = "MERL", noti_time = 20241212 , release_time = 20241219, cap = 173.84, value = 9.35 },
            new TokenUnlock{ s = "HTM", noti_time = 20241213 , release_time = 20241220, cap = 21.07, value = 2.35 },
            new TokenUnlock{ s = "BCUT", noti_time = 20241213 , release_time = 20241220, cap = 33.28, value = 3.12 },
            new TokenUnlock{ s = "VARA", noti_time = 20241214 , release_time = 20241221, cap = 25.64, value = 6.2 },
            new TokenUnlock{ s = "TAO", noti_time = 20241214 , release_time = 20241221, cap = 125.94, value = 126 },
            new TokenUnlock{ s = "VRTX", noti_time = 20241214 , release_time = 20241221, cap = 38.43, value = 2.75 },
            new TokenUnlock{ s = "XPLA", noti_time = 20241214 , release_time = 20241221, cap = 77.58, value = 9.4 },
            new TokenUnlock{ s = "ID", noti_time = 20241215 , release_time = 20241222, cap = 407.67, value = 40.2 },
            new TokenUnlock{ s = "C98", noti_time = 20241216 , release_time = 20241223, cap = 190.53, value = 3.5 },
            new TokenUnlock{ s = "GTAI", noti_time = 20241218 , release_time = 20241225, cap = 36.28, value = 2.71 },
            new TokenUnlock{ s = "ALT", noti_time = 20241218 , release_time = 20241225, cap = 346.44, value = 29.4 },
            new TokenUnlock{ s = "RAD", noti_time = 20241218 , release_time = 20241225, cap = 75.4, value = 2.26 },
            new TokenUnlock{ s = "AURY", noti_time = 20241219 , release_time = 20241226, cap = 17.94, value = 0.37 },
            new TokenUnlock{ s = "SOVRN", noti_time = 20241219 , release_time = 20241226, cap = 30.61, value = 0.6 },
            new TokenUnlock{ s = "NFP", noti_time = 20241220 , release_time = 20241227, cap = 72.5, value = 3.6 },
            new TokenUnlock{ s = "IMX", noti_time = 20241220 , release_time = 20241227, cap = 2350, value = 33.96 },
            new TokenUnlock{ s = "AXL", noti_time = 20241220 , release_time = 20241227, cap = 642.67, value = 10 },
            new TokenUnlock{ s = "GOAL", noti_time = 20241220 , release_time = 20241227, cap = 19.49, value = 0.75 },
            new TokenUnlock{ s = "EDU", noti_time = 20241221 , release_time = 20241228, cap = 213.24, value = 10.12 },
            new TokenUnlock{ s = "PORTAL", noti_time = 20241222 , release_time = 20241229, cap = 126.84, value = 7.87 },
            new TokenUnlock{ s = "BETA", noti_time = 20241223 , release_time = 20241230, cap = 37.15, value = 0.72 },
            new TokenUnlock{ s = "GEOD", noti_time = 20241224 , release_time = 20241229, cap = 41.84, value = 6.14 },
            new TokenUnlock{ s = "JUV", noti_time = 20241225 , release_time = 20250101, cap = 12.7, value = 0.35 },
            new TokenUnlock{ s = "BAR", noti_time = 20241225 , release_time = 20250101, cap = 20.85, value = 0.94 },
            new TokenUnlock{ s = "ATM", noti_time = 20241225 , release_time = 20250101, cap = 12.42, value = 0.3 },
            new TokenUnlock{ s = "ASR", noti_time = 20241225 , release_time = 20250101, cap = 13.04, value = 0.3 },
            new TokenUnlock{ s = "CFX", noti_time = 20241225 , release_time = 20250101, cap = 801.44, value = 15 },
            new TokenUnlock{ s = "HOOK", noti_time = 20241225 , release_time = 20250101, cap = 92.76, value = 3.65 },
            new TokenUnlock{ s = "ACM", noti_time = 20241225 , release_time = 20250101, cap = 11.09, value = 0.34 },
            new TokenUnlock{ s = "ETHDYDX", noti_time = 20241225 , release_time = 20250101, cap = 341.37, value = 13.16 },
            new TokenUnlock{ s = "COTI", noti_time = 20241225 , release_time = 20250101, cap = 241.35, value = 271.16 },
            new TokenUnlock{ s = "OGN", noti_time = 20241225 , release_time = 20250101, cap = 81.07, value = 3.39 },
            new TokenUnlock{ s = "ORBR", noti_time = 20241226 , release_time = 20250102, cap = 246.68, value = 9 },
            new TokenUnlock{ s = "TAI", noti_time = 20241226 , release_time = 20250102, cap = 268.53, value = 11.22 },
            new TokenUnlock{ s = "NTRN", noti_time = 20241227 , release_time = 20250103, cap = 109.09, value = 3.6 },
            new TokenUnlock{ s = "DAR", noti_time = 20241228 , release_time = 20250104, cap = 93.77, value = 1.36 },
            new TokenUnlock{ s = "AI", noti_time = 20241228 , release_time = 20250104, cap = 159.86, value = 11 },
            new TokenUnlock{ s = "G", noti_time = 20241229 , release_time = 20250105, cap = 258.65, value = 5.63 },
            new TokenUnlock{ s = "LKI", noti_time = 20241230 , release_time = 20250106, cap = 13.8, value = 1.27 },
            new TokenUnlock{ s = "THL", noti_time = 20241230 , release_time = 20250106, cap = 19.89, value = 0.73 },
            new TokenUnlock{ s = "NEON", noti_time = 20241231 , release_time = 20250107, cap = 21.21, value = 20.52 },
            new TokenUnlock{ s = "WIFI", noti_time = 20241231 , release_time = 20250107, cap = 14.37, value = 0.52 },
            new TokenUnlock{ s = "BOSON", noti_time = 20241231 , release_time = 20250107, cap = 41.04, value = 0.73 },
            new TokenUnlock{ s = "MYRIA", noti_time = 20241231 , release_time = 20250107, cap = 55.27, value = 1.6 },
            new TokenUnlock{ s = "ACH", noti_time = 20241231 , release_time = 20250107, cap = 221.36, value = 3 },
            new TokenUnlock{ s = "JTO", noti_time = 20241231 , release_time = 20250107, cap = 898.25, value = 36.74 },
            new TokenUnlock{ s = "HFT", noti_time = 20241231 , release_time = 20250107, cap = 99.93, value = 3.25 },
            new TokenUnlock{ s = "NAVX", noti_time = 20250101 , release_time = 20250107, cap = 38.94, value = 3.62 },
            new TokenUnlock{ s = "SLF", noti_time = 20250101 , release_time = 20250108, cap = 31.1, value = 3.83 },
            new TokenUnlock{ s = "SD", noti_time = 20250101 , release_time = 20250108, cap = 72.66, value = 1.88 },
            new TokenUnlock{ s = "ENS", noti_time = 20250101 , release_time = 20250108, cap = 1170, value = 48.32 },
            new TokenUnlock{ s = "SCA", noti_time = 20250101 , release_time = 20250108, cap = 19.8, value = 1.39 },
            new TokenUnlock{ s = "GMT", noti_time = 20250102 , release_time = 20250109, cap = 399.96, value = 14 },
            new TokenUnlock{ s = "G3", noti_time = 20250102 , release_time = 20250109, cap = 21.43, value = 2 },
            new TokenUnlock{ s = "ALPHA", noti_time = 20250103 , release_time = 20250110, cap = 76.94, value = 1.1 },
            new TokenUnlock{ s = "CGPT", noti_time = 20250103 , release_time = 20250110, cap = 214.26, value = 5.54 },
            new TokenUnlock{ s = "CHEEL", noti_time = 20250103 , release_time = 20250110, cap = 95.22, value = 95.22 },
            new TokenUnlock{ s = "TADA", noti_time = 20250104 , release_time = 20250110, cap = 10.35, value = 0.82 },
            new TokenUnlock{ s = "GODS", noti_time = 20250104 , release_time = 20250111, cap = 75.2, value = 1.38 },
            new TokenUnlock{ s = "GFI", noti_time = 20250104 , release_time = 20250111, cap = 50.04, value = 2.14 },
            new TokenUnlock{ s = "APT", noti_time = 20250105 , release_time = 20250112, cap = 5570, value = 112.9 },
            new TokenUnlock{ s = "DIMO", noti_time = 20250105 , release_time = 20250112, cap = 49.86, value = 4.1 },
            new TokenUnlock{ s = "OAS", noti_time = 20250105 , release_time = 20250112, cap = 122.43, value = 5.7 },
            new TokenUnlock{ s = "SWEAT", noti_time = 20250106 , release_time = 20250113, cap = 53.18, value = 2.81 },
            new TokenUnlock{ s = "DUEL", noti_time = 20250107 , release_time = 20250112, cap = 14.08, value = 1.79 },
            new TokenUnlock{ s = "GFAL", noti_time = 20250107 , release_time = 20250113, cap = 29.43, value = 1 },
            new TokenUnlock{ s = "VOXEL", noti_time = 20250107 , release_time = 20250114, cap = 43, value = 0.77 },
            new TokenUnlock{ s = "ALI", noti_time = 20250107 , release_time = 20250114, cap = 93.93, value = 1.53 },
            new TokenUnlock{ s = "RSS3", noti_time = 20250107 , release_time = 20250114, cap = 95.67, value = 2.45 },
            new TokenUnlock{ s = "PUFFER", noti_time = 20250107 , release_time = 20250114, cap = 78.37, value = 8.51 },
            new TokenUnlock{ s = "SEI", noti_time = 20250108 , release_time = 20250115, cap = 1750, value = 93.56 },
            new TokenUnlock{ s = "BURGER", noti_time = 20250108 , release_time = 20250115, cap = 25.9, value = 1.29 },
            new TokenUnlock{ s = "ZTX", noti_time = 20250109 , release_time = 20250116, cap = 26.01, value = 1.42 },
            new TokenUnlock{ s = "ZBCN", noti_time = 20250109 , release_time = 20250116, cap = 61.99, value = 2 },
            new TokenUnlock{ s = "RARE", noti_time = 20250110 , release_time = 20250117, cap = 83.04, value = 1.38 },
            new TokenUnlock{ s = "APE", noti_time = 20250110 , release_time = 20250117, cap = 822.12, value = 16.8 },
            new TokenUnlock{ s = "ACE", noti_time = 20250111 , release_time = 20250118, cap = 83.11, value = 6 },
            new TokenUnlock{ s = "MANTA", noti_time = 20250111 , release_time = 20250118, cap = 306.19, value = 10.83 },
            new TokenUnlock{ s = "GENE", noti_time = 20250111 , release_time = 20250118, cap = 31.36, value = 0.5 },
            new TokenUnlock{ s = "ONDO", noti_time = 20250112 , release_time = 20250118, cap = 1750, value = 3240 },
            new TokenUnlock{ s = "ZKJ", noti_time = 20250112 , release_time = 20250119, cap = 217.3, value = 30 },
            new TokenUnlock{ s = "KARRAT", noti_time = 20250112 , release_time = 20250119, cap = 54.99, value = 3.71 },
            new TokenUnlock{ s = "NUM", noti_time = 20250112 , release_time = 20250119, cap = 34.89, value = 0.5 },
            new TokenUnlock{ s = "PIXEL", noti_time = 20250112 , release_time = 20250119, cap = 171.96, value = 7.62 },
            new TokenUnlock{ s = "MERL", noti_time = 20250112 , release_time = 20250119, cap = 119.9, value = 6.45 },
            new TokenUnlock{ s = "HTM", noti_time = 20250113 , release_time = 20250120, cap = 15.52, value = 4.68 },
            new TokenUnlock{ s = "BCUT", noti_time = 20250113 , release_time = 20250120, cap = 20.17, value = 1.79 },
            new TokenUnlock{ s = "VARA", noti_time = 20250114 , release_time = 20250121, cap = 31.3, value = 4.09 },
            new TokenUnlock{ s = "TAO", noti_time = 20250114 , release_time = 20250121, cap = 3440, value = 91.18 },
            new TokenUnlock{ s = "VRTX", noti_time = 20250114 , release_time = 20250121, cap = 32.45, value = 2.21 },
            new TokenUnlock{ s = "C98", noti_time = 20250116 , release_time = 20250123, cap = 138.06, value = 2.48 },
            new TokenUnlock{ s = "IMX", noti_time = 20250117 , release_time = 20250124, cap = 2320, value = 33.13 },
            new TokenUnlock{ s = "GTAI", noti_time = 20250118 , release_time = 20250125, cap = 29.75, value = 2.15 },
            new TokenUnlock{ s = "ALT", noti_time = 20250118 , release_time = 20250125, cap = 249.37, value = 21.16 },
            new TokenUnlock{ s = "RAD", noti_time = 20250118 , release_time = 20250125, cap = 67.03, value = 2 },
            new TokenUnlock{ s = "AURY", noti_time = 20250119 , release_time = 20250126, cap = 13.14, value = 0.26 },
            new TokenUnlock{ s = "SOVRN", noti_time = 20250119 , release_time = 20250126, cap = 47.5, value = 2.94 },
            new TokenUnlock{ s = "NFP", noti_time = 20250120 , release_time = 20250127, cap = 80.02, value = 3 },
            new TokenUnlock{ s = "AXL", noti_time = 20250120 , release_time = 20250127, cap = 525.19, value = 7.88 },
            new TokenUnlock{ s = "EDU", noti_time = 20250121 , release_time = 20250128, cap = 207.88, value = 9.65 },
            new TokenUnlock{ s = "MAV", noti_time = 20250121 , release_time = 20250128, cap = 75.72, value = 7.8 },
            new TokenUnlock{ s = "PORTAL", noti_time = 20250122 , release_time = 20250129, cap = 108.14, value = 6.31 },
            new TokenUnlock{ s = "BTT", noti_time = 20250124 , release_time = 20250131, cap =1040 , value = 19.11 },
            new TokenUnlock{ s = "WLTH", noti_time = 20250125 , release_time = 20250130, cap = 13.92, value = 0.38 },
            new TokenUnlock{ s = "JUV", noti_time = 20250125 , release_time = 20250201, cap = 10.62, value = 0.3 },
            new TokenUnlock{ s = "BAR", noti_time = 20250125 , release_time = 20250201, cap = 17.6, value = 0.8 },
            new TokenUnlock{ s = "ATM", noti_time = 20250125 , release_time = 20250201, cap = 11.2, value = 0.25 },
            new TokenUnlock{ s = "ASR", noti_time = 20250125 , release_time = 20250201, cap = 11.05, value = 0.24 },
            new TokenUnlock{ s = "CFX", noti_time = 20250125 , release_time = 20250201, cap = 712.94, value = 12.92 },
            new TokenUnlock{ s = "HOOK", noti_time = 20250125 , release_time = 20250201, cap = 61.08, value = 2.34 },
            new TokenUnlock{ s = "AVA", noti_time = 20250125 , release_time = 20250201, cap = 49.01, value = 1 },
            new TokenUnlock{ s = "ETHDYDX", noti_time = 20250125 , release_time = 20250201, cap = 245.61, value = 9.51 },
            new TokenUnlock{ s = "OGN", noti_time = 20250125 , release_time = 20250201, cap = 65.68, value = 1.5 },
            new TokenUnlock{ s = "ORBR", noti_time = 20250126 , release_time = 20250202, cap = 189.68, value = 7 },
            new TokenUnlock{ s = "NTRN", noti_time = 20250127 , release_time = 20250203, cap = 89.99, value = 2.9 },
            new TokenUnlock{ s = "D", noti_time = 20250128 , release_time = 20250204, cap = 85.75, value = 2.79 },
            new TokenUnlock{ s = "AI", noti_time = 20250128 , release_time = 20250204, cap = 134.59, value = 8.9 },
            new TokenUnlock{ s = "G", noti_time = 20250129 , release_time = 20250205, cap = 212.37, value = 7.8 },
            new TokenUnlock{ s = "MAVIA", noti_time = 20250130 , release_time = 20250206, cap = 20.12, value = 5.61 },
            new TokenUnlock{ s = "THL", noti_time = 20250130 , release_time = 20250206, cap = 11.44, value = 0.42 },
            new TokenUnlock{ s = "NEON", noti_time = 20250131 , release_time = 20250207, cap = 22.44, value = 21.46 },
            new TokenUnlock{ s = "WIFI", noti_time = 20250131 , release_time = 20250207, cap = 13.75, value = 0.5 },
            new TokenUnlock{ s = "BOSON", noti_time = 20250131 , release_time = 20250207, cap = 27.83, value = 0.5 },
            new TokenUnlock{ s = "MYRIA", noti_time = 20250131 , release_time = 20250207, cap = 59.34, value = 1.65 },
            new TokenUnlock{ s = "ACH", noti_time = 20250131 , release_time = 20250207, cap = 386.38, value = 5.23 },
            new TokenUnlock{ s = "TKO", noti_time = 20250131 , release_time = 20250207, cap = 60.32, value = 6.71 },
            new TokenUnlock{ s = "JTO", noti_time = 20250131 , release_time = 20250207, cap = 963.62, value = 37.75 },
            new TokenUnlock{ s = "HFT", noti_time = 20250131 , release_time = 20250207, cap = 75.75, value = 2.38 },
            new TokenUnlock{ s = "ATA", noti_time = 20250131 , release_time = 20250207, cap = 58.4, value = 2.62 },
            new TokenUnlock{ s = "SLF", noti_time = 20250201 , release_time = 20250208, cap = 23.17, value = 2.36 },
            new TokenUnlock{ s = "SD", noti_time = 20250201 , release_time = 20250208, cap = 48.95, value = 1.27 },
            new TokenUnlock{ s = "ENS", noti_time = 20250201 , release_time = 20250208, cap = 1200, value = 49.51 },
            new TokenUnlock{ s = "SCA", noti_time = 20250201 , release_time = 20250208, cap = 13.92, value = 0.9 },
            new TokenUnlock{ s = "GMT", noti_time = 20250202 , release_time = 20250209, cap = 225.67, value = 7.9 },
            new TokenUnlock{ s = "CGPT", noti_time = 20250203 , release_time = 20250210, cap = 126.63, value = 2.24 },
            new TokenUnlock{ s = "CHEEL", noti_time = 20250203 , release_time = 20250210, cap = 446.45, value = 93.1 },
            new TokenUnlock{ s = "GODS", noti_time = 20250203 , release_time = 20250211, cap = 46.34, value = 0.83 },
            new TokenUnlock{ s = "GFI", noti_time = 20250203 , release_time = 20250211, cap = 38.83, value = 1.66 },
            new TokenUnlock{ s = "BOBA", noti_time = 20250205 , release_time = 20250212, cap = 27.66, value = 3.63 },
            new TokenUnlock{ s = "DIMO", noti_time = 20250205 , release_time = 20250212, cap = 29.41, value = 2.21 },
            new TokenUnlock{ s = "OAS", noti_time = 20250205 , release_time = 20250212, cap = 85.01, value = 3.83 },
            new TokenUnlock{ s = "NIBI", noti_time = 20250205 , release_time = 20250212, cap = 13.68, value = 0.36 },
            new TokenUnlock{ s = "SWEAT", noti_time = 20250205 , release_time = 20250213, cap = 63.11, value = 3.24 },
            new TokenUnlock{ s = "GFAL", noti_time = 20250207 , release_time = 20250213, cap = 21.49, value = 0.77 },
            new TokenUnlock{ s = "SAND", noti_time = 20250207 , release_time = 20250214, cap = 914.89, value = 70 },
            new TokenUnlock{ s = "VOXEL", noti_time = 20250207 , release_time = 20250214, cap = 19.98, value = 0.35 },
            new TokenUnlock{ s = "ALI", noti_time = 20250207 , release_time = 20250214, cap = 72.64, value = 1.18 },
            new TokenUnlock{ s = "RSS3", noti_time = 20250207 , release_time = 20250214, cap = 47.23, value = 1.21 },
            new TokenUnlock{ s = "PUFFER", noti_time = 20250207 , release_time = 20250214, cap = 45, value = 4.89 },
            new TokenUnlock{ s = "CYBER", noti_time = 20250207 , release_time = 20250215, cap = 52.3, value = 8.54 },
            new TokenUnlock{ s = "MLN", noti_time = 20250207 , release_time = 20250215, cap = 34.29, value = 3.86 },
            new TokenUnlock{ s = "SEI", noti_time = 20250207 , release_time = 20250215, cap = 965.3, value = 48.97 },
            new TokenUnlock{ s = "ALPINE", noti_time = 20250207 , release_time = 20250215, cap = 10.44, value = 8.71 },
            new TokenUnlock{ s = "ZTX", noti_time = 20250209 , release_time = 20250216, cap = 19.43, value = 1.06 },
            new TokenUnlock{ s = "ZBCN", noti_time = 20250209 , release_time = 20250216, cap = 60.36, value = 1.88 },
            new TokenUnlock{ s = "RARE", noti_time = 20250210 , release_time = 20250217, cap = 55.66, value = 0.9 },
            new TokenUnlock{ s = "APE", noti_time = 20250210 , release_time = 20250217, cap = 526.8, value = 10.76 },
        };


        public class TokenUnlock
        {
            public string s { get; set; }
            public long noti_time { get; set; }
            public long release_time { get; set; }
            public double cap { get; set; }
            public double value { get; set; }
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
