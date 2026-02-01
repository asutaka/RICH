using MongoDB.Driver;
using SharpCompress.Common;
using Skender.Stock.Indicators;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Model;
using StockPr.Service;
using StockPr.Utils;

namespace StockPr.Research
{
    public interface IBacktestService
    {
        Task CheckAllDay_OnlyVolume();
        Task BatDayCK();
        Task CheckGDNN();
        Task CheckCungCau();
        Task CheckCrossMa50_BB();
        Task CheckWycKoff();
        Task AnalyzeIndicators(); // ✨ New method
        Task BacktestBBEntryStrategy(); // ✨ New: Backtest BB Entry Strategy
        Task BacktestBBEntryStrategy_Advanced(); // ✨ Advanced with filters
        Task BacktestOptimalStrategy(); // ✨ Optimal: BB + Group only
        Task BackTest22122025();
    }
    
    public class BacktestService : IBacktestService
    {
        private readonly ILogger<BacktestService> _logger;
        private readonly IMarketDataService _marketDataService;
        private readonly ISymbolRepo _symbolRepo;
        private readonly IPhanLoaiNDTRepo _phanLoaiNDTRepo;
        private readonly IPreEntryRepo _preEntryRepo;

        public BacktestService(ILogger<BacktestService> logger, IMarketDataService marketDataService, ISymbolRepo symbolRepo, IPhanLoaiNDTRepo phanLoaiNDTRepo, IPreEntryRepo preEntryRepo)
        {
            _logger = logger;
            _marketDataService = marketDataService;
            _symbolRepo = symbolRepo;
            _phanLoaiNDTRepo = phanLoaiNDTRepo;
            _preEntryRepo = preEntryRepo;
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
                        var lData = await _marketDataService.SSI_GetDataStock(item);
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

                        var lData = await _marketDataService.SSI_GetDataStock(item);
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

                    var dat = await _marketDataService.SSI_GetStockInfo(sym, dtPrev, dt);
                    Thread.Sleep(500);
                    var lData = await _marketDataService.SSI_GetDataStock(sym);
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

                        var dat = await _marketDataService.SSI_GetStockInfo(sym, from, dt);
                        dat.data.Reverse();
                        var count = dat.data.Count;
                        var avgNet = dat.data.Sum(x => Math.Abs(x.netBuySellVal ?? 0));
                        var countNet = dat.data.Count(x => x.netBuySellVal != null && x.netBuySellVal != 0);
                        var avg = avgNet / countNet;
                        //Console.WriteLine($"AVG: {avg}");
                        var lData = await _marketDataService.SSI_GetDataStock(sym);
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
                        var lData = await _marketDataService.SSI_GetDataStock(item);
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

                            //if (entity_Pivot.Date.Year < 2025
                            //    || entity_Pivot.Date.Month < 4)
                            //    continue;

                            if (ma50_Sig.Sma.Value >= bb_Sig.LowerBand.Value
                                && ma50_Pivot.Sma.Value < bb_Pivot.LowerBand.Value)
                            {
                                var ma50_Check = lMa50.Where(x => x.Date < entity_Pivot.Date).SkipLast(9).Last();
                                if (ma50_Check is null)
                                    continue;

                                var goc = ma50_Pivot.Sma.Value.GetAngle(ma50_Check.Sma.Value, 10);
                                if(goc >= 25)
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

        public async Task CheckWycKoff()
        {
            try
            {
                var lsym = _symbolRepo.GetAll();
                foreach (var item in lsym.Select(x => x.s))
                {
                    try
                    {
                        //if (item != "BID")
                        //    continue;

                        var lMes = new List<string>();
                        var lData = await _marketDataService.SSI_GetDataStock(item);
                        var res = lData.IsWyckoff();
                        if(res.Item1)
                        {
                            foreach (var itemWyc in res.Item2)
                            {
                                Console.WriteLine($"{item}: {itemWyc.Date.ToString("dd/MM/yyyy")}");
                            }
                        }    
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                }

                Console.WriteLine("END");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
            }
        }
        /// <summary>
        /// Phân tích các chỉ báo kỹ thuật cho tất cả mã chứng khoán
        /// Tính: Bollinger Bands, RSI, MA9(RSI), WMA45(RSI)
        /// </summary>
        public async Task AnalyzeIndicators()
        {
            try
            {
                Console.WriteLine("=== BẮT ĐẦU PHÂN TÍCH CHỈ BÁO ===");
                Console.WriteLine($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                Console.WriteLine();

                //var lStock = StaticVal._lStock;
                var lStock = new List<Stock>{
                    new Stock { s = "VCB" },
                    new Stock { s = "SSI" },
                    new Stock { s = "DIG" },
                    new Stock { s = "DPG" },
                    new Stock { s = "GEX" },
                    new Stock { s = "VIX" },
                    new Stock { s = "VND" },
                    new Stock { s = "DXG" },
                    new Stock { s = "NLG" },
                    new Stock { s = "DBC" },
                    new Stock { s = "SHB" },
                };
                if (lStock == null || !lStock.Any())
                {
                    Console.WriteLine("Không có mã chứng khoán trong StaticVal._lStock");
                    return;
                }

                Console.WriteLine($"Tổng số mã: {lStock.Count}");
                Console.WriteLine();

                int processedCount = 0;
                int errorCount = 0;

                foreach (var stock in lStock)
                {
                    try
                    {
                        var symbol = stock.s;

                        // Lấy dữ liệu từ API
                        var lData = await _marketDataService.SSI_GetDataStock(symbol);

                        if (lData == null || !lData.Any() || lData.Count() < 50)
                        {
                            Console.WriteLine($"{symbol}: Không đủ dữ liệu (cần ít nhất 50 nến)");
                            errorCount++;
                            continue;
                        }

                        // 1. Tính Bollinger Bands (period 20, stdDev 2)
                        var bollingerBands = lData.GetBollingerBands(20, 2);

                        // 2. Tính RSI (period 14)
                        var rsi = lData.GetRsi(14);

                        // 3. Tính MA9 từ RSI
                        var rsiQuotes = rsi
                            .Where(x => x.Rsi.HasValue)
                            .Select(x => new Quote
                            {
                                Date = x.Date,
                                Close = (decimal)x.Rsi.Value
                            })
                            .ToList();

                        var ma9FromRsi = rsiQuotes.GetSma(9);

                        // 4. Tính WMA45 từ RSI
                        var wma45FromRsi = rsiQuotes.GetWma(45);

                        // Lấy giá trị mới nhất
                        var latestData = lData.Last();
                        var latestBB = bollingerBands.Last();
                        var latestRSI = rsi.Last();
                        var latestMA9 = ma9FromRsi.LastOrDefault(x => x.Sma.HasValue);
                        var latestWMA45 = wma45FromRsi.LastOrDefault(x => x.Wma.HasValue);

                        // In kết quả
                        Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                        Console.WriteLine($"📊 {symbol} - {latestData.Date:dd/MM/yyyy}");
                        Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                        Console.WriteLine($"Giá: {latestData.Close:N0} đồng");
                        Console.WriteLine();

                        Console.WriteLine($"📈 Bollinger Bands:");
                        Console.WriteLine($"   Upper: {latestBB.UpperBand:N2}");
                        Console.WriteLine($"   Middle: {latestBB.Sma:N2}");
                        Console.WriteLine($"   Lower: {latestBB.LowerBand:N2}");
                        Console.WriteLine($"   %B: {latestBB.PercentB:N2}");
                        Console.WriteLine($"   Width: {latestBB.Width:N2}");
                        Console.WriteLine();

                        Console.WriteLine($"📊 RSI(14): {latestRSI.Rsi:N2}");

                        if (latestMA9 != null && latestMA9.Sma.HasValue)
                        {
                            Console.WriteLine($"📉 MA9(RSI): {latestMA9.Sma.Value:N2}");
                        }
                        else
                        {
                            Console.WriteLine($"📉 MA9(RSI): Chưa đủ dữ liệu");
                        }

                        if (latestWMA45 != null && latestWMA45.Wma.HasValue)
                        {
                            Console.WriteLine($"📈 WMA45(RSI): {latestWMA45.Wma.Value:N2}");
                        }
                        else
                        {
                            Console.WriteLine($"📈 WMA45(RSI): Chưa đủ dữ liệu");
                        }
                        
                        // 4. Phân tích Wyckoff (Điểm mua an toàn)
                        try
                        {
                            var wyckoffResult = lData.IsWyckoff();
                            if (wyckoffResult.Item1)
                            {
                                var wyckoffPoint = wyckoffResult.Item2.Last();
                                Console.WriteLine();
                                Console.WriteLine($"🎯 WYCKOFF: Phát hiện điểm mua an toàn!");
                                Console.WriteLine($"   Ngày: {wyckoffPoint.Date:dd/MM/yyyy}");
                                Console.WriteLine($"   Giá: {wyckoffPoint.Close:N0}");
                                Console.WriteLine($"   Volume: {wyckoffPoint.Volume:N0}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error analyzing Wyckoff for {symbol}");
                        }
                        
                        Console.WriteLine();
                        
                        // 5. Phân tích Foreign & Group từ PhanLoaiNDT
                        try
                        {
                            var phanLoaiData = _phanLoaiNDTRepo.GetEntityByFilter(
                                MongoDB.Driver.Builders<DAL.Entity.PhanLoaiNDT>.Filter.Eq(x => x.s, symbol));
                            
                            if (phanLoaiData != null && phanLoaiData.Date != null && phanLoaiData.Date.Any())
                            {
                                // Lấy dữ liệu gần đây
                                var count = phanLoaiData.Date.Count;
                                var recentDays = Math.Min(20, count); // 20 ngày gần nhất
                                var startIdx = Math.Max(0, count - recentDays);
                                
                                var recentForeign = phanLoaiData.Foreign?.Skip(startIdx).ToList() ?? new List<double>();
                                var recentGroup = phanLoaiData.Group?.Skip(startIdx).ToList() ?? new List<double>();
                                var recentIndividual = phanLoaiData.Individual?.Skip(startIdx).ToList() ?? new List<double>();
                                
                                if (recentForeign.Any() && recentGroup.Any() && recentIndividual.Any())
                                {
                                    // Tính toán các metrics
                                    var foreignLatest = recentForeign.Last();
                                    var groupLatest = recentGroup.Last();
                                    var individualLatest = recentIndividual.Last();
                                    
                                    var foreign5Days = recentForeign.TakeLast(5).Sum();
                                    var group5Days = recentGroup.TakeLast(5).Sum();
                                    var group10Days = recentGroup.TakeLast(Math.Min(10, recentGroup.Count)).Sum();
                                    var group20Days = recentGroup.Sum();
                                    
                                    // Đếm số ngày Group mua ròng liên tục
                                    int groupBuyStreak = 0;
                                    for (int i = recentGroup.Count - 1; i >= 0; i--)
                                    {
                                        if (recentGroup[i] > 0)
                                            groupBuyStreak++;
                                        else
                                            break;
                                    }
                                    
                                    Console.WriteLine($"💰 Nhà Đầu Tư:");
                                    Console.WriteLine();
                                    
                                    // Foreign
                                    Console.WriteLine($"   🌍 Foreign:");
                                    Console.WriteLine($"      Hôm nay: {foreignLatest:N0} {(foreignLatest > 0 ? "✅ MUA" : "❌ BÁN")}");
                                    Console.WriteLine($"      5 ngày: {foreign5Days:N0} {(foreign5Days > 0 ? "✅" : "❌")}");
                                    
                                    // Group (Tổ chức) - QUAN TRỌNG NHẤT
                                    Console.WriteLine($"   🏢 Group (Tổ chức):");
                                    Console.WriteLine($"      Hôm nay: {groupLatest:N0} {(groupLatest > 0 ? "✅ MUA" : "❌ BÁN")}");
                                    Console.WriteLine($"      5 ngày: {group5Days:N0} {(group5Days > 0 ? "✅" : "❌")}");
                                    Console.WriteLine($"      10 ngày: {group10Days:N0} {(group10Days > 0 ? "✅" : "❌")}");
                                    Console.WriteLine($"      20 ngày: {group20Days:N0} {(group20Days > 0 ? "✅" : "❌")}");
                                    if (groupBuyStreak > 0)
                                        Console.WriteLine($"      🔥 Mua ròng liên tục: {groupBuyStreak} ngày");
                                    
                                    // Individual (Nhỏ lẻ)
                                    Console.WriteLine($"   👥 Individual (Nhỏ lẻ):");
                                    Console.WriteLine($"      Hôm nay: {individualLatest:N0} {(individualLatest > 0 ? "MUA" : "BÁN")}");
                                    
                                    Console.WriteLine();
                                    
                                    // ⚡ PHÂN TÍCH THÔNG MINH DựA trên kinh nghiệm
                                    var signals = new List<string>();
                                    
                                    // 1. Group gom dài hạn (10-20 ngày) → TỐT
                                    if (group10Days > 0 && group20Days > 0 && groupBuyStreak >= 3)
                                    {
                                        signals.Add($"🎯 TÍCH CỰC: Group gom {groupBuyStreak} ngày liên tục ({group20Days:N0})");
                                    }
                                    else if (group10Days > 0 && group20Days > 0)
                                    {
                                        signals.Add($"📈 KHẢ QUAN: Group tích lũy dài hạn ({group20Days:N0})");
                                    }
                                    
                                    // 2. Group bán lớn 1 phiên → CẢNH BÁO
                                    if (groupLatest < 0 && Math.Abs(groupLatest) > Math.Abs(group5Days / 5) * 2)
                                    {
                                        signals.Add($"⚠️ CẢNH BÁO: Group bán lớn hôm nay ({groupLatest:N0})");
                                    }
                                    
                                    // 3. Individual mua lớn 1 phiên → KHÔNG TỐT
                                    var avgIndividual = recentIndividual.TakeLast(5).Average();
                                    if (individualLatest > 0 && individualLatest > avgIndividual * 2)
                                    {
                                        signals.Add($"❌ TIÊU CỰC: Nhỏ lẻ mua lớn bất thường ({individualLatest:N0})");
                                    }
                                    
                                    // 4. Foreign + Group cùng mua → RẤT TỐT
                                    if (foreign5Days > 0 && group5Days > 0)
                                    {
                                        signals.Add($"🌟 XUẤT SẮC: Foreign + Group đều mua ròng");
                                    }
                                    
                                    // 5. Foreign + Group cùng bán → RẤT XẤU
                                    if (foreign5Days < 0 && group5Days < 0)
                                    {
                                        signals.Add($"🔴 NGUY HIỂM: Foreign + Group đều bán ròng");
                                    }
                                    
                                    // In tín hiệu
                                    if (signals.Any())
                                    {
                                        Console.WriteLine($"📊 TÍN HIỆU GIAO DỊCH:");
                                        foreach (var signal in signals)
                                        {
                                            Console.WriteLine($"   {signal}");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"📊 TÍN HIỆU: TRUNG LẬP");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error analyzing investor data for {symbol}");
                        }
                        
                        // 6. Phân tích áp lực mua/bán từ SSI_GetStockInfo_Extend
                        try
                        {
                            var now = DateTime.Now;
                            var from = now.AddDays(-10); // Lấy 10 ngày gần nhất
                            var stockInfo = await _marketDataService.SSI_GetStockInfo_Extend(
symbol, from, now);
                            
                            if (stockInfo?.data != null && stockInfo.data.Count >= 2)
                            {
                                // Lấy 2 phiên gần nhất
                                var latestSession = stockInfo.data.Last();
                                var previousSession = stockInfo.data[stockInfo.data.Count - 2];
                                
                                // Tính tỷ lệ mua/bán
                                var latestRatio = latestSession.totalSellTradeVol > 0 
                                    ? (double)latestSession.totalBuyTradeVol / latestSession.totalSellTradeVol 
                                    : 0;
                                    
                                var previousRatio = previousSession.totalSellTradeVol > 0 
                                    ? (double)previousSession.totalBuyTradeVol / previousSession.totalSellTradeVol 
                                    : 0;
                                
                                // Tính % thay đổi
                                var ratioChange = previousRatio > 0 
                                    ? ((latestRatio - previousRatio) / previousRatio) * 100 
                                    : 0;
                                
                                Console.WriteLine($"📊 Áp Lực Mua/Bán:");
                                Console.WriteLine($"   Hôm nay:");
                                Console.WriteLine($"      KL Mua: {latestSession.totalBuyTradeVol:N0}");
                                Console.WriteLine($"      KL Bán: {latestSession.totalSellTradeVol:N0}");
                                Console.WriteLine($"      KL Khớp: {latestSession.totalMatchVol:N0}");
                                Console.WriteLine($"      Tỷ lệ Mua/Bán: {latestRatio:N2}");
                                
                                Console.WriteLine($"   Phiên trước:");
                                Console.WriteLine($"      Tỷ lệ Mua/Bán: {previousRatio:N2}");
                                Console.WriteLine($"      Thay đổi: {ratioChange:N1}%");
                                
                                Console.WriteLine();
                                
                                // Phân tích tín hiệu
                                if (ratioChange >= 30)
                                {
                                    Console.WriteLine($"🎯 TÍN HIỆU MUA: Áp lực mua tăng mạnh {ratioChange:N1}% (≥30%)");
                                }
                                else if (ratioChange <= -30)
                                {
                                    Console.WriteLine($"⚠️ TÍN HIỆU BÁN: Áp lực bán tăng mạnh {Math.Abs(ratioChange):N1}% (≥30%)");
                                }
                                else if (latestRatio > 1.2)
                                {
                                    Console.WriteLine($"📈 KHẢ QUAN: Áp lực mua > áp lực bán ({latestRatio:N2})");
                                }
                                else if (latestRatio < 0.8)
                                {
                                    Console.WriteLine($"📉 TIÊU CỰC: Áp lực bán > áp lực mua ({latestRatio:N2})");
                                }
                                else
                                {
                                    Console.WriteLine($"📊 TRUNG LẬP: Áp lực cân bằng ({latestRatio:N2})");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error analyzing buy/sell pressure for {symbol}");
                        }

                        Console.WriteLine();

                        processedCount++;

                        // Delay để tránh rate limit
                        await Task.Delay(200);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ {stock.s}: Lỗi - {ex.Message}");
                        _logger.LogError(ex, $"Error analyzing {stock.s}");
                        errorCount++;
                    }
                }

                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("=== KẾT THÚC PHÂN TÍCH ===");
                Console.WriteLine($"✅ Đã xử lý: {processedCount}/{lStock.Count}");
                Console.WriteLine($"❌ Lỗi: {errorCount}");
                Console.WriteLine($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ LỖI NGHIÊM TRỌNG: {ex.Message}");
                _logger.LogError(ex, "AnalyzeIndicators failed");
            }
        }
        /// <summary>
        /// Backtest Bollinger Bands Entry Strategy
        /// Entry: Close trong 1/2 khoảng từ SMA20-Lower HOẶC Close >= SMA20 và trong 1/2 khoảng từ SMA20-Upper
        /// Test multiple Take Profit levels cho ngắn hạn và dài hạn
        /// </summary>
        public async Task BacktestBBEntryStrategy()
        {
            try
            {
                Console.WriteLine("=== BACKTEST BOLLINGER BANDS ENTRY STRATEGY ===");
                Console.WriteLine($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                Console.WriteLine();

                // Danh sách 10 mã test
                var testSymbols = new List<string>
                {
                    "SSI", "DIG", "DPG", "GEX", "VIX",
                    "VND", "DXG", "NLG", "DBC", "SHB"
                };

                // Các mức Take Profit để test
                var shortTermTPs = new List<decimal> { 0.03m, 0.05m, 0.07m, 0.10m }; // 3%, 5%, 7%, 10%
                var longTermTPs = new List<decimal> { 0.15m, 0.20m, 0.25m, 0.30m };  // 15%, 20%, 25%, 30%
                var stopLoss = 0.07m; // 7% SL

                // Kết quả cho từng TP level
                var shortTermResults = new Dictionary<decimal, (int wins, int losses, decimal totalProfit)>();
                var longTermResults = new Dictionary<decimal, (int wins, int losses, decimal totalProfit)>();

                foreach (var tp in shortTermTPs)
                    shortTermResults[tp] = (0, 0, 0m);
                foreach (var tp in longTermTPs)
                    longTermResults[tp] = (0, 0, 0m);

                Console.WriteLine($"Testing {testSymbols.Count} symbols...");
                Console.WriteLine($"Short-term TPs: {string.Join(", ", shortTermTPs.Select(x => $"{x * 100}%"))}");
                Console.WriteLine($"Long-term TPs: {string.Join(", ", longTermTPs.Select(x => $"{x * 100}%"))}");
                Console.WriteLine($"Stop Loss: {stopLoss * 100}%");
                Console.WriteLine();

                foreach (var symbol in testSymbols)
                {
                    try
                    {
                        Console.WriteLine($"Processing {symbol}...");

                        // Lấy dữ liệu
                        var lData = await _marketDataService.SSI_GetDataStock(symbol);
                        if (lData == null || lData.Count() < 100)
                        {
                            Console.WriteLine($"  Không đủ dữ liệu");
                            continue;
                        }

                        // Tính Bollinger Bands
                        var bb = lData.GetBollingerBands(20, 2);

                        // Tìm các điểm entry
                        var entries = new List<(DateTime date, decimal price, string reason)>();

                        for (int i = 1; i < lData.Count(); i++)
                        {
                            var current = lData.ElementAt(i);
                            var bbCurrent = bb.ElementAt(i);

                            if (!bbCurrent.Sma.HasValue || !bbCurrent.LowerBand.HasValue || !bbCurrent.UpperBand.HasValue)
                                continue;

                            var sma = (decimal)bbCurrent.Sma.Value;
                            var lower = (decimal)bbCurrent.LowerBand.Value;
                            var upper = (decimal)bbCurrent.UpperBand.Value;

                            // Entry Rule 1: Close trong 1/2 khoảng từ SMA20 đến LowerBand
                            var midLower = sma - (sma - lower) / 2;
                            if (current.Close >= midLower && current.Close <= sma)
                            {
                                entries.Add((current.Date, current.Close, "Lower Zone"));
                                continue;
                            }

                            // Entry Rule 2: Close >= SMA20 và trong 1/2 khoảng từ SMA20 đến UpperBand
                            var midUpper = sma + (upper - sma) / 2;
                            if (current.Close >= sma && current.Close <= midUpper)
                            {
                                entries.Add((current.Date, current.Close, "Upper Zone"));
                            }
                        }

                        Console.WriteLine($"  Found {entries.Count} entry points");

                        // Test từng entry point với các TP levels
                        foreach (var entry in entries)
                        {
                            // Tìm dữ liệu sau entry
                            var futureData = lData.Where(x => x.Date > entry.date).ToList();
                            if (!futureData.Any()) continue;

                            // Test Short-term TPs
                            foreach (var tp in shortTermTPs)
                            {
                                var result = SimulateTrade(entry.price, tp, stopLoss, futureData, 30); // 30 days max
                                var current = shortTermResults[tp];
                                if (result.isWin)
                                {
                                    shortTermResults[tp] = (current.wins + 1, current.losses, current.totalProfit + result.profit);
                                }
                                else
                                {
                                    shortTermResults[tp] = (current.wins, current.losses + 1, current.totalProfit + result.profit);
                                }
                            }

                            // Test Long-term TPs
                            foreach (var tp in longTermTPs)
                            {
                                var result = SimulateTrade(entry.price, tp, stopLoss, futureData, 90); // 90 days max
                                var current = longTermResults[tp];
                                if (result.isWin)
                                {
                                    longTermResults[tp] = (current.wins + 1, current.losses, current.totalProfit + result.profit);
                                }
                                else
                                {
                                    longTermResults[tp] = (current.wins, current.losses + 1, current.totalProfit + result.profit);
                                }
                            }
                        }

                        await Task.Delay(200); // Rate limit
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Error: {ex.Message}");
                        _logger.LogError(ex, $"Error backtesting {symbol}");
                    }
                }

                // In kết quả
                Console.WriteLine();
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("📊 KẾT QUẢ BACKTEST");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine();

                Console.WriteLine("📈 SHORT-TERM STRATEGY (Max 30 days):");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                decimal bestShortTermTP = 0;
                decimal bestShortTermWinRate = 0;
                foreach (var tp in shortTermTPs.OrderByDescending(x => x))
                {
                    var result = shortTermResults[tp];
                    var total = result.wins + result.losses;
                    var winRate = total > 0 ? (decimal)result.wins / total * 100 : 0;
                    var avgProfit = total > 0 ? result.totalProfit / total : 0;

                    Console.WriteLine($"TP {tp * 100}%:");
                    Console.WriteLine($"  Trades: {total} | Wins: {result.wins} | Losses: {result.losses}");
                    Console.WriteLine($"  Win Rate: {winRate:N1}%");
                    Console.WriteLine($"  Avg Profit: {avgProfit:N2}%");
                    Console.WriteLine($"  Total Profit: {result.totalProfit:N2}%");
                    Console.WriteLine();

                    if (winRate > bestShortTermWinRate)
                    {
                        bestShortTermWinRate = winRate;
                        bestShortTermTP = tp;
                    }
                }

                Console.WriteLine("📈 LONG-TERM STRATEGY (Max 90 days):");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                decimal bestLongTermTP = 0;
                decimal bestLongTermWinRate = 0;
                foreach (var tp in longTermTPs.OrderByDescending(x => x))
                {
                    var result = longTermResults[tp];
                    var total = result.wins + result.losses;
                    var winRate = total > 0 ? (decimal)result.wins / total * 100 : 0;
                    var avgProfit = total > 0 ? result.totalProfit / total : 0;

                    Console.WriteLine($"TP {tp * 100}%:");
                    Console.WriteLine($"  Trades: {total} | Wins: {result.wins} | Losses: {result.losses}");
                    Console.WriteLine($"  Win Rate: {winRate:N1}%");
                    Console.WriteLine($"  Avg Profit: {avgProfit:N2}%");
                    Console.WriteLine($"  Total Profit: {result.totalProfit:N2}%");
                    Console.WriteLine();

                    if (winRate > bestLongTermWinRate)
                    {
                        bestLongTermWinRate = winRate;
                        bestLongTermTP = tp;
                    }
                }

                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("🎯 RECOMMENDED SETTINGS:");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine($"Short-term: TP {bestShortTermTP * 100}% (Win Rate: {bestShortTermWinRate:N1}%)");
                Console.WriteLine($"Long-term: TP {bestLongTermTP * 100}% (Win Rate: {bestLongTermWinRate:N1}%)");
                Console.WriteLine($"Stop Loss: {stopLoss * 100}%");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
                _logger.LogError(ex, "BacktestBBEntryStrategy failed");
            }
        }

        private (bool isWin, decimal profit) SimulateTrade(decimal entryPrice, decimal takeProfitPercent, decimal stopLossPercent, List<Quote> futureData, int maxDays)
        {
            var tpPrice = entryPrice * (1 + takeProfitPercent);
            var slPrice = entryPrice * (1 - stopLossPercent);

            for (int i = 0; i < Math.Min(futureData.Count, maxDays); i++)
            {
                var candle = futureData[i];

                // Check SL first
                if (candle.Low <= slPrice)
                {
                    return (false, -stopLossPercent * 100); // Loss
                }

                // Check TP
                if (candle.High >= tpPrice)
                {
                    return (true, takeProfitPercent * 100); // Win
                }
            }

            // Timeout - close at current price
            if (futureData.Any())
            {
                var lastPrice = futureData.Last().Close;
                var profit = ((lastPrice - entryPrice) / entryPrice) * 100;
                return (profit > 0, profit);
            }

            return (false, 0);
        }

        public async Task BacktestBBEntryStrategy_Advanced()
        {
            try
            {
                Console.WriteLine("=== BACKTEST BB ENTRY STRATEGY (ADVANCED WITH FILTERS) ===");
                Console.WriteLine($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                Console.WriteLine();

                var testSymbols = new List<string>
                {
                    "SSI", "DIG", "DPG", "GEX", "VIX",
                    "VND", "DXG", "NLG", "DBC", "SHB"
                };

                Console.WriteLine($"Testing {testSymbols.Count} symbols with 5 filter levels...");
                Console.WriteLine();

                // Test each filter level
                for (int filterLevel = 1; filterLevel <= 5; filterLevel++)
                {
                    await TestFilterLevel(filterLevel, testSymbols);
                }

                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("✅ BACKTEST COMPLETED");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
                _logger.LogError(ex, "BacktestBBEntryStrategy_Advanced failed");
            }
        }

        private async Task TestFilterLevel(int level, List<string> symbols)
        {
            Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine($"📊 FILTER LEVEL {level}");
            Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            var filterDesc = level switch
            {
                1 => "BB Entry only (baseline)",
                2 => "BB + Group Accumulation",
                3 => "BB + Group + RSI",
                4 => "BB + Group + RSI + Volume",
                5 => "ALL Filters (Group + RSI + Volume + Buy Pressure)",
                _ => "Unknown"
            };
            Console.WriteLine($"Filters: {filterDesc}");
            Console.WriteLine();

            var results = new Dictionary<decimal, (int wins, int losses, decimal totalProfit)>();
            var tpLevels = new List<decimal> { 0.05m, 0.07m, 0.10m };

            foreach (var tp in tpLevels)
                results[tp] = (0, 0, 0m);

            int totalEntries = 0;
            int filteredEntries = 0;

            foreach (var symbol in symbols)
            {
                try
                {
                    var lData = await _marketDataService.SSI_GetDataStock(symbol);
                    if (lData == null || lData.Count() < 100) continue;

                    var bb = lData.GetBollingerBands(20, 2);
                    var rsi = lData.GetRsi(14);

                    // Get investor data if needed
                    DAL.Entity.PhanLoaiNDT phanLoaiData = null;
                    if (level >= 2)
                    {
                        phanLoaiData = _phanLoaiNDTRepo.GetEntityByFilter(
                            MongoDB.Driver.Builders<DAL.Entity.PhanLoaiNDT>.Filter.Eq(x => x.s, symbol));
                    }

                    // Find entries
                    for (int i = 50; i < lData.Count(); i++)
                    {
                        var current = lData.ElementAt(i);
                        var bbCurrent = bb.ElementAt(i);
                        var rsiCurrent = rsi.ElementAt(i);

                        if (!bbCurrent.Sma.HasValue || !bbCurrent.LowerBand.HasValue || !bbCurrent.UpperBand.HasValue)
                            continue;

                        var sma = (decimal)bbCurrent.Sma.Value;
                        var lower = (decimal)bbCurrent.LowerBand.Value;
                        var upper = (decimal)bbCurrent.UpperBand.Value;

                        // Check BB Entry
                        var midLower = sma - (sma - lower) / 2;
                        var midUpper = sma + (upper - sma) / 2;

                        bool isBBEntry = (current.Close >= midLower && current.Close <= sma) ||
                                         (current.Close >= sma && current.Close <= midUpper);

                        if (!isBBEntry) continue;

                        totalEntries++;

                        // Apply filters based on level
                        bool passFilters = true;

                        // Level 2+: Group Accumulation
                        if (level >= 2 && passFilters && phanLoaiData != null)
                        {
                            var groupData = phanLoaiData.Group;
                            if (groupData != null && groupData.Count > 5)
                            {
                                var recent = groupData.TakeLast(5).ToList();
                                int buyStreak = 0;
                                for (int j = recent.Count - 1; j >= 0; j--)
                                {
                                    if (recent[j] > 0) buyStreak++;
                                    else break;
                                }
                                if (buyStreak < 3) passFilters = false;
                            }
                            else
                            {
                                passFilters = false;
                            }
                        }

                        // Level 3+: RSI
                        if (level >= 3 && passFilters)
                        {
                            if (!rsiCurrent.Rsi.HasValue) passFilters = false;
                            else
                            {
                                var rsiVal = (decimal)rsiCurrent.Rsi.Value;
                                // Lower zone: RSI < 50, Upper zone: RSI > 50
                                if (current.Close < sma && rsiVal > 50) passFilters = false;
                                if (current.Close > sma && rsiVal < 50) passFilters = false;
                            }
                        }

                        // Level 4+: Volume (simplified - check if above average)
                        if (level >= 4 && passFilters)
                        {
                            // Get MA20 volume
                            var recentVolumes = lData.Skip(Math.Max(0, i - 20)).Take(20).Select(x => x.Volume).ToList();
                            if (recentVolumes.Any())
                            {
                                var avgVolume = recentVolumes.Average();
                                if (current.Volume < avgVolume) passFilters = false;
                            }
                        }

                        if (!passFilters) continue;

                        filteredEntries++;

                        // Test this entry
                        var futureData = lData.Skip(i + 1).ToList();
                        if (!futureData.Any()) continue;

                        foreach (var tp in tpLevels)
                        {
                            var result = SimulateTrade(current.Close, tp, 0.07m, futureData, 30);
                            var curr = results[tp];
                            if (result.isWin)
                                results[tp] = (curr.wins + 1, curr.losses, curr.totalProfit + result.profit);
                            else
                                results[tp] = (curr.wins, curr.losses + 1, curr.totalProfit + result.profit);
                        }
                    }

                    await Task.Delay(200);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in level {level} for {symbol}");
                }
            }

            // Print results
            Console.WriteLine($"Total Potential Entries: {totalEntries}");
            Console.WriteLine($"Entries After Filters: {filteredEntries}");
            Console.WriteLine($"Filter Rate: {(totalEntries > 0 ? (decimal)filteredEntries / totalEntries * 100 : 0):N1}%");
            Console.WriteLine();

            decimal bestWinRate = 0;
            decimal bestTP = 0;

            foreach (var tp in tpLevels.OrderByDescending(x => x))
            {
                var result = results[tp];
                var total = result.wins + result.losses;
                var winRate = total > 0 ? (decimal)result.wins / total * 100 : 0;
                var avgProfit = total > 0 ? result.totalProfit / total : 0;

                Console.WriteLine($"TP {tp * 100}%:");
                Console.WriteLine($"  Trades: {total} | Wins: {result.wins} | Losses: {result.losses}");
                Console.WriteLine($"  Win Rate: {winRate:N1}%");
                Console.WriteLine($"  Avg Profit: {avgProfit:N2}%");
                Console.WriteLine($"  Total Profit: {result.totalProfit:N2}%");
                Console.WriteLine();

                if (winRate > bestWinRate)
                {
                    bestWinRate = winRate;
                    bestTP = tp;
                }
            }

            Console.WriteLine($"🎯 Best for Level {level}: TP {bestTP * 100}% (Win Rate: {bestWinRate:N1}%)");
            Console.WriteLine();
        }

        /// <summary>
        /// Backtest Optimal Strategy: BB Entry + Group Accumulation only
        /// Test với TẤT CẢ mã trong StaticVal._lStock
        /// TP: 10%, SL: 7%
        /// </summary>
        public async Task BacktestOptimalStrategy()
        {
            try
            {
                Console.WriteLine("=== BACKTEST OPTIMAL STRATEGY (BB + GROUP) ===");
                Console.WriteLine($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                Console.WriteLine();
                Console.WriteLine("Filters: BB Entry (1/2 zones) + Group Accumulation (3+ days)");
                Console.WriteLine("TP: 10% | SL: 7%");
                Console.WriteLine();

                var allSymbols = _symbolRepo.GetAll();
                allSymbols = new List<Symbol>
                {
                    new Symbol{s="SSI"}
                };
                if (allSymbols == null || !allSymbols.Any())
                {
                    Console.WriteLine("❌ Không có mã trong StaticVal._lStock");
                    return;
                }

                Console.WriteLine($"Testing {allSymbols.Count} symbols...");
                Console.WriteLine();

                var tp = 0.10m; // 10%
                var sl = 0.07m; // 7%
                int wins = 0;
                int losses = 0;
                decimal totalProfit = 0;
                int totalEntries = 0;
                int validEntries = 0;
                int processedSymbols = 0;

                foreach (var stock in allSymbols)
                {
                    try
                    {
                        var symbol = stock.s;
                        
                        // Lấy dữ liệu
                        var lData = await _marketDataService.SSI_GetDataStock(symbol);
                        if (lData == null || lData.Count() < 100) continue;

                        // ✨ QUALITY FILTERS
                        var latest = lData.Last();
                        
                        // Filter 1: Minimum Price (≥ 5,000 VND)
                        if (latest.Close < 5) continue;
                        
                        // Filter 2: Minimum Average Volume (≥ 100,000 shares/day)
                        var recentVolumes = lData.TakeLast(20).Select(x => x.Volume).ToList();
                        var avgVolume = recentVolumes.Average();
                        if (avgVolume < 100000) continue;
                        
                        // Filter 3: Minimum Liquidity (Price × Volume ≥ 500M VND)
                        var liquidity = latest.Close * avgVolume;
                        if (liquidity < 500000) continue;

                        // Tính BB
                        var bb = lData.GetBollingerBands(20, 2);

                        // Lấy Group data
                        var phanLoaiData = _phanLoaiNDTRepo.GetEntityByFilter(
                            MongoDB.Driver.Builders<DAL.Entity.PhanLoaiNDT>.Filter.Eq(x => x.s, symbol));

                        if (phanLoaiData == null || phanLoaiData.Group == null || phanLoaiData.Group.Count < 5)
                            continue;

                        // Tìm entries
                        for (int i = 50; i < lData.Count(); i++)
                        {
                            var current = lData.ElementAt(i);
                            var bbCurrent = bb.ElementAt(i);

                            if (!bbCurrent.Sma.HasValue || !bbCurrent.LowerBand.HasValue || !bbCurrent.UpperBand.HasValue)
                                continue;

                            var sma = (decimal)bbCurrent.Sma.Value;
                            var lower = (decimal)bbCurrent.LowerBand.Value;
                            var upper = (decimal)bbCurrent.UpperBand.Value;

                            // Check BB Entry (1/2 zones)
                            var midLower = sma - (sma - lower) / 2;
                            var midUpper = sma + (upper - sma) / 2;

                            bool isBBEntry = (current.Close >= midLower && current.Close <= sma) ||
                                             (current.Close >= sma && current.Close <= midUpper);

                            if (!isBBEntry) continue;

                            totalEntries++;

                            // Check Group Accumulation (3+ days)
                            var groupData = phanLoaiData.Group;
                            var recent = groupData.TakeLast(Math.Min(5, groupData.Count)).ToList();
                            int buyStreak = 0;
                            for (int j = recent.Count - 1; j >= 0; j--)
                            {
                                if (recent[j] > 0) buyStreak++;
                                else break;
                            }

                            if (buyStreak < 3) continue;

                            validEntries++;

                            // Test trade
                            var futureData = lData.Skip(i + 1).ToList();
                            if (!futureData.Any()) continue;

                            var result = SimulateTrade(current.Close, tp, sl, futureData, 30);
                            if (result.isWin)
                            {
                                wins++;
                                totalProfit += result.profit;
                            }
                            else
                            {
                                losses++;
                                totalProfit += result.profit;
                            }
                        }

                        processedSymbols++;
                        if (processedSymbols % 100 == 0)
                        {
                            Console.WriteLine($"Processed {processedSymbols}/{allSymbols.Count} symbols...");
                        }

                        await Task.Delay(200); // Rate limit
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing {stock.s}");
                    }
                }

                // Print results
                Console.WriteLine();
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("📊 KẾT QUẢ BACKTEST");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine();
                Console.WriteLine($"Symbols Processed: {processedSymbols}/{allSymbols.Count}");
                Console.WriteLine($"Total Potential Entries: {totalEntries}");
                Console.WriteLine($"Valid Entries (after Group filter): {validEntries}");
                Console.WriteLine($"Filter Pass Rate: {(totalEntries > 0 ? (decimal)validEntries / totalEntries * 100 : 0):N1}%");
                Console.WriteLine();

                var totalTrades = wins + losses;
                var winRate = totalTrades > 0 ? (decimal)wins / totalTrades * 100 : 0;
                var avgProfit = totalTrades > 0 ? totalProfit / totalTrades : 0;

                Console.WriteLine($"Total Trades: {totalTrades}");
                Console.WriteLine($"Wins: {wins} | Losses: {losses}");
                Console.WriteLine($"Win Rate: {winRate:N1}%");
                Console.WriteLine($"Avg Profit per Trade: {avgProfit:N2}%");
                Console.WriteLine($"Total Profit: {totalProfit:N2}%");
                Console.WriteLine();
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine("✅ BACKTEST COMPLETED");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
                _logger.LogError(ex, "BacktestOptimalStrategy failed");
            }
        }

        public async Task BackTest22122025()
        {
            try
            {
                Console.WriteLine("=== BACKTEST OPTIMAL STRATEGY (BB + GROUP) ===");
                Console.WriteLine($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                Console.WriteLine();
                Console.WriteLine("Filters: BB Entry (1/2 zones) + Group Accumulation (3+ days)");
                Console.WriteLine("TP: 10% | SL: 7%");
                Console.WriteLine();

                var allSymbols = _symbolRepo.GetAll();
                allSymbols = new List<Symbol>
                {
                    new Symbol{s="STB"}
                };
                if (allSymbols == null || !allSymbols.Any())
                {
                    Console.WriteLine("❌ Không có mã trong StaticVal._lStock");
                    return;
                }

                Console.WriteLine($"Testing {allSymbols.Count} symbols...");
                Console.WriteLine();

                //var tp = 0.10m; // 10%
                //var sl = 0.07m; // 7%
                //int wins = 0;
                //int losses = 0;
                //decimal totalProfit = 0;
                //int totalEntries = 0;
                //int validEntries = 0;
                //int processedSymbols = 0;

                foreach (var stock in allSymbols)
                {
                    try
                    {
                        var symbol = stock.s;

                        // Lấy dữ liệu
                        var lData = (await _marketDataService.SSI_GetDataStockT(
symbol)).DistinctBy(x => x.Date).ToList();
                        if (lData == null || lData.Count() < 100) continue;

                        // ✨ QUALITY FILTERS
                        var latest = lData.Last();

                        // Filter 1: Minimum Price (≥ 5,000 VND)
                        if (latest.Close < 5) continue;

                        // Filter 2: Minimum Average Volume (≥ 100,000 shares/day)
                        var recentVolumes = lData.TakeLast(20).Select(x => x.Volume).ToList();
                        var avgVolume = recentVolumes.Average();
                        if (avgVolume < 100000) continue;

                        // Filter 3: Minimum Liquidity (Price × Volume ≥ 500M VND)
                        var liquidity = latest.Close * avgVolume;
                        if (liquidity < 500000) continue;

                        // Tính BB
                        var lbb = lData.GetBollingerBands(20, 2);
                        var lrsi = lData.GetRsi(14);
                        var lma9 = lrsi.GetSma(9);
                        //StockInfo
                        var now = DateTime.Now;
                        var from = now.AddYears(-1);

                        var lInfo = await _marketDataService.SSI_GetStockInfo(symbol, now.AddDays(-30), now);
                        var lPrev_1 = await _marketDataService.SSI_GetStockInfo(symbol, now.AddDays(-60), now.AddDays(-31));
                        var lPrev_2 = await _marketDataService.SSI_GetStockInfo(symbol, now.AddDays(-90), now.AddDays(-61));
                        var lPrev_3 = await _marketDataService.SSI_GetStockInfo(symbol, now.AddDays(-120), now.AddDays(-91));
                        lInfo.data.AddRange(lPrev_1.data);
                        lInfo.data.AddRange(lPrev_2.data);
                        lInfo.data.AddRange(lPrev_3.data);
                        foreach (var item in lInfo.data)
                        {
                            var date = DateTime.ParseExact(item.tradingDate, "dd/MM/yyyy", null);
                            item.TimeStamp = new DateTimeOffset(date.Date, TimeSpan.Zero).ToUnixTimeSeconds();
                        }

                        // Tìm entries
                        bool isPrePressure = false, isPreNN1 = false;
                        for (int i = 50; i < lData.Count(); i++)
                        {
                            var lDataCheck = lData.Take(i).ToList();
                            var filter = Builders<PreEntry>.Filter.Eq(x => x.s, symbol);
                            var pre = _preEntryRepo.GetEntityByFilter(filter);
                            var res = lDataCheck.CheckEntry(lInfo, pre);
                            if(res.preAction == EPreEntryAction.DELETE)
                            {
                                if (pre != null)
                                {
                                    _preEntryRepo.DeleteMany(filter);
                                }
                            }
                            else if(res.preAction == EPreEntryAction.UPDATE)
                            {
                                if((!res.pre.isPrePressure && !res.pre.isPreNN1))
                                {
                                    if(pre != null)
                                    {
                                        _preEntryRepo.DeleteMany(filter);
                                    }
                                }
                                else
                                {
                                    if (pre is null)
                                    {
                                        res.pre.s = symbol;
                                        _preEntryRepo.InsertOne(res.pre);
                                    }
                                    else
                                    {
                                        _preEntryRepo.Update(res.pre);
                                    }
                                }
                            }
                            if(res.Response > 0)
                            {
                                var signal = string.Empty;
                                if ((res.Response & EEntry.RSI) == EEntry.RSI)
                                {
                                    // Có RSI
                                    signal += "RSI;";
                                }
                                if ((res.Response & EEntry.PRESSURE) == EEntry.PRESSURE)
                                {
                                    // Có Pressure
                                    signal += "Pressure;";
                                }
                                if ((res.Response & EEntry.NN1) == EEntry.NN1)
                                {
                                    // Có NN1
                                    signal += "NN1;";
                                }

                                if ((res.Response & EEntry.NN2) == EEntry.NN2)
                                {
                                    // Có NN1
                                    signal += "NN2;";
                                }
                                if ((res.Response & EEntry.NN3) == EEntry.NN3)
                                {
                                    // Có NN1
                                    signal += "NN3;";
                                }
                                if ((res.Response & EEntry.WYCKOFF) == EEntry.WYCKOFF)
                                {
                                    // Có NN1
                                    signal += "Wyckoff;";
                                }
                                var mes = $"{symbol}|{res.quote.Date.ToString("dd/MM/yyyy")}|{signal}";
                                Console.WriteLine(mes);
                            }
                        }
                        await Task.Delay(200); // Rate limit
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing {stock.s}");
                    }
                }

                //// Print results
                //Console.WriteLine();
                //Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                //Console.WriteLine("📊 KẾT QUẢ BACKTEST");
                //Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                //Console.WriteLine();
                //Console.WriteLine($"Symbols Processed: {processedSymbols}/{allSymbols.Count}");
                //Console.WriteLine($"Total Potential Entries: {totalEntries}");
                //Console.WriteLine($"Valid Entries (after Group filter): {validEntries}");
                //Console.WriteLine($"Filter Pass Rate: {(totalEntries > 0 ? (decimal)validEntries / totalEntries * 100 : 0):N1}%");
                //Console.WriteLine();

                //var totalTrades = wins + losses;
                //var winRate = totalTrades > 0 ? (decimal)wins / totalTrades * 100 : 0;
                //var avgProfit = totalTrades > 0 ? totalProfit / totalTrades : 0;

                //Console.WriteLine($"Total Trades: {totalTrades}");
                //Console.WriteLine($"Wins: {wins} | Losses: {losses}");
                //Console.WriteLine($"Win Rate: {winRate:N1}%");
                //Console.WriteLine($"Avg Profit per Trade: {avgProfit:N2}%");
                //Console.WriteLine($"Total Profit: {totalProfit:N2}%");
                //Console.WriteLine();
                //Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                //Console.WriteLine("✅ BACKTEST COMPLETED");
                //Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
                _logger.LogError(ex, "BacktestOptimalStrategy failed");
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
}
