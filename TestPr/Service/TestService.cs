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

        //LONG RSI Tong(55): 883.0%|W/L: 468/225
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
                    "1000BONKUSDT",
                    "B3USDT",
                    "SAFEUSDT",
                    "LUMIAUSDT",
                    "PLUMEUSDT",
                    "RAREUSDT",
                    "VIRTUALUSDT",
                    "VTHOUSDT",
                    "INJUSDT",
                    "QTUMUSDT",
                    "ARKMUSDT",
                    "ORDIUSDT",
                    "ALTUSDT",
                    "ADAUSDT",
                    "CETUSUSDT",
                    "PHBUSDT",
                    "DODOXUSDT",
                    "GRTUSDT",
                    "BADGERUSDT",
                    "NFPUSDT",
                    "BSWUSDT",
                    "RAYSOLUSDT",
                    "1000CHEEMSUSDT",
                    "THEUSDT",
                    "BIOUSDT",
                    "SEIUSDT",
                    "ZRXUSDT",
                    "ONDOUSDT",
                    "LPTUSDT",
                    "CFXUSDT",
                    "BRETTUSDT",
                    "CATIUSDT",
                    "EPICUSDT",
                    "PHAUSDT",
                    "IOTAUSDT",
                    "IOSTUSDT",
                    "ALGOUSDT",
                    "KSMUSDT",
                    "GLMUSDT",
                    "BTCUSDT",
                    "STORJUSDT",
                    "1000FLOKIUSDT",
                    "LQTYUSDT",
                    "TOKENUSDT",
                    "ACXUSDT",
                    "ETCUSDT",
                    "SANDUSDT",
                    "ANKRUSDT",
                    "MTLUSDT",
                    "MAGICUSDT",
                    "BBUSDT",
                    "RENDERUSDT",
                    "PROMUSDT",
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
                                var maVol = lMaVol.First(x => x.Date == ma20.Date);

                                if (cur.Close >= cur.Open
                                    || ma20.Sma is null
                                    || rsi.Rsi > 35
                                    || cur.Low >= (decimal)ma20.LowerBand.Value
                                    //|| cur.High >= (decimal)ma20.Sma.Value
                                    || Math.Abs(minOpenClose - (decimal)ma20.LowerBand.Value) > Math.Abs((decimal)ma20.Sma.Value - minOpenClose)
                                    )
                                    continue;

                                if(!StaticVal._lCoinSpecial.Contains(item))
                                {
                                    if (cur.Volume < (decimal)(maVol.Sma.Value * 1.5))
                                        continue;
                                }    

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

                                    var rateCheck = Math.Round(100 * (-1 + itemClose.High / eEntry.Close), 1);
                                    if (rateCheck > 10)
                                    {
                                        var close = eEntry.Close * (decimal)1.1;
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
                            if (perRate < 0.8)
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
        //SHORT RSI Tong(52): 654.5%|W/L: 344/167
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
                    "1000XUSDT",
                    "HEIUSDT",
                    "OMUSDT",
                    "CATIUSDT",
                    "GRIFFAINUSDT",
                    "GPSUSDT",
                    "DUSKUSDT",
                    "LTCUSDT",
                    "BIOUSDT",
                    "NEIROUSDT",
                    "PONKEUSDT",
                    "LINKUSDT",
                    "MORPHOUSDT",
                    "AVAAIUSDT",
                    "VICUSDT",
                    "KASUSDT",
                    "DYDXUSDT",
                    "HOOKUSDT",
                    "XVGUSDT",
                    "PENGUUSDT",
                    "BADGERUSDT",
                    "BAKEUSDT",
                    "CAKEUSDT",
                    "ONDOUSDT",
                    "OMNIUSDT",
                    "1MBABYDOGEUSDT",
                    "SAFEUSDT",
                    "VETUSDT",
                    "API3USDT",
                    "1000000MOGUSDT",
                    "RUNEUSDT",
                    "BERAUSDT",
                    "AGLDUSDT",
                    "CRVUSDT",
                    "TRUUSDT",
                    "TWTUSDT",
                    "DEXEUSDT",
                    "IPUSDT",
                    "CHESSUSDT",
                    "PROMUSDT",
                    "CTSIUSDT",
                    "EDUUSDT",
                    "ETHWUSDT",
                    "FIOUSDT",
                    "MOODENGUSDT",
                    "QTUMUSDT",
                    "ALPHAUSDT",
                    "APEUSDT",
                    "ORDIUSDT",
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
                                var maVol = lMaVol.First(x => x.Date == ma20.Date);

                                if (
                                    cur.Close <= cur.Open
                                   || ma20.Sma is null
                                   || rsi.Rsi < 65
                                   || cur.High <= (decimal)ma20.UpperBand.Value
                                   //|| cur.High >= (decimal)ma20.Sma.Value
                                   || Math.Abs(maxOpenClose - (decimal)ma20.UpperBand.Value) > Math.Abs((decimal)ma20.Sma.Value - maxOpenClose)
                                   )
                                    continue;

                                if (!StaticVal._lCoinSpecial.Contains(item))
                                {
                                    if (cur.Volume < (decimal)(maVol.Sma.Value * 1.5))
                                        continue;
                                }

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

                                    var rateCheck = Math.Round(100 * (-1 + eEntry.Close / itemClose.Low), 1);
                                    if (rateCheck > 10)
                                    {
                                        var close = eEntry.Close * (decimal)0.9;
                                        itemClose.Close = close;
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
                            if (perRate < 0.8)
                            {
                                var lRemove = lModel.Where(x => x.s == item);
                                lModel = lModel.Except(lRemove).ToList();
                                continue;
                            }

                            Console.WriteLine($"{item}\t\t\t| W/Total: {realWin}/{lModel.Count(x => x.s == item)} = {rate}%|Rate: {sumRate}%|Per: {perRate}%");

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

        //LONG RSI Tong(55): 918.2%|W/L: 507/282
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

                var lModel = new List<LongMa20>();

                var winTotal = 0;
                var lossTotal = 0;

                #region comment
                lTake.Clear();
                var lTmp = new List<string>
                {
                    "DGBUSDT",
                    "GPSUSDT",
                    "AUDIOUSDT",
                    "ZBCNUSDT",
                    "GMTUSDT",
                    "MEMEUSDT",
                    "SERAPHUSDT",
                    "VIRTUALUSDT",
                    "PENDLEUSDT",
                    "AERGOUSDT",
                    "RAREUSDT",
                    "RAYDIUMUSDT",
                    "PYTHUSDT",
                    "FLRUSDT",
                    "GOATUSDT",
                    "DATAUSDT",
                    "MANEKIUSDT",
                    "MAXUSDT",
                    "PHBUSDT",
                    "AI16ZUSDT",
                    "SLFUSDT",
                    "XCHUSDT",
                    "ALTUSDT",
                    "GLMUSDT",
                    "POPCATUSDT",
                    "ZILUSDT",
                    "PROMUSDT",
                    "BIGTIMEUSDT",
                    "FIDAUSDT",
                    "BNBUSDT",
                    "KNCUSDT",
                    "LUMIAUSDT",
                    "LUCEUSDT",
                    "MOVRUSDT",
                    "ANKRUSDT",
                    "GLMRUSDT",
                    "BSWUSDT",
                    "BTCUSDT",
                    "FLOCKUSDT",
                    "LPTUSDT",
                    "KAVAUSDT",
                    "TLMUSDT",
                    "MAVUSDT",
                    "TOKENUSDT",
                    "QTUMUSDT",
                    "ACXUSDT",
                    "ARKMUSDT",
                    "FLUXUSDT",
                    "INJUSDT",
                    "LDOUSDT",
                    "LQTYUSDT",
                    "MTLUSDT",
                    "PLUMEUSDT",
                    "XVGUSDT",
                    "SUNDOGUSDT",
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
                                    || entity_Sig.Volume < (decimal)(maVol_Sig.Sma.Value * 1.5)
                                    )
                                    continue;

                                if (entity_Pivot is null
                                   || rsi_Pivot.Rsi > 35 || rsi_Pivot.Rsi < 25
                                   || entity_Pivot.Low >= (decimal)bb_Pivot.LowerBand.Value
                                   || entity_Pivot.High >= (decimal)bb_Pivot.Sma.Value
                                   || (entity_Pivot.Low >= entity_Sig.Low && entity_Pivot.High <= entity_Sig.High)
                                   )
                                    continue;

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
                                Quote tmpClose = null;
                                foreach (var itemClose in lClose)
                                {
                                    var ma = lbb.First(x => x.Date == itemClose.Date);
                                    if (itemClose.Close > (decimal)ma.UpperBand)//do something
                                    {
                                        eClose = itemClose;
                                        break;
                                    }

                                    var rateCheck = Math.Round(100 * (-1 + itemClose.High / eEntry.Close), 1); //chốt khi lãi > 10%
                                    if(rateCheck > 10)
                                    {
                                        var close = eEntry.Close * (decimal)1.1;
                                        itemClose.Close = close;
                                        eClose = itemClose;
                                        break;
                                    }

                                    if (tmpClose != null)
                                    {
                                        if (tmpClose.Close > tmpClose.Open
                                            && itemClose.Volume / tmpClose.Volume < (decimal)0.6)
                                        {
                                            var bbTmp = lbb.First(x => x.Date == itemClose.Date);
                                            if (Math.Max(itemClose.Close, itemClose.Open) > (decimal)bbTmp.Sma.Value
                                                && (itemClose.Close - (decimal)bbTmp.Sma.Value) > ((decimal)bbTmp.UpperBand.Value - itemClose.Close))
                                            {
                                                eClose = itemClose;
                                                break;
                                            }
                                        }
                                    }
                                    tmpClose = itemClose;
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

                        //if (winCount <= lossCount)
                        //    continue;
                        if (winCount + lossCount <= 0)
                            continue;

                        var rateRes = Math.Round(((decimal)winCount / (winCount + lossCount)), 2);
                        var sumRate = lModel.Where(x => x.s == item).Sum(x => x.Rate);
                        var count = lModel.Count(x => x.s == item);
                        var items = lModel.Where(x => x.s == item);
                        //Special 
                        //if (rateRes <= (decimal)0.5
                        //  || sumRate <= 1
                        //  || sumRate / count <= (decimal)0.5)
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

                        var perRate = Math.Round((float)sumRate / count, 1);
                        if (perRate < 0.8)
                        {
                            lModel = lModel.Except(items).ToList();
                            continue;
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
                    "MAVIAUSDT",
                    "HEIUSDT",
                    "JUSDT",
                    "CATIUSDT",
                    "OLUSDT",
                    "MORPHOUSDT",
                    "NCUSDT",
                    "IDEXUSDT",
                    "XVSUSDT",
                    "ZBCNUSDT",
                    "AIOZUSDT",
                    "CPOOLUSDT",
                    "TROYUSDT",
                    "ALUUSDT",
                    "XVGUSDT",
                    "MAJORUSDT",
                    "XCHUSDT",
                    "OMNIUSDT",
                    "XNOUSDT",
                    "OMUSDT",
                    "ZENUSDT",
                    "FWOGUSDT",
                    "FLUXUSDT",
                    "VICUSDT",
                    "PENGUUSDT",
                    "PERPUSDT",
                    "EGLDUSDT",
                    "RDNTUSDT",
                    "FLOCKUSDT",
                    "MEMEFIUSDT",
                    "SHELLUSDT",
                    "ENSUSDT",
                    "SPECUSDT",
                    "KASUSDT",
                    "FOXYUSDT",
                    "DYDXUSDT",
                    "PORTALUSDT",
                    "ROSEUSDT",
                    "ZRCUSDT",
                    "AEROUSDT",
                    "ALICEUSDT",
                    "GODSUSDT",
                    "LINKUSDT",
                    "XTZUSDT",
                    "SPELLUSDT",
                    "BLURUSDT",
                    "VTHOUSDT",
                    "ANKRUSDT",
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
                                var maVol = lMaVol.First(x => x.Date == ma20.Date);

                                if (
                                    cur.Close <= cur.Open
                                   || ma20.Sma is null
                                   || rsi.Rsi < 65
                                   || cur.High <= (decimal)ma20.UpperBand.Value
                                   //|| cur.High >= (decimal)ma20.Sma.Value
                                   || Math.Abs(maxOpenClose - (decimal)ma20.UpperBand.Value) > Math.Abs((decimal)ma20.Sma.Value - maxOpenClose)
                                   )
                                    continue;

                                if (!StaticVal._lCoinSpecial.Contains(item))
                                {
                                    if (cur.Volume < (decimal)(maVol.Sma.Value * 1.5))
                                        continue;
                                }

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

                                    var rateCheck = Math.Round(100 * (-1 + eEntry.Close / itemClose.Low), 1);
                                    if (rateCheck > 10)
                                    {
                                        var close = eEntry.Close * (decimal)0.9;
                                        itemClose.Close = close;
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
                            if (perRate < 0.8)
                            {
                                var lRemove = lModel.Where(x => x.s == item);
                                lModel = lModel.Except(lRemove).ToList();
                                continue;
                            }
                            Console.WriteLine($"{item}\t\t\t| W/Total: {realWin}/{count} = {rate}%|Rate: {sumRate}%|Per: {perRate}%");

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



