﻿using Bybit.Net.Enums;
using CoinUtilsPr;
using CoinUtilsPr.DAL;
using CoinUtilsPr.DAL.Entity;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Skender.Stock.Indicators;
using System.Net.WebSockets;

namespace TestPr.Service
{
    public interface ITestService
    {
        Task PreTestLONG();
        Task ListLong();
        Task<List<clsResult>> Bybit_LONG(string s = "", int DAY = 20, int SKIP_DAY = 0);

        Task PreTestLONG_DOJI();
        Task ListLONG_DOJI();
        Task<List<clsResult>> Bybit_LONG_DOJI(string s = "", int DAY = 20, int SKIP_DAY = 0);

        Task PreTestSHORT();
        Task ListShort();
        Task<List<clsResult>> Bybit_SHORT(string s = "", int DAY = 20, int SKIP_DAY = 0);

        Task PreTestSHORT_DOJI();
        Task ListShort_DOJI();
        Task<List<clsResult>> Bybit_SHORT_DOJI(string s = "", int DAY = 20, int SKIP_DAY = 0);

        Task CheckWycKoff();
    }
    public class TestService : ITestService
    {
        private readonly ILogger<TestService> _logger;
        private readonly IAPIService _apiService;
        private readonly ISymbolRepo _symRepo;
        private int _COUNT = 0;
        public TestService(ILogger<TestService> logger, IAPIService apiService, ISymbolRepo symRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _symRepo = symRepo;
        }
        private Dictionary<string, List<Quote>> _dicData = new Dictionary<string, List<Quote>>();
        private async Task<List<Quote>> GetData(string sym, int DAY, int SKIP_DAY)
        {
            try
            {
                if (_dicData.ContainsKey(sym))
                {
                    var start = DateTime.UtcNow;
                    var day = start.AddDays(-DAY);
                    var skip = DateTime.MinValue;
                    if (SKIP_DAY > 0)
                    {
                        skip = start.AddDays(-SKIP_DAY);
                    }
                    var dat = _dicData[sym];
                    var lData = dat.Where(x => x.Date >= day && (skip == DateTime.MinValue || x.Date < skip)).ToList();
                    return lData;
                } 

                var lData15m = await _apiService.GetData_Bybit(sym, DAY, SKIP_DAY);
                _dicData.Add(sym, lData15m);


                return lData15m;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return null;
        }

        //Lấy các kết quả trả về để PreTest
        public async Task PreTestLONG()
        {
            try
            {
                var dt = DateTime.UtcNow;
                var lTake = new List<string>
                {
                    "SEIUSDT",
                    "COOKUSDT",
                    "FBUSDT",
                    "FARTCOINUSDT",
                    "ZRCUSDT",
                    "SOLVUSDT",
                    "REQUSDT",
                    "FLRUSDT",
                    "A8USDT",
                    "ALEOUSDT",
                    "POLYXUSDT",
                    "MOODENGUSDT",
                    "NTRNUSDT",
                    "SONICUSDT",
                    "MAGICUSDT",
                    "KAIAUSDT",
                    "MYRIAUSDT",
                    "API3USDT",
                    "ZENTUSDT",
                    "IOTXUSDT",
                    "RIFUSDT",
                    "DOGUSDT",
                    "SUPERUSDT",
                    "TRXUSDT",
                    "SERAPHUSDT",
                };
                //lTake.Sync(EExchange.Bybit, OrderSide.Buy, EOptionTrade.Normal, _symRepo);
                var lRank = new List<clsShow>();

                foreach (var s in lTake)
                {
                    try
                    {
                        var res20 = await Bybit_LONG(s, 20);
                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{s}| {ex.Message}");
                    }
                }
                var totalMinute = (DateTime.UtcNow - dt).TotalMinutes;
                Console.WriteLine($"TotalTime: {totalMinute}| COUNT: {_COUNT}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        //Tìm danh sách các Coin có tỉ lệ winrate tốt nhất
        public async Task ListLong()
        {
            try
            {
                var dt = DateTime.UtcNow;   
                var lAll = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, limit: 1000);
                var lUsdt = lAll.Data.List.Where(x => x.QuoteAsset == "USDT" && !x.Name.StartsWith("1000")).Select(x => x.Name);
                var lTake = lUsdt.Skip(400).Take(300);
                var lRank = new List<clsShow>();

                foreach (var s in lTake)
                {
                    try
                    {
                        var res180 = await Bybit_LONG(s, 180);
                        var res20 = await Bybit_LONG(s, 20);
                        var res60 = await Bybit_LONG(s, 60, 20);
                        var res90 = await Bybit_LONG(s, 90, 60);
                        var res150 = await Bybit_LONG(s, 150, 90);
                        Thread.Sleep(1000);

                        var total = res20.First().Total
                                    + res60.First().Total
                                    + res90.First().Total
                                    + res150.First().Total
                                    + res180.First().Total;

                        var win = res20.First().Win
                                  + res60.First().Win
                                  + res90.First().Win
                                  + res150.First().Win
                                  + res180.First().Win;

                        var winrate = Math.Round((double)win / total, 2);
                        var per = res20.First().Perate
                                 + res60.First().Perate
                                 + res90.First().Perate
                                 + res150.First().Perate
                                 + res180.First().Perate;
                        var perRate = Math.Round((double)per / 5, 2);

                        lRank.Add(new clsShow
                        {
                            s = s,
                            Win = win,
                            Total = total,
                            Winrate = winrate,
                            PerRate = perRate
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{s}| {ex.Message}");
                    }
                }
                Console.WriteLine("///////////////////////////////////////////////");
                Console.WriteLine("///////////////////////////////////////////////");
                Console.WriteLine("///////////////////////////////////////////////");
                foreach (var item in lRank.OrderByDescending(x => x.PerRate).ThenByDescending(x => x.Winrate).ThenByDescending(x => x.Total))
                {
                    Console.WriteLine($"{item.s}, W/Total: {item.Win}/{item.Total} = {item.Winrate}%, Per: {item.PerRate}%");
                }

                var totalMinute = (DateTime.UtcNow - dt).TotalMinutes;
                Console.WriteLine($"TotalTime: {totalMinute}");
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
            }
        }
        public async Task<List<clsResult>> Bybit_LONG(string s = "", int DAY = 20, int SKIP_DAY = 0)
        {
            try
            {
                //var DAY = 150;
                int HOUR = 8;
                var start = DateTime.UtcNow;
                var exchange = (int)EExchange.Bybit;
                var builder = Builders<Symbol>.Filter;
                var lSym = _symRepo.GetByFilter(builder.And(
                    builder.Eq(x => x.ex, exchange),
                    builder.Eq(x => x.ty, (int)OrderSide.Buy),
                    builder.Eq(x => x.status, 0)
                ));
                decimal SL_RATE = 3m;
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
                        var lData15m = await GetData(sym, DAY, SKIP_DAY);
                        var count = lData15m.Count();
                        //var lbb = lData15m.GetBollingerBands();
                        var lAdd = new List<Quote>();
                        for (int i = 0; i < count; i++)
                        {
                            var element = lData15m[i];
                            //var element_BB = lbb.ElementAt(i);
                            //var upMA20 = element_BB.Sma is null ? -1 : (element.Close >= (decimal)element_BB.Sma.Value ? 1 : 0);
                            //var rateUP = -1;
                            //var rateDOWN = -1;
                            //if (upMA20 == 1)
                            //{
                            //    var is1_3 = Math.Abs(element.Close - (decimal)element_BB.Sma.Value) >= 2 * Math.Abs((decimal)element_BB.UpperBand.Value - element.Close);
                            //    rateUP = is1_3 ? 1 : 0;
                            //}
                            //else if (upMA20 == 0)
                            //{
                            //    var is1_3 = Math.Abs(element.Close - (decimal)element_BB.Sma.Value) >= 2 * Math.Abs((decimal)element_BB.LowerBand.Value - element.Close);
                            //    rateDOWN = is1_3 ? 1 : 0;
                            //}
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

                                var flag = lData15m.Where(x => x.Date <= cur.Date).ToList().IsFlagBuy();
                                if (!flag.Item1)
                                    continue;

                                var entity_Pivot = flag.Item2;
                                var bb_Pivot = lbb.First(x => x.Date == entity_Pivot.Date);

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
                                        itemClose.Close = (decimal)ma.UpperBand;
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

                                    if (itemClose.Close >= (decimal)ma.Sma.Value)
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
                                    if (rateL <= -SL_RATE)
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


                                //for test
                                //Số nến High > ma20
                                //var lzz = lData15m.Where(x => x.Date < flag.Item2.Date).TakeLast(20);
                                //var countzz = 0;
                                //var countzz_green = 0;
                                //var countCUPMa20 = 0;
                                //var index = 0;
                                //var count10 = 0;
                                //var lavg = new List<decimal>();
                                //double bb_Prev10 = 0, bb_Prev20 = 0;
                                //foreach (var itemzz in lzz)
                                //{
                                //    index++;
                                      
                                //    var bb = lbb.First(x => x.Date == itemzz.Date);
                                //    if (index == 1)
                                //    {
                                //        bb_Prev20 = bb.UpperBand.Value - bb.LowerBand.Value;
                                //    }
                                //    if (itemzz.High > (decimal)bb.Sma.Value)
                                //        countzz++;

                                //    if(itemzz.Close >  (decimal)bb.Sma.Value)
                                //        countCUPMa20++;

                                //    if(itemzz.Close > itemzz.Open)
                                //        countzz_green++;

                                //    if(index >= 10)
                                //    {
                                //        if(index == 10)
                                //            bb_Prev10 = bb.UpperBand.Value - bb.LowerBand.Value; 

                                //        if (itemzz.High > (decimal)bb.Sma.Value)
                                //            count10++;
                                //    }

                                //    var len = Math.Round(100 * (-1 + itemzz.High / itemzz.Low), 2);
                                //    lavg.Add(len);
                                //}
                                //var ratezz = Math.Round(100 * (decimal)countzz / lzz.Count(), 1);
                                //var ratezz10 = Math.Round(100 * (decimal)count10 / 10, 1);
                                //var ratezz_green = Math.Round(100 * (decimal)countzz_green / lzz.Count(), 1);
                                //var ratezz_CUPMa20 = Math.Round(100 * (decimal)countCUPMa20 / lzz.Count(), 1);

                                //var lenSig = Math.Round(100 * (-1 + flag.Item2.High / flag.Item2.Low), 2);
                                //var lenRateSig = Math.Round(lenSig / lavg.Average(), 1);

                                //var lenPivot = Math.Round(100 * (-1 + entity_Pivot.High / entity_Pivot.Low), 2);
                                //var lenRatePivot = Math.Round(lenPivot / lavg.Average(), 1);
                                //var nearRate = Math.Round(lzz.TakeLast(5).Max(x => Math.Round(100 * (-1 + x.High / x.Low), 2)) / lavg.Average(), 1);

                                //var bbPivot = lbb.First(x => x.Date == entity_Pivot.Date);
                                //var bbRate10 = Math.Round((bbPivot.UpperBand.Value - bbPivot.LowerBand.Value) / bb_Prev10, 1);
                                //var bbRate20 = Math.Round((bbPivot.UpperBand.Value - bbPivot.LowerBand.Value) / bb_Prev20, 1);

                                //////////////////////////////////////////////////////////////////////////////
                                var mesItem = $"{sym}|{winloss}|ENTRY: {entity_Pivot.Date.ToString("dd/MM/yyyy HH:mm")}|CLOSE: {eClose.Date.ToString("dd/MM/yyyy HH:mm")}|Rate: {rate}%";
                                //var mesItem = $"{sym}|{winloss}|ENTRY: {flag.Item2.Date.ToString("dd/MM/yyyy HH:mm")}|CLOSE: {eClose.Date.ToString("dd/MM/yyyy HH:mm")}|Rate: {rate}%|zz: {ratezz}%|C: {ratezz_CUPMa20}%|Green: {ratezz_green}%|nearRate: {nearRate}";
                                mesItem = mesItem.Replace("|", ",");
                                Console.WriteLine(mesItem);
                                _COUNT++;
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
                            Sumrate = sumRate,
                            Perate = perRate,
                            Total = count,
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

                return lResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.Bybit_LONG|EXCEPTION| {ex.Message}");
            }

            return null;
        }


        //Lấy các kết quả trả về để PreTest
        public async Task PreTestLONG_DOJI()
        {
            try
            {
                var dt = DateTime.UtcNow;
                var lTake = new List<string>//Doji
                {
                    "DUCKUSDT",
                    "FARTCOINUSDT",
                    "NMRUSDT",
                    "QUICKUSDT",
                    "DEXEUSDT",
                    "FUSDT",
                    "COOKUSDT",
                    "FBUSDT",
                    "KAITOUSDT",
                    "VRUSDT",
                    "NCUSDT",
                    "ZRCUSDT",
                    "VELODROMEUSDT",
                    "BSWUSDT",
                    "BLURUSDT",
                    "HNTUSDT",
                    "ONEUSDT",
                    "SWARMSUSDT",
                    "KAVAUSDT",
                    "HYPEUSDT",
                    "BRUSDT",
                };
                //lTake.Sync(EExchange.Bybit, OrderSide.Buy, EOptionTrade.Doji, _symRepo);
                var lRank = new List<clsShow>();
                foreach (var s in lTake)
                {
                    try
                    {
                        var res20 = await Bybit_LONG_DOJI(s, 20);
                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{s}| {ex.Message}");
                    }
                }
                var totalMinute = (DateTime.UtcNow - dt).TotalMinutes;
                Console.WriteLine($"TotalTime: {totalMinute}| COUNT: {_COUNT}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        //Tìm danh sách các Coin có tỉ lệ winrate tốt nhất
        public async Task ListLONG_DOJI()
        {
            try
            {
                var dt = DateTime.UtcNow;
                var lAll = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, limit: 1000);
                var lUsdt = lAll.Data.List.Where(x => x.QuoteAsset == "USDT" && !x.Name.StartsWith("1000")).Select(x => x.Name);
                var lTake = lUsdt.Skip(400).Take(400);
                var lRank = new List<clsShow>();

                foreach (var s in lTake)
                {
                    try
                    {
                        var res180 = await Bybit_LONG_DOJI(s, 180);
                        var res20 = await Bybit_LONG_DOJI(s, 20);
                        var res60 = await Bybit_LONG_DOJI(s, 60, 20);
                        var res90 = await Bybit_LONG_DOJI(s, 90, 60);
                        var res150 = await Bybit_LONG_DOJI(s, 150, 90);
                        Thread.Sleep(1000);

                        var total = res20.First().Total
                                    + res60.First().Total
                                    + res90.First().Total
                                    + res150.First().Total
                                    + res180.First().Total;

                        var win = res20.First().Win
                                  + res60.First().Win
                                  + res90.First().Win
                                  + res150.First().Win
                                  + res180.First().Win;

                        var winrate = Math.Round((double)win / total, 2);
                        var per = res20.First().Perate
                                 + res60.First().Perate
                                 + res90.First().Perate
                                 + res150.First().Perate
                                 + res180.First().Perate;
                        var perRate = Math.Round((double)per / 5, 2);

                        lRank.Add(new clsShow
                        {
                            s = s,
                            Win = win,
                            Total = total,
                            Winrate = winrate,
                            PerRate = perRate
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{s}| {ex.Message}");
                    }
                }
                Console.WriteLine("///////////////////////////////////////////////");
                Console.WriteLine("///////////////////////////////////////////////");
                Console.WriteLine("///////////////////////////////////////////////");
                foreach (var item in lRank.OrderByDescending(x => x.PerRate).ThenByDescending(x => x.Winrate).ThenByDescending(x => x.Total))
                {
                    Console.WriteLine($"{item.s}, W/Total: {item.Win}/{item.Total} = {item.Winrate}%, Per: {item.PerRate}%");
                }

                var totalMinute = (DateTime.UtcNow - dt).TotalMinutes;
                Console.WriteLine($"TotalTime: {totalMinute}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public async Task<List<clsResult>> Bybit_LONG_DOJI(string s = "", int DAY = 20, int SKIP_DAY = 0)
        {
            try
            {
                //var DAY = 150;
                int HOUR = 8;
                var start = DateTime.UtcNow;
                var exchange = (int)EExchange.Bybit;
                var builder = Builders<Symbol>.Filter;
                var lSym = _symRepo.GetByFilter(builder.And(
                    builder.Eq(x => x.ex, exchange),
                    builder.Eq(x => x.ty, (int)OrderSide.Buy),
                    builder.Eq(x => x.status, 0)
                ));
                decimal SL_RATE = 3m;
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
                        var lData15m = await GetData(sym, DAY, SKIP_DAY);
                        dic.Add(sym, lData15m);
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

                                var flag = lData15m.Where(x => x.Date <= cur.Date).ToList().IsFlagBuy_Doji();
                                if (!flag.Item1)
                                    continue;

                                var entity_Pivot = flag.Item2;
                                var entity_Sig = lData15m.Last(x => x.Date < entity_Pivot.Date);
                                var bb_Pivot = lbb.First(x => x.Date == entity_Pivot.Date);

                                var lClose = lData15m.Where(x => x.Date > entity_Pivot.Date && x.Date <= entity_Pivot.Date.AddHours(HOUR));
                                var closeCount = lClose.Count();
                                var isEnd = closeCount == HOUR * 4;

                                var isChotNon = false;
                                Quote eClose = null;
                                foreach (var itemClose in lClose)
                                {
                                    //if ((itemClose.Date - entity_Pivot.Date).TotalHours >= 1
                                    //    && itemClose.Close < entity_Pivot.Close
                                    //    && itemClose.Close > entity_Sig.Open)
                                    //{
                                    //    eClose = itemClose;
                                    //    break;
                                    //}
                                    var ma = lbb.First(x => x.Date == itemClose.Date);
                                    if (itemClose.High > (decimal)ma.UpperBand)//do something
                                    {
                                        itemClose.Close = (decimal)ma.UpperBand;
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

                                    if (itemClose.Close >= (decimal)ma.Sma.Value)
                                    {
                                        isChotNon = true;
                                    }

                                    var rateH = Math.Round(100 * (-1 + itemClose.High / entity_Pivot.Close), 1);
                                    if (rateH >= flag.Item2.Rate_TP)
                                    {
                                        var close = entity_Pivot.Close * (decimal)(1 + flag.Item2.Rate_TP / 100);
                                        itemClose.Close = close;
                                        eClose = itemClose;
                                        break;
                                    }
                                    var rateL = Math.Round(100 * (-1 + itemClose.Low / entity_Pivot.Close), 1);
                                    if (rateL <= -SL_RATE)
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

                                //////////////////////////////////////////////////////////////////////////////
                                var mesItem = $"{sym}|{winloss}|ENTRY: {entity_Pivot.Date.ToString("dd/MM/yyyy HH:mm")}|CLOSE: {eClose.Date.ToString("dd/MM/yyyy HH:mm")}|Rate: {rate}%";
                                //var mesItem = $"{sym}|{winloss}|ENTRY: {flag.Item2.Date.ToString("dd/MM/yyyy HH:mm")}|CLOSE: {eClose.Date.ToString("dd/MM/yyyy HH:mm")}|Rate: {rate}%|zz: {ratezz}%|C: {ratezz_CUPMa20}%|Green: {ratezz_green}%|nearRate: {nearRate}";
                                mesItem = mesItem.Replace("|", ",");
                                Console.WriteLine(mesItem);
                                _COUNT++;
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
                            Sumrate = sumRate,
                            Perate = perRate,
                            Total = count,
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

                return lResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.Bybit_SHORT|EXCEPTION| {ex.Message}");
            }

            return null;
        }

        
        //Lấy các kết quả trả về để PreTest
        public async Task PreTestSHORT()
        {
            try
            {
                var dt = DateTime.UtcNow;
                var lTake = new List<string>//Origin
                {
                    "ZEREBROUSDT",
                    "CPOOLUSDT",
                    "FLMUSDT",
                    "ETHFIUSDT",
                    "ALUUSDT",
                    "EIGENUSDT",
                    "ALEOUSDT",
                    "ETHWUSDT",
                    "MASAUSDT",
                    "FARTCOINUSDT",
                    "FUELUSDT",
                    "DEGENUSDT",
                    "ELXUSDT",
                    "ACXUSDT",
                    "AKTUSDT",
                    "MNTUSDT",
                    "FORMUSDT",
                    "LOOKSUSDT",
                    "MAGICUSDT",
                    "PLUMEUSDT",
                    "BADGERUSDT",
                    "AVLUSDT",
                    "AUCTIONUSDT",
                    "DEXEUSDT",
                    "LTCUSDT",
                    "GRASSUSDT",
                    "ARPAUSDT",
                    "JUSDT",
                    "TIAUSDT",
                    "THEUSDT",
                    "UMAUSDT",
                    "PRIMEUSDT",
                    "SHELLUSDT",
                    "CELOUSDT",
                    "MASKUSDT",
                    "HMSTRUSDT",
                    "PEAQUSDT",
                    "POLUSDT",
                    "SENDUSDT",
                    "DYMUSDT",
                    "CLOUDUSDT",
                    "ANKRUSDT",
                    "AXLUSDT",
                    "SIRENUSDT",
                    "XEMUSDT",
                    "VINEUSDT",
                    "XVGUSDT",
                    "FORTHUSDT",
                    "IMXUSDT",
                    "OLUSDT",
                    "SERAPHUSDT",
                    "DYDXUSDT",
                    "MANTAUSDT",
                    "SONICUSDT",
                    "XNOUSDT",
                    "XTERUSDT",
                    "SSVUSDT",
                    "SPELLUSDT",
                    "WAXPUSDT",
                    "ICXUSDT",
                    "ETCUSDT",
                    "DOGUSDT",
                    "DUCKUSDT",
                };
                //lTake.Sync(EExchange.Bybit, OrderSide.Sell, EOptionTrade.Normal, _symRepo);
                var lRank = new List<clsShow>();

                foreach (var s in lTake)
                {
                    try
                    {
                        var res20 = await Bybit_SHORT(s, 20);
                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{s}| {ex.Message}");
                    }
                }
                var totalMinute = (DateTime.UtcNow - dt).TotalMinutes;
                Console.WriteLine($"TotalTime: {totalMinute}| COUNT: {_COUNT}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        //Tìm danh sách các Coin có tỉ lệ winrate tốt nhất
        public async Task ListShort()
        {
            try
            {
                var dt = DateTime.UtcNow;
                var lAll = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, limit: 1000);
                var lUsdt = lAll.Data.List.Where(x => x.QuoteAsset == "USDT" && !x.Name.StartsWith("1000")).Select(x => x.Name);
                var lTake = lUsdt.Skip(0).Take(100);
                var lRank = new List<clsShow>();

                foreach (var s in lTake)
                {
                    try
                    {
                        var res180 = await Bybit_SHORT(s, 180);
                        var res20 = await Bybit_SHORT(s, 20);
                        var res60 = await Bybit_SHORT(s, 60, 20);
                        var res90 = await Bybit_SHORT(s, 90, 60);
                        var res150 = await Bybit_SHORT(s, 150, 90);
                        Thread.Sleep(1000);

                        var total = res20.First().Total
                                    + res60.First().Total
                                    + res90.First().Total
                                    + res150.First().Total
                                    + res180.First().Total;

                        var win = res20.First().Win
                                  + res60.First().Win
                                  + res90.First().Win
                                  + res150.First().Win
                                  + res180.First().Win;

                        var winrate = Math.Round((double)win / total, 2);
                        var per = res20.First().Perate
                                 + res60.First().Perate
                                 + res90.First().Perate
                                 + res150.First().Perate
                                 + res180.First().Perate;
                        var perRate = Math.Round((double)per / 5, 2);

                        lRank.Add(new clsShow
                        {
                            s = s,
                            Win = win,
                            Total = total,
                            Winrate = winrate,
                            PerRate = perRate
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{s}| {ex.Message}");
                    }
                }
                Console.WriteLine("///////////////////////////////////////////////");
                Console.WriteLine("///////////////////////////////////////////////");
                Console.WriteLine("///////////////////////////////////////////////");
                foreach (var item in lRank.OrderByDescending(x => x.PerRate).ThenByDescending(x => x.Winrate).ThenByDescending(x => x.Total))
                {
                    Console.WriteLine($"{item.s}, W/Total: {item.Win}/{item.Total} = {item.Winrate}%, Per: {item.PerRate}%");
                }

                var totalMinute = (DateTime.UtcNow - dt).TotalMinutes;
                Console.WriteLine($"TotalTime: {totalMinute}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public async Task<List<clsResult>> Bybit_SHORT(string s = "", int DAY = 20, int SKIP_DAY = 0)
        {
            try
            {
                //var DAY = 150;
                int HOUR = 8;
                var start = DateTime.UtcNow;
                var exchange = (int)EExchange.Bybit;
                var builder = Builders<Symbol>.Filter;
                var lSym = _symRepo.GetByFilter(builder.And(
                    builder.Eq(x => x.ex, exchange),
                    builder.Eq(x => x.ty, (int)OrderSide.Sell),
                    builder.Eq(x => x.status, 0)
                ));
                decimal SL_RATE = 3m;
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
                        var lData15m = await GetData(sym, DAY, SKIP_DAY);
                        dic.Add(sym, lData15m);
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

                                var flag = lData15m.Where(x => x.Date < cur.Date).ToList().IsFlagSell();
                                if (!flag.Item1)
                                    continue;

                                var entity_Pivot = flag.Item2;
                                var bb_Pivot = lbb.First(x => x.Date == entity_Pivot.Date);

                                var entity_Sig = lData15m.Last(x => x.Date < entity_Pivot.Date);

                                var rateBB = (decimal)(Math.Round(100 * (-1 + bb_Pivot.UpperBand.Value / bb_Pivot.LowerBand.Value)) - 1);
                                if (rateBB > rateProfit_Max)
                                {
                                    rateBB = rateProfit_Max;
                                }
                                else if (rateBB < rateProfit_Min)
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
                                    if ((itemClose.Date - entity_Pivot.Date).TotalHours >= 1
                                        && itemClose.Close < entity_Pivot.Close
                                        && itemClose.Close > Math.Min(entity_Pivot.Open, entity_Pivot.Close))
                                    {
                                        eClose = itemClose;
                                        break;
                                    }
                                    var ma = lbb.First(x => x.Date == itemClose.Date);
                                    if (itemClose.Low < (decimal)ma.LowerBand)//do something
                                    {
                                        itemClose.Close = (decimal)ma.LowerBand;
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

                                    if (itemClose.Close <= (decimal)ma.Sma.Value)
                                    {
                                        isChotNon = true;
                                    }

                                    var rateH = Math.Round(100 * (-1 + entity_Pivot.Close / itemClose.Low), 1);
                                    if (rateH >= rateBB)
                                    {
                                        var close = entity_Pivot.Close * (decimal)(1 - rateBB / 100);
                                        itemClose.Close = close;
                                        eClose = itemClose;
                                        break;
                                    }
                                    var rateL = Math.Round(100 * (-1 + entity_Pivot.Close / itemClose.High), 1);
                                    if (rateL <= -SL_RATE)
                                    {
                                        var close = entity_Pivot.Close * (decimal)(1 + SL_RATE / 100);
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
                                var rate = Math.Round(100 * (-1 + entity_Pivot.Close / eClose.Close), 2);
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


                                //for test
                                //Số nến High > ma20
                                //var lzz = lData15m.Where(x => x.Date < flag.Item2.Date).TakeLast(20);
                                //var countzz = 0;
                                //var countzz_green = 0;
                                //var countCUPMa20 = 0;
                                //var index = 0;
                                //var count10 = 0;
                                //var lavg = new List<decimal>();
                                //double bb_Prev10 = 0, bb_Prev20 = 0;
                                //foreach (var itemzz in lzz)
                                //{
                                //    index++;

                                //    var bb = lbb.First(x => x.Date == itemzz.Date);
                                //    if (index == 1)
                                //    {
                                //        bb_Prev20 = bb.UpperBand.Value - bb.LowerBand.Value;
                                //    }
                                //    if (itemzz.High > (decimal)bb.Sma.Value)
                                //        countzz++;

                                //    if(itemzz.Close >  (decimal)bb.Sma.Value)
                                //        countCUPMa20++;

                                //    if(itemzz.Close > itemzz.Open)
                                //        countzz_green++;

                                //    if(index >= 10)
                                //    {
                                //        if(index == 10)
                                //            bb_Prev10 = bb.UpperBand.Value - bb.LowerBand.Value; 

                                //        if (itemzz.High > (decimal)bb.Sma.Value)
                                //            count10++;
                                //    }

                                //    var len = Math.Round(100 * (-1 + itemzz.High / itemzz.Low), 2);
                                //    lavg.Add(len);
                                //}
                                //var ratezz = Math.Round(100 * (decimal)countzz / lzz.Count(), 1);
                                //var ratezz10 = Math.Round(100 * (decimal)count10 / 10, 1);
                                //var ratezz_green = Math.Round(100 * (decimal)countzz_green / lzz.Count(), 1);
                                //var ratezz_CUPMa20 = Math.Round(100 * (decimal)countCUPMa20 / lzz.Count(), 1);

                                //var lenSig = Math.Round(100 * (-1 + flag.Item2.High / flag.Item2.Low), 2);
                                //var lenRateSig = Math.Round(lenSig / lavg.Average(), 1);

                                //var lenPivot = Math.Round(100 * (-1 + entity_Pivot.High / entity_Pivot.Low), 2);
                                //var lenRatePivot = Math.Round(lenPivot / lavg.Average(), 1);
                                //var nearRate = Math.Round(lzz.TakeLast(5).Max(x => Math.Round(100 * (-1 + x.High / x.Low), 2)) / lavg.Average(), 1);

                                //var bbPivot = lbb.First(x => x.Date == entity_Pivot.Date);
                                //var bbRate10 = Math.Round((bbPivot.UpperBand.Value - bbPivot.LowerBand.Value) / bb_Prev10, 1);
                                //var bbRate20 = Math.Round((bbPivot.UpperBand.Value - bbPivot.LowerBand.Value) / bb_Prev20, 1);

                                //////////////////////////////////////////////////////////////////////////////
                                var mesItem = $"{sym}|{winloss}|ENTRY: {entity_Pivot.Date.ToString("dd/MM/yyyy HH:mm")}|CLOSE: {eClose.Date.ToString("dd/MM/yyyy HH:mm")}|Rate: {rate}%";
                                //var mesItem = $"{sym}|{winloss}|ENTRY: {flag.Item2.Date.ToString("dd/MM/yyyy HH:mm")}|CLOSE: {eClose.Date.ToString("dd/MM/yyyy HH:mm")}|Rate: {rate}%|zz: {ratezz}%|C: {ratezz_CUPMa20}%|Green: {ratezz_green}%|nearRate: {nearRate}";
                                mesItem = mesItem.Replace("|", ",");
                                Console.WriteLine(mesItem);
                                _COUNT++;
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
                            Sumrate = sumRate,
                            Perate = perRate,
                            Total = count,
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

                return lResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.Bybit_SHORT|EXCEPTION| {ex.Message}");
            }

            return null;
        }


        //Lấy các kết quả trả về để PreTest
        public async Task PreTestSHORT_DOJI()
        {
            try
            {
                var dt = DateTime.UtcNow;
                var lTake = new List<string>//Doji
                {
                    "ALUUSDT",
                    "AVAAIUSDT",
                    "REXUSDT",
                    "SXPUSDT",
                    "CGPTUSDT",
                    "TUSDT",
                    "MEMEUSDT",
                    "MYROUSDT",
                    "FIOUSDT",
                    "BRETTUSDT",
                    "COOKIEUSDT",
                    "ZEUSUSDT",
                    "EPICUSDT",
                    "VTHOUSDT",
                    "XEMUSDT",
                    "VVVUSDT",
                    "ZBCNUSDT",
                    "PROMUSDT",
                    "RAREUSDT",
                    "PEAQUSDT",
                    "COOKUSDT",
                    "ALEOUSDT",
                    "MVLUSDT",
                    "LOOKSUSDT",
                    "AXSUSDT",
                    "ENAUSDT",
                    "ARKMUSDT",
                    "ALCHUSDT",
                    "SHELLUSDT",
                    "MUBARAKUSDT",
                    "ATOMUSDT",
                    "FORTHUSDT",
                    "CVXUSDT",
                    "DOGUSDT",
                    "TIAUSDT",
                    "VANAUSDT",
                    "SPELLUSDT",
                    "HMSTRUSDT",
                    "LUMIAUSDT",
                    "MELANIAUSDT"
                };
                //lTake.Sync(EExchange.Bybit, OrderSide.Sell, EOptionTrade.Doji, _symRepo);
                var lRank = new List<clsShow>();
                foreach (var s in lTake)
                {
                    try
                    {
                        var res20 = await Bybit_SHORT_DOJI(s, 20);
                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{s}| {ex.Message}");
                    }
                }
                var totalMinute = (DateTime.UtcNow - dt).TotalMinutes;
                Console.WriteLine($"TotalTime: {totalMinute}| COUNT: {_COUNT}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        //Tìm danh sách các Coin có tỉ lệ winrate tốt nhất
        public async Task ListShort_DOJI()
        {
            try
            {
                var dt = DateTime.UtcNow;
                var lAll = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, limit: 1000);
                var lUsdt = lAll.Data.List.Where(x => x.QuoteAsset == "USDT" && !x.Name.StartsWith("1000")).Select(x => x.Name);
                var lTake = lUsdt.Skip(400).Take(400);
                var lRank = new List<clsShow>();

                foreach (var s in lTake)
                {
                    try
                    {
                        var res180 = await Bybit_SHORT_DOJI(s, 180);
                        var res20 = await Bybit_SHORT_DOJI(s, 20);
                        var res60 = await Bybit_SHORT_DOJI(s, 60, 20);
                        var res90 = await Bybit_SHORT_DOJI(s, 90, 60);
                        var res150 = await Bybit_SHORT_DOJI(s, 150, 90);
                        Thread.Sleep(1000);

                        var total = res20.First().Total
                                    + res60.First().Total
                                    + res90.First().Total
                                    + res150.First().Total
                                    + res180.First().Total;

                        var win = res20.First().Win
                                  + res60.First().Win
                                  + res90.First().Win
                                  + res150.First().Win
                                  + res180.First().Win;

                        var winrate = Math.Round((double)win / total, 2);
                        var per = res20.First().Perate
                                 + res60.First().Perate
                                 + res90.First().Perate
                                 + res150.First().Perate
                                 + res180.First().Perate;
                        var perRate = Math.Round((double)per / 5, 2);

                        lRank.Add(new clsShow
                        {
                            s = s,
                            Win = win,
                            Total = total,
                            Winrate = winrate,
                            PerRate = perRate
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{s}| {ex.Message}");
                    }
                }
                Console.WriteLine("///////////////////////////////////////////////");
                Console.WriteLine("///////////////////////////////////////////////");
                Console.WriteLine("///////////////////////////////////////////////");
                foreach (var item in lRank.OrderByDescending(x => x.PerRate).ThenByDescending(x => x.Winrate).ThenByDescending(x => x.Total))
                {
                    Console.WriteLine($"{item.s}, W/Total: {item.Win}/{item.Total} = {item.Winrate}%, Per: {item.PerRate}%");
                }

                var totalMinute = (DateTime.UtcNow - dt).TotalMinutes;
                Console.WriteLine($"TotalTime: {totalMinute}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public async Task<List<clsResult>> Bybit_SHORT_DOJI(string s = "", int DAY = 20, int SKIP_DAY = 0)
        {
            try
            {
                //var DAY = 150;
                int HOUR = 8;
                var start = DateTime.UtcNow;
                var exchange = (int)EExchange.Bybit;
                var builder = Builders<Symbol>.Filter;
                var lSym = _symRepo.GetByFilter(builder.And(
                    builder.Eq(x => x.ex, exchange),
                    builder.Eq(x => x.ty, (int)OrderSide.Sell),
                    builder.Eq(x => x.status, 0)
                ));
                decimal SL_RATE = 3m;
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
                        var lData15m = await GetData(sym, DAY, SKIP_DAY);
                        dic.Add(sym, lData15m);
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

                                var flag = lData15m.Where(x => x.Date <= cur.Date).ToList().IsFlagSell_Doji();
                                if (!flag.Item1)
                                    continue;

                                var entity_Pivot = flag.Item2;
                                var entity_Sig = lData15m.Last(x => x.Date < entity_Pivot.Date);
                                var bb_Pivot = lbb.First(x => x.Date == entity_Pivot.Date);

                                var lClose = lData15m.Where(x => x.Date > entity_Pivot.Date && x.Date <= entity_Pivot.Date.AddHours(HOUR));
                                var closeCount = lClose.Count();
                                var isEnd = closeCount == HOUR * 4;

                                var isChotNon = false;
                                Quote eClose = null;
                                foreach (var itemClose in lClose)
                                {
                                    //if ((itemClose.Date - entity_Pivot.Date).TotalHours >= 1
                                    //    && itemClose.Close < entity_Pivot.Close
                                    //    && itemClose.Close > entity_Sig.Open)
                                    //{
                                    //    eClose = itemClose;
                                    //    break;
                                    //}
                                    var ma = lbb.First(x => x.Date == itemClose.Date);
                                    if (itemClose.Low < (decimal)ma.LowerBand)//do something
                                    {
                                        itemClose.Close = (decimal)ma.LowerBand;
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

                                    if (itemClose.Close <= (decimal)ma.Sma.Value)
                                    {
                                        isChotNon = true;
                                    }

                                    var rateH = Math.Round(100 * (-1 + entity_Pivot.Close / itemClose.Low), 1);
                                    if (rateH >= flag.Item2.Rate_TP)
                                    {
                                        var close = entity_Pivot.Close * (decimal)(1 - flag.Item2.Rate_TP / 100);
                                        itemClose.Close = close;
                                        eClose = itemClose;
                                        break;
                                    }
                                    var rateL = Math.Round(100 * (-1 + entity_Pivot.Close / itemClose.High), 1);
                                    if (rateL <= -SL_RATE)
                                    {
                                        var close = entity_Pivot.Close * (decimal)(1 + SL_RATE / 100);
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
                                var rate = Math.Round(100 * (-1 + entity_Pivot.Close / eClose.Close), 2);
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

                                //////////////////////////////////////////////////////////////////////////////
                                var mesItem = $"{sym}|{winloss}|ENTRY: {entity_Pivot.Date.ToString("dd/MM/yyyy HH:mm")}|CLOSE: {eClose.Date.ToString("dd/MM/yyyy HH:mm")}|Rate: {rate}%";
                                //var mesItem = $"{sym}|{winloss}|ENTRY: {flag.Item2.Date.ToString("dd/MM/yyyy HH:mm")}|CLOSE: {eClose.Date.ToString("dd/MM/yyyy HH:mm")}|Rate: {rate}%|zz: {ratezz}%|C: {ratezz_CUPMa20}%|Green: {ratezz_green}%|nearRate: {nearRate}";
                                mesItem = mesItem.Replace("|", ",");
                                Console.WriteLine(mesItem);
                                _COUNT++;
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
                            Sumrate = sumRate,
                            Perate = perRate,
                            Total = count,
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

                return lResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.Bybit_SHORT|EXCEPTION| {ex.Message}");
            }

            return null;
        }

        public async Task CheckWycKoff()
        {
            try
            {
                var dt = DateTime.UtcNow;
                var lAll = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, limit: 1000);
                var lUsdt = lAll.Data.List.Where(x => x.QuoteAsset == "USDT" && !x.Name.StartsWith("1000")).Select(x => x.Name);
                //var lTake = lUsdt.Skip(0).Take(50);
                var lTake = new List<string>
                {
                    "ETHUSDT"
                };
                /*
                 
                 */
                foreach (var item in lTake)
                {
                    try
                    {
                        //var l1H = await _apiService.GetData_Bybit_1H(item);
                        var l1H = await GetData(item, 20, 0);
                        var count = l1H.Count();
                        var timeFlag = DateTime.MinValue;
                        for (int i = 100; i < count; i++)
                        {
                            var lDat = l1H.Take(i).ToList();
                            var last = lDat.Last();
                            if (last.Date < timeFlag)
                                continue;

                            var rs = lDat.IsWyckoff_20250726();
                            if (rs.Item1)
                            {
                                if (rs.Item2.Date < timeFlag)
                                    continue;
                                Console.WriteLine($"{item}|SOS: {rs.Item2.Date.ToString("dd/MM/yyyy HH:mm")}|ENTRY: {rs.Item3.Date.ToString("dd/MM/yyyy HH:mm")}");
                                timeFlag = rs.Item3.Date;

                                for (int j = 1; j < 100; j++)
                                {
                                    var res = rs.Item3.IsWyckoffOut(l1H.Take(i + j));
                                    if (res.Item1)
                                    {
                                        var rate = Math.Round(100 * (-1 + res.Item2.Open / rs.Item3.Close), 2);
                                        Console.WriteLine($"{item}|ENTRY: {rs.Item3.Date.ToString("dd/MM/yyyy HH:mm")}|TP: {res.Item2.Date.ToString("dd/MM/yyyy HH:mm")}|Rate: {rate}%");
                                        break;
                                    }

                                }
                            }
                            //if(rs.Item1 && rs.Item2.Date > timeFlag)
                            //{
                            //    timeFlag = rs.Item2.Date;

                            //    for (int i2 = 0; i2 < count; i2++)
                            //    {
                            //        var check = l1H.Where(x => x.Date > timeFlag).Skip(i2).FirstOrDefault();
                            //        if(check is null)
                            //            continue;

                            //        var lCheck = l1H.Where(x => x.Date <= check.Date);
                            //        var sell = rs.Item2.IsWyckoffOut(lCheck);
                            //        if(sell.Item1)
                            //        {
                            //            var rate = Math.Round(100 * (-1 + sell.Item2.Close / rs.Item2.Close));
                            //            var winlose = rate > 0 ? "W" : "L";

                            //            Console.WriteLine($"{item}|{winlose}|SOS: {rs.Item3.Date.ToString("dd/MM/yyyy HH")}|ENTRY: {rs.Item2.Date.ToString("dd/MM/yyyy HH")}|TP: {sell.Item2.Date.ToString("dd/MM/yyyy HH")}|ANGLE: {rs.Item4}|Rate: {rate}%");
                            //            break;
                            //        }
                            //    }
                                
                            //    //Console.WriteLine($"{item}: {rs.Item2.Date.ToString("dd/MM/yyyy HH:mm")}");
                            //}
                        }

                        //var res1H = l1H.IsWyckoff(20, 1);

                        //var lMes = new List<string>();
                        //var lData15m = await GetData(item, 20, 0);
                        //var res = lData15m.IsWyckoff();
                        //if (res.Item1)
                        //{
                        //    foreach (var itemWyc in res.Item2)
                        //    {
                        //        Console.WriteLine($"{item}: {itemWyc.Date.ToString("dd/MM/yyyy")}");
                        //    }
                        //}
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

        public class clsData
        {
            public string s { get; set; }
            public DateTime Date { get; set; }
            public decimal Rate { get; set; }
        }  
    }

    public static class ExMethod
    {
        public static void Sync(this List<string> lDat, EExchange ex, OrderSide side, EOptionTrade op, ISymbolRepo symRepo)
        {
            try
            {
                var builder = Builders<Symbol>.Filter;
                symRepo.DeleteMany(builder.And(
                    builder.Eq(x => x.ex, (int)ex),
                    builder.Eq(x => x.ty, (int)side),
                    builder.Eq(x => x.op, (int)op)
                ));
                var index = 1;
                foreach (var item in lDat)
                {
                    symRepo.InsertOne(new Symbol
                    {
                        ex = (int)ex,
                        ty = (int)side,
                        op = (int)op,
                        s = item,
                        rank = index++
                    });
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    public class clsResult
    {
        public string s { get; set; }
        public int Win { get; set; }
        public double Winrate { get; set; }
        public decimal Sumrate { get; set; }
        public double Perate { get; set; }
        public int Total { get; set; }
        public string Mes { get; set; }
    }

    public class clsShow
    {
        public string s { get; set; }
        public int Win { get; set; }
        public int Total { get; set; }
        public double Winrate { get; set; }
        public double PerRate { get; set; }
    }
}



