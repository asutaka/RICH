using Bybit.Net.Enums;
using CoinUtilsPr;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Skender.Stock.Indicators;
using TestPr.DAL;
using TestPr.DAL.Entity;

namespace TestPr.Service
{
    public interface ITestService
    {
        Task Bybit_LONG(string s = "");
        Task Bybit_SHORT(string s = "");
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

        public async Task Bybit_LONG(string s = "")
        {
            try
            {
                var DAY = 180;
                int HOUR = 8;
                var start = DateTime.UtcNow;
                var exchange = (int)EExchange.Bybit;
                var builder = Builders<Symbol>.Filter;
                var lSym = _symRepo.GetByFilter(builder.And(
                    builder.Eq(x => x.ex, exchange),
                    builder.Eq(x => x.ty, (int)OrderSide.Buy),
                    builder.Eq(x => x.status, 0)
                ));
                decimal SL_RATE = 2.5m;
                decimal rateProfit_Min = 2.5m;
                decimal rateProfit_Max = 7m;

                var lModel = new List<clsData>();
                var lResult = new List<clsResult>();

                var winTotal = 0;
                var lossTotal = 0;

                if (!string.IsNullOrWhiteSpace(s))
                {
                    lSym.Clear();
                    lSym.Add(new Symbol
                    {
                        s = s,
                        op = (int)EOrderSideOption.OP_4
                    });
                }
                var dic = new Dictionary<string, List<Quote>>();
                foreach (var sym in lSym.Select(x => x.s))
                {
                    if (sym.Contains('-'))
                        continue;
                    var winCount = 0;
                    var lossCount = 0;
                    try
                    {
                        var lData15m = await _apiService.GetData_Bybit(sym, start.AddDays(-DAY));
                        var count = lData15m.Count();
                        var lbb = lData15m.GetBollingerBands();
                        var lAdd = new List<Quote>();
                        for (int i = 0; i < count; i++)
                        {
                            var element = lData15m[i];
                            var element_BB = lbb.ElementAt(i);
                            var upMA20 = element_BB.Sma is null ? -1 : (element.Close >= (decimal)element_BB.Sma.Value ? 1 : 0);
                            var rateUP = -1;
                            var rateDOWN = -1;
                            if (upMA20 == 1)
                            {
                                var is1_3 = Math.Abs(element.Close - (decimal)element_BB.Sma.Value) >= 2 * Math.Abs((decimal)element_BB.UpperBand.Value - element.Close);
                                rateUP = is1_3 ? 1 : 0;
                            }
                            else if (upMA20 == 0)
                            {
                                var is1_3 = Math.Abs(element.Close - (decimal)element_BB.Sma.Value) >= 2 * Math.Abs((decimal)element_BB.LowerBand.Value - element.Close);
                                rateDOWN = is1_3 ? 1 : 0;
                            }
                            lAdd.Add(element);
                        }
                        dic.Add(sym, lAdd);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{sym}| {ex.Message}");
                    }
                }

                foreach (var sym in dic.Select(x => x.Key))
                {
                    var element = lSym.First(x => x.s == sym);
                    //if (element.op != (int)EOrderSideOption.OP_2)
                    //    continue;

                    var winCount = 0;
                    var lossCount = 0;
                    try
                    {
                        var lData15m = dic[sym];
                        var lbb = lData15m.GetBollingerBands();
                        var lrsi = lData15m.GetRsi();
                        var lVol = lData15m.Select(x => new Quote
                        {
                            Date = x.Date,
                            Close = x.Volume
                        }).ToList();
                        var lMaVol = lVol.GetSma(20);

                        DateTime dtFlag = DateTime.MinValue;
                        foreach (var cur in lData15m)
                        {
                            try
                            {
                                if (cur.Date <= dtFlag)
                                    continue;

                                var flag = lData15m.Where(x => x.Date <= cur.Date).ToList().IsFlagBuy3();
                                if (!flag.Item1)
                                    continue;

                                var entity_Pivot = flag.Item2;
                                var bb_Pivot = lbb.First(x => x.Date == entity_Pivot.Date);

                                #region Buy ENTRY
                                var isPass = false;
                                var lCheck = lData15m.Where(x => x.Date > entity_Pivot.Date).Take(8);
                                foreach (var check in lCheck)
                                {
                                    if (flag.Item3)
                                    {
                                        isPass = true; break;
                                    }
                                    var action = check.IsBuy2();
                                    if (!action)
                                        continue;

                                    entity_Pivot = check;
                                    isPass = true; break;
                                }
                                if (!isPass)
                                    continue;
                                #endregion

                                var rateBB = (decimal)(Math.Round(100 * (-1 + bb_Pivot.UpperBand.Value / bb_Pivot.LowerBand.Value)) - 1);
                                if (rateBB > rateProfit_Max)
                                {
                                    rateBB = rateProfit_Max;
                                }
                                else if(rateBB < rateProfit_Min)
                                {
                                    rateBB = rateProfit_Min;
                                }

                                var lClose = lData15m.Where(x => x.Date > entity_Pivot.Date && x.Date <= entity_Pivot.Date.AddHours(HOUR));
                                var closeCount = lClose.Count();
                                var isEnd = closeCount == HOUR * 4;

                                var isChotNon = false;
                                Quote eClose = null;
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

                                    var rateH = Math.Round(100 * (-1 + itemClose.High / entity_Pivot.Close), 1); 
                                    if (rateH >= rateBB)
                                    {
                                        var close = entity_Pivot.Close * (decimal)(1 + rateBB / 100);
                                        itemClose.Close = close;
                                        eClose = itemClose;
                                        break;
                                    }
                                    var rateL = Math.Round(100 * (-1 + itemClose.Low / entity_Pivot.Close), 1);
                                    if(rateL <= -SL_RATE)
                                    {
                                        var close = entity_Pivot.Close * (decimal)(1 - SL_RATE / 100);
                                        itemClose.Close = close;
                                        eClose = itemClose;
                                        break;
                                    }

                                    eClose = itemClose;
                                }
                                if (!isEnd)
                                {
                                    continue;
                                }

                                dtFlag = eClose.Date;
                                var rate = Math.Round(100 * (-1 + eClose.Close / entity_Pivot.Close), 2);
                                var lRange = lData15m.Where(x => x.Date >= entity_Pivot.Date.AddMinutes(15) && x.Date <= eClose.Date);

                                var winloss = "W";
                                if (rate <= (decimal)0)
                                {
                                    winloss = "L";
                                    lossCount++;
                                }
                                else
                                {
                                    winCount++;
                                }

                                var mesItem = $"{sym}|{winloss}|ENTRY: {entity_Pivot.Date.ToString("dd/MM/yyyy HH:mm")}|CLOSE: {eClose.Date.ToString("dd/MM/yyyy HH:mm")}|Rate: {rate}%";
                                Console.WriteLine(mesItem);
                                //lRate.Add(rate);
                                lModel.Add(new clsData
                                {
                                    s = sym,
                                    Date = entity_Pivot.Date,
                                    Rate = rate,
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"SyncDataService.Bybit_LONG|EXCEPTION| {ex.Message}");
                            }

                        }

                        if (winCount + lossCount <= 0)
                            continue;

                        var rateRes = Math.Round(((decimal)winCount / (winCount + lossCount)), 2);
                        var sumRate = lModel.Where(x => x.s == sym).Sum(x => x.Rate);
                        var count = lModel.Count(x => x.s == sym);
                        var items = lModel.Where(x => x.s == sym);
                        var perRate = Math.Round((float)sumRate / count, 1);
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

                        var winrate = Math.Round((double)realWin / count, 1);

                        var mes = $"{sym}\t\t\t| W/Total: {realWin}/{count} = {winrate}%|Rate: {sumRate}%|Per: {perRate}%";
                        //Console.WriteLine(mes);

                        lResult.Add(new clsResult
                        {
                            s = sym,
                            Win = realWin,
                            Winrate = winrate,
                            Perate = perRate,
                            Mes = mes
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{sym}| {ex.Message}");
                    }
                }

                var lRes = lResult.OrderByDescending(x => x.Winrate).ThenByDescending(x => x.Win).ThenByDescending(x => x.Perate).ToList();

                foreach (var item in lRes)
                {
                    Console.WriteLine(item.Mes);
                }

                Console.WriteLine($"Tong: {lModel.Sum(x => x.Rate)}%|W/L: {winTotal}/{lossTotal}");


                var end = DateTime.Now;
                Console.WriteLine($"TotalTime: {(end - start).TotalSeconds}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.Bybit_LONG|EXCEPTION| {ex.Message}");
            }
        }

        //Tong: 340.7%|W/L: 287/119
        public async Task Bybit_SHORT(string s = "")
        {
            try
            {
                var start = DateTime.Now;
                var exchange = (int)EExchange.Bybit;
                var builder = Builders<Symbol>.Filter;
                var lSym = _symRepo.GetByFilter(builder.And(
                    builder.Eq(x => x.ex, exchange),
                    builder.Eq(x => x.ty, (int)OrderSide.Sell),
                    builder.Eq(x => x.status, 0)
                ));
                decimal SL_RATE = 2.5m;
                int hour = 4;
                decimal rateProfit_Min = 2.5m;
                decimal rateProfit_Max = 7m;

                var lModel = new List<clsData>();
                var lResult = new List<clsResult>();

                var winTotal = 0;
                var lossTotal = 0;
                //
                if (!string.IsNullOrWhiteSpace(s))
                {
                    lSym.Clear();
                    lSym.Add(new Symbol
                    {
                        s = s
                    });
                }
                var dic = new Dictionary<string, List<Quote>>();
                foreach (var item in lSym.Select(x => x.s))
                {
                    if (item.Contains('-'))
                        continue;

                    try
                    {
                        var lData15m = await _apiService.GetData_Bybit(item, DateTime.UtcNow.AddDays(-20));
                        var lbb = lData15m.GetBollingerBands();
                        var count = lData15m.Count();
                        var lAdd = new List<Quote>();
                        for (int i = 0; i < count; i++)
                        {
                            var element = lData15m[i];
                            var element_BB = lbb.ElementAt(i);
                            var upMA20 = element_BB.Sma is null ? -1 : (element.Close >= (decimal)element_BB.Sma.Value ? 1 : 0);
                            var rateUP = -1;
                            var rateDOWN = -1;
                            if(upMA20 == 1)
                            {
                                var is1_3 = Math.Abs(element.Close - (decimal)element_BB.Sma.Value) >= 2 * Math.Abs((decimal)element_BB.UpperBand.Value - element.Close);
                                rateUP = is1_3 ? 1 : 0;
                            }
                            else if(upMA20 == 0)
                            {
                                var is1_3 = Math.Abs(element.Close - (decimal)element_BB.Sma.Value) >= 2 * Math.Abs((decimal)element_BB.LowerBand.Value - element.Close);
                                rateDOWN = is1_3 ? 1 : 0;
                            }
                          
                            lAdd.Add(element);
                        }
                        dic.Add(item, lAdd);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                }

                foreach (var item in dic.Select(x => x.Key))
                {
                    if (item.Contains('-'))
                        continue;

                    var element = lSym.First(x => x.s == item);
                    //if (element.op != (int)EOrderSideOption.OP_0)
                    //    continue;

                    var winCount = 0;
                    var lossCount = 0;
                    try
                    {
                        var lData15m = dic[item];
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
                                var flag = lData15m.Where(x => x.Date <= ma20.Date).ToList().IsFlagSell();
                                if (!flag.Item1)
                                    continue;

                                var entity_Pivot = flag.Item2;
                                var entity_Sig = lData15m.First(x => x.Date == entity_Pivot.Date.AddMinutes(-15));
                                var bb_Pivot = lbb.First(x => x.Date == entity_Pivot.Date);

                                #region Thêm xử lý
                                var isPass = false;
                                var lCheck = lData15m.Where(x => x.Date > entity_Pivot.Date).Take(8).Skip(1);
                                var dtPrint = entity_Pivot.Date;
                                foreach (var check in lCheck)
                                {
                                    var action = check.IsSell(flag.Item2.Close, (EOrderSideOption)element.op);
                                    if (!action.Item1)
                                        continue;

                                    entity_Pivot = action.Item2;
                                    isPass = true; break;
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
                                ////zz
                                //var trace = lData15m.First(x => x.Date > entity_Sig.Date);
                                //float avg1_3_UP = 0;
                                //foreach (var val in dic.Values)
                                //{
                                //    var first = val.First(x => x.Date > entity_Sig.Date);
                                //    if (first.TotalRate1_3_UP == 1)
                                //        avg1_3_UP++;
                                //}
                                //double rate1_3_UP = Math.Round(100 * avg1_3_UP / dic.Count());
                                //if (rate1_3_UP >= 90)
                                //    continue;

                                //dtFlag = eClose.Date;
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

                                //var mesItem = $"{item}|{winloss}{rate1_3_UP >= 50}{rate1_3_UP >= 60}{rate1_3_UP >= 70}{rate1_3_UP >= 80}{rate1_3_UP >= 90}|ENTRY: {entity_Pivot.Date.ToString("dd/MM/yyyy HH:mm")}|CLOSE: {eClose.Date.ToString("dd/MM/yyyy HH:mm")}|SIG: {dtPrint.ToString("dd/MM/yyyy HH:mm")}|Rate: {rate}%";
                                var mesItem = $"{item}|{winloss}|ENTRY: {entity_Pivot.Date.ToString("dd/MM/yyyy HH:mm")}|CLOSE: {eClose.Date.ToString("dd/MM/yyyy HH:mm")}|SIG: {dtPrint.ToString("dd/MM/yyyy HH:mm")}|Rate: {rate}%";
                                Console.WriteLine(mesItem);

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

                        ////Special 
                        //if (rateRes < (decimal)0.5
                        //  || perRate <= 0.7)
                        //{
                        //    lModel = lModel.Except(items).ToList();
                        //    continue;
                        //}

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
                //var lRes = lResult.OrderByDescending(x => x.Winrate).ThenByDescending(x => x.Win).ThenByDescending(x => x.Perate).ToList();

                foreach (var item in lResult)
                {
                    Console.WriteLine(item.Mes);
                }

                Console.WriteLine($"Tong: {lModel.Sum(x => x.Rate)}%|W/L: {winTotal}/{lossTotal}");

                var end = DateTime.Now;
                Console.WriteLine($"TotalTime: {(end - start).TotalSeconds}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SyncDataService.Bybit_SHORT|EXCEPTION| {ex.Message}");
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
}



