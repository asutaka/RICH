using Skender.Stock.Indicators;
using StockPr.DAL;
using StockPr.Model;
using StockPr.Utils;

namespace StockPr.Service
{
    public interface ITestService
    {
        Task CheckAllDay_OnlyVolume();
        Task BatDayCK();
        Task CheckGDNN();
        Task CheckCungCau();
        Task CheckCrossMa50_BB();
    }
    public class TestService : ITestService
    {
        private readonly ILogger<TestService> _logger;
        private readonly IAPIService _apiService;
        private readonly ISymbolRepo _symbolRepo;
        public TestService(ILogger<TestService> logger, IAPIService apiService, ISymbolRepo symbolRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _symbolRepo = symbolRepo;
        }

        public async Task BatDayCK()
        {
            try
            {
                decimal SL_RATE = 10m;//1.5,1.6,1.8,1.9,2
                int hour = 10;//1h,2h,3h,4h

                var lMesAll = new List<string>();

                var winTotal = 0;
                var lossTotal = 0;
                var lPoint = new List<clsPoint>();
                var lTrace = new List<clsTrace>();
                var lResult = new List<clsResult>();
                var lsym = _symbolRepo.GetAll();
                foreach (var item in lsym.Select(x => x.s))
                {
                    try
                    {
                        //if (item != "AAA")
                        //    continue;

                        var lMes = new List<string>();
                        var lData = await _apiService.SSI_GetDataStock(item);
                        var lbb_Total = lData.GetBollingerBands();
                        var lMaVol_Total = lData.Select(x => new Quote
                        {
                            Date = x.Date,
                            Close = x.Volume
                        }).GetSma(20);
                        Thread.Sleep(200);
                        if (lData == null || !lData.Any() || lData.Count() < 250 || lData.Last().Volume < 10000)
                            continue;
                        var count = lData.Count();
                        var take = 250;
                        do
                        {
                            var lData15m = lData.Take(take++);

                            var lbb = lData15m.GetBollingerBands();
                            var lrsi = lData15m.GetRsi();
                            var lMaVol = lData15m.Select(x => new Quote
                            {
                                Date = x.Date,
                                Close = x.Volume
                            }).GetSma(20);


                            var last = lData15m.Last();
                            var cur = lData15m.SkipLast(1).Last();
                            var bb_Last = lbb.First(x => x.Date == last.Date);
                            var bb_Cur = lbb.First(x => x.Date == cur.Date);

                            //if(item == "BFC" && last.Date.Year == 2025 && last.Date.Month == 4 && last.Date.Day == 9)
                            //{
                            //    var zz = 1;
                            //}    

                            if (cur.Low <= last.Low && cur.High >= last.High)
                                continue;

                            var volCheck = last.Volume / cur.Volume;
                            if (volCheck < 1.2m)
                                continue;

                            var isPinbar = ((Math.Min(last.Open, last.Close) - last.Low) >= 3 * (last.High - Math.Min(last.Open, last.Close)))
                                            && (Math.Abs(last.Open - last.Close) >= 0.1m * (last.High - last.Low));
                            if (!isPinbar
                                && last.Open > last.Close)
                                continue;

                            if (isPinbar 
                                && Math.Min(last.Open, last.Close) >= Math.Min(cur.Open, cur.Close))
                                continue;

                            if (last.Close > last.Open
                                && last.High <= cur.High
                                && last.Low >= cur.Low)
                                continue;

                            var posCheck_Cur = (cur.Close - (decimal)bb_Cur.LowerBand.Value) > 0 && Math.Abs((decimal)bb_Cur.Sma.Value - cur.Close) < 3 * Math.Abs(cur.Close - (decimal)bb_Cur.LowerBand.Value);
                            if (posCheck_Cur)
                                continue;

                            var posCheck_Last = (last.Close - (decimal)bb_Last.LowerBand.Value) > 0 && Math.Abs((decimal)bb_Last.Sma.Value - last.Close) < 2 * Math.Abs(last.Close - (decimal)bb_Last.LowerBand.Value);
                            if (posCheck_Last)
                                continue;

                            var isSignal = false;
                            var lSignal = lData15m.SkipLast(1).TakeLast(6);
                            var countSignal = lSignal.Count();
                            for (int i = 0; i < countSignal - 2; i++)
                            {
                                var curSignal = lSignal.ElementAt(i);
                                var curPivot = lSignal.ElementAt(i + 1);
                                if(curPivot.Volume / curSignal.Volume <= 0.6m)
                                {
                                    isSignal = true;
                                    break;
                                }
                            }

                            //Console.WriteLine($"{item}|{last.Date.ToString("dd/MM/yyyy")}");
                            if (last.Date.Year == 2025 && last.Date.Month == 4)
                                continue;
                            var model = new clsTrace
                            {
                                s = item,
                                date = last.Date,
                                entry = last.Close,
                                isSignal = isSignal,
                                isCrossMa20Vol = (decimal)lMaVol.First(x => x.Date == last.Date).Sma.Value * 0.9m <= last.Volume
                            };

                            if (!model.isCrossMa20Vol)
                                continue;
                            

                            lTrace.Add(model);

                            var tp = TakeProfit(model, lData, lbb_Total, lMaVol_Total);
                            if(tp != null)
                            {
                                lResult.Add(tp);
                            }
                        }
                        while (take <= count);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                }

                foreach (var item in lResult.OrderBy(x => x.s))
                {
                    Console.WriteLine($"{item.s}|BUY: {item.date.ToString("dd/MM/yyyy")}|SELL: {item.dateSell.ToString("dd/MM/yyyy")}|Rate: {item.Signal}%");
                }

                var sumT3 = lResult.Sum(x => x.T3);
                var sumT5 = lResult.Sum(x => x.T5);
                var sumT10 = lResult.Sum(x => x.T10);
                var sumSignal = lResult.Sum(x => x.Signal);
                Console.WriteLine($"Total({lResult.Count()})| T3({lResult.Count(x => x.T3 > 0)}): {sumT3}%| T5({lResult.Count(x => x.T5 > 0)}): {sumT5}%| T10({lResult.Count(x => x.T10 > 0)}): {sumT10}%| Signal({lResult.Count(x => x.Signal > 0)}): {sumSignal}%|End: {lResult.Count(x => x.IsEnd)}");

                var tmp = 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
            }
        }

        public clsResult TakeProfit(clsTrace param, List<Quote> lData, IEnumerable<BollingerBandsResult> lBB, IEnumerable<SmaResult> lMaVol)
        {
            try
            {
                //if(param.s == "GIL" && param.date.Year == 2025 && param.date.Month == 4 && param.date.Day == 9)
                //{
                //    var tmp = 1;
                //}

                var lCheck = lData.Where(x => x.Date > param.date).Take(10);//Giữ tối đa 2 tuần
                if (lCheck.Count() < 10)
                    return null;
                //SL: 7% -> 10%: chưa check
                var rateT3 = Math.Round(100 * (-1 + lCheck.Skip(2).First().Close / param.entry), 1);
                var rateT5 = Math.Round(100 * (-1 + lCheck.Skip(4).First().Close / param.entry), 1);
                var rateT10 = Math.Round(100 * (-1 + lCheck.Skip(9).First().Close / param.entry), 1);
                /*
                    Tín hiệu 1: 
                - Nến close vượt ma rồi lại quay lại đỏ và nằm dưới MA(có cần xem nến nằm dưới là pivot hay ko)
                -    Signal xanh + vol pivot giảm 40% tại: 
                        - 
                            + pivot đỏ hoặc Doji 
                            + pivot thuộc 1/3 và dưới MA
                        -
                            + pivot thuộc 1/3 Upper trở lên 
                            + vol sig > 1.2 ma20 vol
                 */
                //                 Signal(Close) thuộc 1/3 dưới MA 
                // (Signal(Close) thuộc 1/3 dưới MA hoặc signal xanh) 


                var GiamToiThieu = 0.4m;//40% -> 30%, 50%
                var SL = -7;//StopLoss
                var IsSell = false;
                var priceSell = lCheck.Last().Close;
                var dateSell = lCheck.Last().Date;
                var IsEnd = false;
                for (int i = 1; i < 10; i++)
                {
                    if (i == 9)
                        IsEnd = true;

                    var sig = lCheck.ElementAt(i - 1);
                    var pivot = lCheck.ElementAt(i);
                    if (pivot.High == pivot.Low)
                        continue;

                    var bb_Sig = lBB.First(x => x.Date == sig.Date);
                    var bb_Pivot = lBB.First(x => x.Date == pivot.Date);
                    var Len_Pivot = Math.Abs((pivot.Open - pivot.Close) / (pivot.High - pivot.Low));//Độ dài nến pivot
                    var isAboveMA = pivot.Close > (decimal)bb_Pivot.Sma.Value;
                    var isAboveUpper = pivot.Close > (decimal)bb_Pivot.UpperBand.Value;
                    var ma1_3 = (pivot.Close - (decimal)bb_Pivot.LowerBand.Value) >= 2 * ((decimal)bb_Pivot.Sma.Value - pivot.Close);
                    var ma1_3_Signal = (sig.Close - (decimal)bb_Sig.LowerBand.Value) >= 2 * ((decimal)bb_Sig.Sma.Value - sig.Close);
                    var upper1_3 = (pivot.Close - (decimal)bb_Pivot.Sma.Value) >= 2 * ((decimal)bb_Pivot.UpperBand.Value - pivot.Close);
                    var ratePivot = Math.Round(100 * (-1 + Math.Min(pivot.Open, pivot.Close) / param.entry), 1);
                    var maVolSignal = lMaVol.First(x => x.Date == sig.Date);

                    if (IsSell && i > 2) 
                    {
                        //sell
                        priceSell = pivot.Open;
                        dateSell = pivot.Date;
                        break;
                    }

                    if (ratePivot < SL)
                    {
                        if (i > 2)
                        {
                            priceSell = pivot.Low;
                            dateSell = pivot.Date;
                            break;
                        }

                        IsSell = true;
                        continue;
                    }

                    if (sig.Close >= (decimal)bb_Sig.Sma.Value && !isAboveMA)
                    {
                        if(i > 2)
                        {
                            priceSell = pivot.Close;
                            dateSell = pivot.Date;
                            break;
                        }

                        IsSell = true;
                        continue;
                    }
                    //              

                    if(!isAboveMA)
                    {
                        //if (Len_Pivot > 0.15m && pivot.Close > pivot.Open) // pivot đỏ hoặc Doji 
                        //    continue;
                        if (!ma1_3_Signal
                            && pivot.Close > pivot.Open)
                            continue;

                        if (!ma1_3)
                            continue;

                        if (i > 2)
                        {
                            priceSell = pivot.Close;
                            dateSell = pivot.Date;
                            break;
                        }

                        IsSell = true;
                        continue;
                    }
                    else
                    {
                        if(Len_Pivot > 0.8m && pivot.Close > pivot.Open)
                        {
                            continue;
                        }

                        if (pivot.Volume / sig.Volume > (1 - GiamToiThieu))
                            continue;

                        if (sig.Volume < 1.2m * (decimal)maVolSignal.Sma.Value)//Vol Sig lớn hơn 1.2 lần MaVol
                            continue;

                        if (isAboveUpper 
                            || upper1_3)
                        {
                            //Console.WriteLine($"{tmp++}| {Math.Round(sig.Volume / (decimal)maVolSignal.Sma.Value, 1)}");
                            if (i > 2)
                            {
                                priceSell = pivot.Close;
                                dateSell = pivot.Date;
                                break;
                            }

                            IsSell = true;
                            continue;
                        }
                    }
                }
                
                var rateSignal = Math.Round(100 * (-1 + priceSell / param.entry), 1);
                var result = new clsResult
                {
                    s = param.s,
                    date = param.date,
                    dateSell = dateSell,
                    T3 = rateT3,
                    T5 = rateT5,
                    T10 = rateT10,
                    Signal = rateSignal,
                    IsEnd = IsEnd
                };
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.TakeProfit|EXCEPTION| {ex.Message}");
            }
            return null;
        }
        int tmp = 0;
        public async Task CheckAllDay_OnlyVolume()
        {
            try
            {
                decimal SL_RATE = 10m;//1.5,1.6,1.8,1.9,2
                int hour = 10;//1h,2h,3h,4h

                var lMesAll = new List<string>();
                var lModel = new List<LongMa20>();

                var winTotal = 0;
                var lossTotal = 0;

                var lsym = _symbolRepo.GetAll();
                foreach (var item in lsym.Select(x => x.s))
                {
                    var winCount = 0;
                    var lossCount = 0;
                    try
                    {
                        var lMes = new List<string>();

                        var lData = await _apiService.SSI_GetDataStock(item);
                        var lbbGlobal = lData.GetBollingerBands();
                        Thread.Sleep(200);
                        if (lData == null || !lData.Any() || lData.Count() < 250 || lData.Last().Volume < 50000)
                            continue;

                        var i = 30;
                        do
                        {
                            var lData15m = lData.Take(i++).ToList();
                            var lbb = lData15m.GetBollingerBands();
                            var lrsi = lData15m.GetRsi();
                            var lMaVol = lData15m.Select(x => new Quote
                            {
                                Date = x.Date,
                                Close = x.Volume
                            }).GetSma(20);

                            var entity_Pivot = lData15m.Last();
                            var bb_Pivot = lbb.Last();

                            var entity_Sig = lData15m.SkipLast(1).Last();
                            var maVol_Sig = lMaVol.SkipLast(1).Last();
                            var bb_Sig = lbb.SkipLast(1).Last();
                            var UpOrLow_Pivot = entity_Pivot.Close > (decimal)bb_Pivot.Sma.Value ? (decimal)bb_Pivot.UpperBand.Value : (decimal)bb_Pivot.LowerBand.Value;

                            var near2 = lData15m.SkipLast(2).Last();
                            var near3 = lData15m.SkipLast(3).Last();
                            var near4 = lData15m.SkipLast(4).Last();
                            var near5 = lData15m.SkipLast(5).Last();

                            var bb2 = lbb.SkipLast(2).Last();
                            var bb3 = lbb.SkipLast(3).Last();
                            var bb4 = lbb.SkipLast(4).Last();
                            var bb5 = lbb.SkipLast(5).Last();

                            var rateVol = Math.Round(entity_Pivot.Volume / entity_Sig.Volume, 2);
                            var rateMaVol = Math.Round(entity_Sig.Volume / (decimal)maVol_Sig.Sma.Value,2);
                            var rateNear = Math.Round(entity_Sig.Volume / near2.Volume,2);
                            var ma20_Sig = (decimal)bb_Sig.Sma.Value;
                            var upper_Sig = (decimal)bb_Sig.UpperBand.Value;
                            var lower_Sig = (decimal)bb_Sig.LowerBand.Value;
                            var close_Sig = entity_Sig.Close;
                            var UpOrLow_Sig = close_Sig > ma20_Sig ? upper_Sig : lower_Sig;
                            var mes = close_Sig > ma20_Sig ? "SELL" : "BUY";
                            var pos_Sig = Math.Abs((close_Sig - ma20_Sig) / (close_Sig - UpOrLow_Sig));
                            var pos_Pivot = Math.Abs((entity_Pivot.Close - (decimal)bb_Pivot.Sma.Value) / (entity_Pivot.Close - UpOrLow_Pivot));

                            if(rateVol <= 0.6m 
                                && (rateMaVol >= 1.5m || rateNear >= 2)
                                && pos_Sig >= 2
                                && pos_Pivot >= 2)
                            {
                                if(entity_Sig.Date.Day == 27)
                                {
                                    var tmp = 1;
                                }

                                if(close_Sig > ma20_Sig)
                                {
                                    //Console.WriteLine($"{mes}|({entity_Sig.Date.ToString("dd/MM/yyyy")}): {item}");
                                }
                                else
                                {
                                    var lCheck = lData.Where(x => x.Date > entity_Sig.Date).Take(5);
                                    foreach (var itemCheck in lCheck)
                                    {
                                        if (itemCheck.Date.Day == 22)
                                        {
                                            var tmp = 1;
                                        }
                                        //var bbCheck = lbb.First(x => x.Date == itemCheck.Date);
                                        //var pos_Check = Math.Abs((itemCheck.Close - (decimal)bbCheck.Sma.Value) / (itemCheck.Close - (decimal)bbCheck.LowerBand));
                                        //if (pos_Check < 1)
                                        //    continue;
                                        //if (itemCheck.Close > (decimal)bbCheck.Sma.Value)
                                        //    break;

                                        var isPinbar = (Math.Min(itemCheck.Open, itemCheck.Close) - itemCheck.Low) >= 3 * (itemCheck.High - Math.Min(itemCheck.Open, itemCheck.Close));
                                        if(isPinbar
                                            || itemCheck.Close >= itemCheck.Open)
                                        {
                                            if(itemCheck.Low < entity_Sig.Low
                                                || itemCheck.High > entity_Sig.High)
                                            {
                                                Console.WriteLine($"{mes}|({itemCheck.Date.ToString("dd/MM/yyyy")}): {item}");
                                                break;
                                            }
                                        }   
                                    }
                                }
                            }
                            continue;
                            var sig_lower = Math.Abs(entity_Sig.Close - (decimal)bb_Sig.LowerBand.Value);
                            var sig_ma = Math.Abs(entity_Sig.Close - (decimal)bb_Sig.Sma.Value);
                            var sig_upper = Math.Abs(entity_Sig.Close - (decimal)bb_Sig.UpperBand.Value);
                            var min = Math.Min(Math.Min(sig_lower, sig_ma), sig_upper);

                            if (min == sig_lower)
                            {
                                var maxPivot = Math.Max(entity_Pivot.Open, entity_Pivot.Close);
                                var compareMa20Vol = Math.Round(entity_Sig.Volume / (decimal)maVol_Sig.Sma.Value, 2);

                                if (maxPivot < Math.Max(entity_Sig.Open, entity_Sig.Close)
                                    && ((decimal)bb_Pivot.Sma.Value - maxPivot) > (maxPivot - (decimal)bb_Pivot.LowerBand.Value))
                                {
                                    //good 
                                    Console.WriteLine($"LONG_BB({entity_Pivot.Date.ToString("dd/MM/yyyy")} - {rateVol}% - {compareMa20Vol}%): {item}");
                                }
                            }
                            //else if (min == sig_upper)
                            //{
                            //    Console.WriteLine($"SHORT_BB({entity_Pivot.Date.ToString("dd/MM/yyyy")} - {rateVol}%): {item}");
                            //}
                            //else
                            //{
                            //    if (entity_Sig.Close < (decimal)bb_Sig.Sma.Value)
                            //    {
                            //        if (entity_Pivot.Close <= (decimal)bb_Pivot.Sma.Value
                            //            && near2.Close < (decimal)bb2.Sma.Value
                            //            && near3.Close < (decimal)bb3.Sma.Value
                            //            )
                            //        {
                            //            Console.WriteLine($"SHORT_MA({entity_Pivot.Date.ToString("dd/MM/yyyy")} - {rateVol}%): {item}");
                            //        }
                            //    }
                            //    else
                            //    {
                            //        if (entity_Pivot.Close >= (decimal)bb_Pivot.Sma.Value
                            //            && near2.Close > (decimal)bb2.Sma.Value
                            //            && near3.Close > (decimal)bb3.Sma.Value
                            //            )
                            //        {
                            //            Console.WriteLine($"LONG_MA({entity_Pivot.Date.ToString("dd/MM/yyyy")} - {rateVol}%): {item}");
                            //        }
                            //    }
                            //}
                        }
                        while (i < lData.Count - 1);
                        //break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
            }
        }

        public async Task CheckGDNN()
        {
            try
            {
                var dt = DateTime.Now;
                var dtPrev = dt.AddYears(-1);
                var lTotal = new List<decimal>();
                var min = 200000000;
                var lsym = _symbolRepo.GetAll();
                foreach (var sym in lsym.Select(x => x.s))
                {
                    if (sym == "VNINDEX")
                        continue;

                    var dat = await _apiService.SSI_GetStockInfo(sym, dtPrev, dt);
                    Thread.Sleep(500);
                    var lData = await _apiService.SSI_GetDataStock(sym);
                    var lbb = lData.GetBollingerBands();
                    var count = dat.data.Count;
                    var lDat = dat.data;
                    lDat.Reverse();
                    QuoteEx itemBuy = null, itemSell = null;

                    for (int i = 5; i < count; i++)
                    {
                        try
                        {
                            var prev_2 = dat.data[i - 3];
                            var prev_1 = dat.data[i - 2];
                            var prev_0 = dat.data[i - 1];

                            var item = dat.data[i];
                            var curDate = item.tradingDate.ToDateTime("dd/MM/yyyy");

                            if (itemBuy is null//chi them dk nay kq khac han
                                && prev_2.netBuySellVal <= min
                                && prev_1.netBuySellVal <= min
                                && prev_0.netBuySellVal <= min
                                && item.netBuySellVal > min)
                            {
                                var itemData = lData.First(x => x.Date.Year == curDate.Year && x.Date.Month == curDate.Month && x.Date.Day == curDate.Day);
                                var bb = lbb.First(x => x.Date == itemData.Date);
                                if (Math.Max(itemData.Open, itemData.Close) >= (decimal)bb.UpperBand.Value)
                                    continue;
                                if (itemData.High > (decimal)bb.Sma.Value
                                    && itemData.Close < itemData.Open
                                    && itemData.Close < (decimal)bb.Sma.Value)
                                    continue;

                                itemBuy = new QuoteEx
                                {
                                    Close = item.close,
                                    Date = curDate,
                                    Index = i
                                };
                                itemSell = null;
                                continue;
                            }
                            if (itemBuy != null
                                && prev_2.netBuySellVal >= -min
                                && prev_1.netBuySellVal >= -min
                                && prev_0.netBuySellVal >= -min
                                && item.netBuySellVal < -min
                                && (i - itemBuy.Index) > 3)
                            {
                                itemSell = new QuoteEx
                                {
                                    Close = item.close,
                                    Date = curDate,
                                    Index = i
                                };
                                var rate = Math.Round(100 * (-1 + itemSell.Close / itemBuy.Close), 1);
                                lTotal.Add(rate);
                                var mes = $"{sym}|BUY({itemBuy.Date.ToString("dd/MM/yyyy")})|SELL({itemSell.Date.ToString("dd/MM/yyyy")})| Rate: {rate}%";
                                Console.WriteLine(mes);
                                itemBuy = null;
                                itemSell = null;
                            }
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine($"{ex.Message}");
                        }
                    }
                }

                Console.WriteLine($"Total: {lTotal.Sum()}%");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }

        public async Task CheckCungCau()
        {
            try
            {
                var dt = DateTime.Now;
                var from = dt.AddYears(-1);
                var lTotal = new List<decimal>();
                var min = 200000000;
                var lsym = _symbolRepo.GetAll();
                foreach (var sym in lsym.Select(x => x.s))
                {
                    try
                    {
                        if (sym == "VNINDEX")
                            continue;

                        //if (sym != "PVT")
                        //    continue;

                        var dat = await _apiService.SSI_GetStockInfo(sym, from, dt);
                        dat.data.Reverse();
                        var count = dat.data.Count;
                        var avgNet = dat.data.Sum(x => Math.Abs(x.netBuySellVal ?? 0));
                        var countNet = dat.data.Count(x => x.netBuySellVal != null && x.netBuySellVal != 0);
                        var avg = avgNet / countNet;
                        //Console.WriteLine($"AVG: {avg}");
                        var lData = await _apiService.SSI_GetDataStock(sym);
                        var lbb = lData.GetBollingerBands();
                        for (int i = 1; i < count - 1; i++)
                        {
                            try
                            {
                                var prev = dat.data[i - 1];
                                var sig = dat.data[i];
                                var pivot_1 = dat.data[i + 1];
                                if (Math.Abs(sig.netBuySellVal ?? 0) < avg)
                                    continue;

                                var ratePrev = Math.Round(-1 + Math.Abs((sig.netBuySellVal ?? 0) / (prev.netBuySellVal ?? 1)), 1);
                                var ratePivot = Math.Round(-1 + Math.Abs((sig.netBuySellVal ?? 0) / (pivot_1.netBuySellVal ?? 1)), 1);
                                if (Math.Abs(ratePrev) > 1.5 && Math.Abs(ratePivot) > 1.5)
                                {
                                    if (Math.Round((decimal)(prev.netBuySellVal ?? 0) / 1000000, 1) == 0
                                        || Math.Round((decimal)(pivot_1.netBuySellVal ?? 0) / 1000000000, 1) == 0)
                                        continue;

                                    var valid = false;
                                    if (sig.netBuySellVal > 0 || pivot_1.netBuySellVal > 0)
                                    {
                                        valid = true;
                                    }

                                    SSI_DataStockInfoDetailResponse pivot_2 = null;
                                    if (!valid)
                                    {
                                        if ((i + 2) < count - 1)
                                        {
                                            pivot_2 = dat.data[i + 2];
                                            if (pivot_2.netBuySellVal > 0)
                                                valid = true;
                                        }
                                    }

                                    if (valid)
                                    {
                                        var dtPrev = prev.tradingDate.ToDateTime("dd/MM/yyyy");
                                        var dtSig = sig.tradingDate.ToDateTime("dd/MM/yyyy");
                                        var dtPivot = pivot_1.tradingDate.ToDateTime("dd/MM/yyyy");
                                        //if(dtPivot.Month == 4 && dtPivot.Day == 14)
                                        //{
                                        //    var tmp = 1;
                                        //}

                                        var entityPrev = lData.First(x => x.Date.Day == dtPrev.Day && x.Date.Month == dtPrev.Month && x.Date.Year == dtPrev.Year);
                                        var entitySignal = lData.First(x => x.Date.Day == dtSig.Day && x.Date.Month == dtSig.Month && x.Date.Year == dtSig.Year);
                                        var entityPivot = lData.First(x => x.Date.Day == dtPivot.Day && x.Date.Month == dtPivot.Month && x.Date.Year == dtPivot.Year);
                                        var bb_Prev = lbb.First(x => x.Date == entityPrev.Date);
                                        var bb_Signal = lbb.First(x => x.Date == entitySignal.Date);
                                        var bb_Pivot = lbb.First(x => x.Date == entityPivot.Date);
                                        if (entitySignal.Low < (decimal)bb_Signal.LowerBand.Value)
                                        {
                                            if (entitySignal.Low < entityPivot.Low)
                                                continue;
                                        }
                                        else if(sig.netBuySellVal < 0 
                                            && entityPivot.Close > (decimal)bb_Pivot.Sma.Value)
                                        {
                                            var divUp = (decimal)bb_Pivot.UpperBand.Value - entityPivot.Close;
                                            var divMA = entityPivot.Close - (decimal)bb_Pivot.Sma.Value;
                                            if (divUp < 2 * divMA)
                                                continue;
                                        }

                                        if (entitySignal.High > (decimal)bb_Signal.UpperBand.Value)
                                        {
                                            //SELL
                                            //Console.WriteLine($"{sym}|SELL| {pivot_1.tradingDate}|NN: {Math.Round((decimal)(pivot_1.netBuySellVal ?? 0) / 1000000000, 1)}");
                                        }
                                        else if (sig.netBuySellVal < 0)
                                        {
                                            if (Math.Max(entitySignal.Open, entitySignal.Close) > (decimal)bb_Signal.Sma.Value
                                                && entityPivot.Close < (decimal)bb_Pivot.Sma.Value)
                                                continue;

                                            //BUY
                                            if (pivot_2 != null)
                                            {
                                                var entityShow = lData.Skip(1).First(x => x.Date > entityPivot.Date);
                                                Console.WriteLine($"{sym}|BUY| {entityShow.Date.ToString("dd/MM/yyyy")}|NN: {Math.Round((decimal)(pivot_2.netBuySellVal ?? 0) / 1000000000, 1)}");
                                            }
                                            else
                                            {
                                                var entityShow = lData.First(x => x.Date > entityPivot.Date);
                                                Console.WriteLine($"{sym}|BUY| {entityShow.Date.ToString("dd/MM/yyyy")}|NN: {Math.Round((decimal)(pivot_1.netBuySellVal ?? 0) / 1000000000, 1)}");
                                            }
                                        }
                                        else
                                        {
                                            //SELL
                                            //Console.WriteLine($"{sym}|SELL| {pivot_1.tradingDate}|NN: {Math.Round((decimal)(pivot_1.netBuySellVal ?? 0) / 1000000000, 1)}");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"{ex.Message}");
                    }
                    
                }

                Console.WriteLine($"Total: {lTotal.Sum()}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }

        public async Task CheckCrossMa50_BB()
        {
            try
            {
                decimal SL_RATE = 10m;//1.5,1.6,1.8,1.9,2
                int hour = 10;//1h,2h,3h,4h

                var lMesAll = new List<string>();

                var winTotal = 0;
                var lossTotal = 0;
                var lPoint = new List<clsPoint>();
                var lTrace = new List<clsTrace>();
                var lResult = new List<clsResult>();
                var lsym = _symbolRepo.GetAll();
                foreach (var item in lsym.Select(x => x.s))
                {
                    try
                    {
                        //if (item != "AAA")
                        //    continue;

                        var lMes = new List<string>();
                        var lData = await _apiService.SSI_GetDataStock(item);
                        var lbb_Total = lData.GetBollingerBands();
                        var lMaVol_Total = lData.Select(x => new Quote
                        {
                            Date = x.Date,
                            Close = x.Volume
                        }).GetSma(20);
                        Thread.Sleep(200);
                        if (lData == null || !lData.Any() || lData.Count() < 250 || lData.Last().Volume < 10000)
                            continue;
                        var count = lData.Count();
                        var take = 250;
                        do
                        {
                            var lData15m = lData.Take(take++);

                            var lbb = lData15m.GetBollingerBands();
                            var lrsi = lData15m.GetRsi();
                            var lMaVol = lData15m.Select(x => new Quote
                            {
                                Date = x.Date,
                                Close = x.Volume
                            }).GetSma(20);
                            var lMa50 = lData15m.GetSma(50);

                            var entity_Pivot = lData15m.Last();
                            var bb_Pivot = lbb.First(x => x.Date == entity_Pivot.Date);
                            var ma50_Pivot = lMa50.First(x => x.Date == entity_Pivot.Date);

                            var entity_Sig = lData15m.SkipLast(1).Last();
                            var bb_Sig = lbb.First(x => x.Date == entity_Sig.Date);
                            var ma50_Sig = lMa50.First(x => x.Date == entity_Sig.Date);

                            if (ma50_Sig.Sma.Value >= bb_Sig.LowerBand.Value
                                && ma50_Pivot.Sma.Value < bb_Pivot.LowerBand.Value)
                            {
                                var ma50_Check = lMa50.First(x => x.Date == entity_Pivot.Date.AddDays(-20));
                                var goc = ma50_Pivot.Sma.Value.GetAngle(ma50_Check.Sma.Value, 20);
                                if(goc > 0)
                                {
                                    Console.WriteLine($"{item}: {entity_Pivot.Date.ToString("dd/MM/yyyy")}|Goc: {goc}");
                                }
                            }
                               


                            ////if(item == "BFC" && last.Date.Year == 2025 && last.Date.Month == 4 && last.Date.Day == 9)
                            ////{
                            ////    var zz = 1;
                            ////}    

                            //if (cur.Low <= last.Low && cur.High >= last.High)
                            //    continue;

                            //var volCheck = last.Volume / cur.Volume;
                            //if (volCheck < 1.2m)
                            //    continue;

                            //var isPinbar = ((Math.Min(last.Open, last.Close) - last.Low) >= 3 * (last.High - Math.Min(last.Open, last.Close)))
                            //                && (Math.Abs(last.Open - last.Close) >= 0.1m * (last.High - last.Low));
                            //if (!isPinbar
                            //    && last.Open > last.Close)
                            //    continue;

                            //if (isPinbar
                            //    && Math.Min(last.Open, last.Close) >= Math.Min(cur.Open, cur.Close))
                            //    continue;

                            //if (last.Close > last.Open
                            //    && last.High <= cur.High
                            //    && last.Low >= cur.Low)
                            //    continue;

                            //var posCheck_Cur = (cur.Close - (decimal)bb_Cur.LowerBand.Value) > 0 && Math.Abs((decimal)bb_Cur.Sma.Value - cur.Close) < 3 * Math.Abs(cur.Close - (decimal)bb_Cur.LowerBand.Value);
                            //if (posCheck_Cur)
                            //    continue;

                            //var posCheck_Last = (last.Close - (decimal)bb_Last.LowerBand.Value) > 0 && Math.Abs((decimal)bb_Last.Sma.Value - last.Close) < 2 * Math.Abs(last.Close - (decimal)bb_Last.LowerBand.Value);
                            //if (posCheck_Last)
                            //    continue;

                            //var isSignal = false;
                            //var lSignal = lData15m.SkipLast(1).TakeLast(6);
                            //var countSignal = lSignal.Count();
                            //for (int i = 0; i < countSignal - 2; i++)
                            //{
                            //    var curSignal = lSignal.ElementAt(i);
                            //    var curPivot = lSignal.ElementAt(i + 1);
                            //    if (curPivot.Volume / curSignal.Volume <= 0.6m)
                            //    {
                            //        isSignal = true;
                            //        break;
                            //    }
                            //}

                            ////Console.WriteLine($"{item}|{last.Date.ToString("dd/MM/yyyy")}");
                            //if (last.Date.Year == 2025 && last.Date.Month == 4)
                            //    continue;
                            //var model = new clsTrace
                            //{
                            //    s = item,
                            //    date = last.Date,
                            //    entry = last.Close,
                            //    isSignal = isSignal,
                            //    isCrossMa20Vol = (decimal)lMaVol.First(x => x.Date == last.Date).Sma.Value * 0.9m <= last.Volume
                            //};

                            //if (!model.isCrossMa20Vol)
                            //    continue;


                            //lTrace.Add(model);

                            //var tp = TakeProfit(model, lData, lbb_Total, lMaVol_Total);
                            //if (tp != null)
                            //{
                            //    lResult.Add(tp);
                            //}
                        }
                        while (take <= count);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                }

                foreach (var item in lResult.OrderBy(x => x.s))
                {
                    Console.WriteLine($"{item.s}|BUY: {item.date.ToString("dd/MM/yyyy")}|SELL: {item.dateSell.ToString("dd/MM/yyyy")}|Rate: {item.Signal}%");
                }

                var sumT3 = lResult.Sum(x => x.T3);
                var sumT5 = lResult.Sum(x => x.T5);
                var sumT10 = lResult.Sum(x => x.T10);
                var sumSignal = lResult.Sum(x => x.Signal);
                Console.WriteLine($"Total({lResult.Count()})| T3({lResult.Count(x => x.T3 > 0)}): {sumT3}%| T5({lResult.Count(x => x.T5 > 0)}): {sumT5}%| T10({lResult.Count(x => x.T10 > 0)}): {sumT10}%| Signal({lResult.Count(x => x.Signal > 0)}): {sumSignal}%|End: {lResult.Count(x => x.IsEnd)}");

                var tmp = 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
            }
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

    public class clsPoint
    {
        public string s { get; set; }
        public string mes { get; set; }
        public float TotalPoint { get; set; }
    }

    public class clsTrace
    {
        public string s { get; set; }
        public DateTime date { get; set; }
        public decimal entry { get; set; }
        public bool isSignal { get; set; }
        public bool isCrossMa20Vol { get; set; }
    }

    public class clsResult
    {
        public string s { get; set; }
        public DateTime date { get; set; }
        public DateTime dateSell { get; set; }
        public decimal T3 { get; set; }
        public decimal T5 { get; set; }
        public decimal T10 { get; set; }
        public decimal Signal { get; set; }
        public bool IsEnd { get; set; }
    }

    //"VNINDEX",
    //"DC4",
    //"GIL",
    //"GVR",
    //"DPG",
    //"CTG",
    //"BFC",
    //"VRE",
    //"PVB",
    //"GEX",
    //"SZC",
    //"HDG",
    //"BMP",
    //"TLG",
    //"VPB",
    //"DIG",
    //"KBC",
    //"HSG",
    //"PET",
    //"TNG",
    //"SBT",
    //"MSH",
    //"NAB",
    //"VGC",
    //"CSV",
    //"VCS",
    //"CSM",
    //"PHR",
    //"PVT",
    //"PC1",
    //"ASM",
    //"LAS",
    //"DXG",
    //"HCM",
    //"CTI",
    //"NHA",
    //"DPR",
    //"ANV",
    //"OCB",
    //"TVB",
    //"STB",
    //"HDC",
    //"POW",
    //"VSC",
    //"L18",
    //"DDV",
    //"VCI",
    //"GMD",
    //"NTP",
    //"KSV",
    //"NT2",
    //"TCM",
    //"LSS",
    //"GEG",
    //"HHS",
    //"MSB",
    //"TCH",
    //"VHC",
    //"PVD",
    //"FOX",
    //"SSI",
    //"NKG",
    //"BSI",
    //"ACB",
    //"REE",
    //"VHM",
    //"PAN",
    //"SIP",
    //"PTB",
    //"BSR",
    //"BID",
    //"PVS",
    //"CTS",
    //"FTS",
    //"HPG",
    //"DBC",
    //"MSR",
    //"THG",
    //"CTD",
    //"VOS",
    //"FMC",
    //"PHP",
    //"GAS",
    //"DCM",
    //"KSB",
    //"MSN",
    //"BVB",
    //"MBB",
    //"TRC",
    //"VPI",
    //"EIB",
    //"KDH",
    //"VCB",
    //"FPT",
    //"DRC",
    //"CMG",
    //"HAG",
    //"SHB",
    //"CII",
    //"CTR",
    //"IDC",
    //"GEE",
    //"NVB",
    //"BVS",
    //"BWE",
    //"HAX",
    //"QNS",
    //"VEA",
    //"TVS",
    //"DGC",
    //"HAH",
    //"NVL",
    //"PAC",
    //"AAA",
    //"TNH",
    //"ACV",
    //"BCC",
    //"FRT",
    //"HT1",
    //"SCS",
    //"TLH",
    //"MIG",
    //"SKG",
    //"VAB",
    //"NLG",
    //"HVN",
    //"HNG",
    //"PDR",
    //"VDS",
    //"SJE",
    //"PNJ",
    //"CEO",
    //"YEG",
    //"KLB",
    //"BCM",
    //"BVH",
    //"NTL",
    //"TDH",
    //"MBS",
    //"HUT",
    //"VIB",
    //"BAF",
    //"HHV",
    //"NDN",
    //"SGP",
    //"MCH",
    //"FCN",
    //"SCR",
    //"TCB",
    //"LPB",
    //"VTP",
    //"AGR",
    //"VCG",
    //"DPM",
    //"IDJ",
    //"DXS",
    //"OIL",
    //"AGG",
    //"VND",
    //"PSI",
    //"DHA",
    //"VIC",
    //"BCG",
    //"TPB",
    //"VIX",
    //"IJC",
    //"DGW",
    //"SBS",
    //"MFS",
    //"PLX",
    //"DRI",
    //"EVF",
    //"ORS",
    //"SAB",
    //"TDC",
    //"VNM",
    //"TV2",
    //"C4G",
    //"MWG",
    //"JVC",
    //"GDA",
    //"VGI",
    //"DSC",
    //"SMC",
    //"DTD",
    //"QCG",
}
