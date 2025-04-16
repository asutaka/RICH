using Skender.Stock.Indicators;
using StockPr.DAL;
using StockPr.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace StockPr.Service
{
    public interface ITestService
    {
        Task Check2Buy();
    }
    public class TestService : ITestService
    {
        private readonly ILogger<TestService> _logger;
        private readonly IAPIService _apiService;
        private readonly IStockRepo _stockRepo;
        public TestService(ILogger<TestService> logger, IAPIService apiService, IStockRepo stockRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _stockRepo = stockRepo;
        }
       
        public async Task Check2Buy()
        {
            try
            {
                var lStock = _stockRepo.GetAll();
                var lUsdt = lStock.Select(x => x.s);
                var countUSDT = lUsdt.Count();//461
                var lTake = lUsdt.ToList();
                //var lTake = lUsdt.Skip(0).Take(50).ToList();
                //2x1.7 best
                //decimal SL_RATE = 1.7m;//1.5,1.6,1.8,1.9,2
                decimal SL_RATE = 7m;//1.5,1.6,1.8,1.9,2
                int hour = 7;//1h,2h,3h,4h

                var lMesAll = new List<string>();
                var lModel = new List<LongMa20>();

                var winTotal = 0;
                var lossTotal = 0;
                lTake.Clear();
                var lTmp = new List<string>
                {
                    "DPG"
                };
                lTake.AddRange(lTmp);
                foreach (var item in lTake)
                {
                    var winCount = 0;
                    var lossCount = 0;
                    try
                    {
                        var lMes = new List<string>();

                        var lData15m = await _apiService.SSI_GetDataStock(item);
                        Thread.Sleep(200);
                        if (lData15m == null || !lData15m.Any() || lData15m.Count() < 250 || lData15m.Last().Volume < 50000)
                            continue;
                        var last = lData15m.Last();
                        var lbb = lData15m.GetBollingerBands();
                        var lrsi = lData15m.GetRsi();

                        var countRSI = lrsi.Where(x => x.Rsi != null && x.Rsi <= 40);
                        Console.WriteLine($"{countRSI}");
                        foreach (var rsi in countRSI)
                        {
                            Console.WriteLine(rsi.Date.ToString("dd/MM/yyyy"));
                        }

                        DateTime dtFlag = DateTime.MinValue;
                        //var count = 0;
                        foreach (var ma20 in lbb)
                        {
                            try
                            {
                                //if (dtFlag >= ma20.Date)
                                //    continue;

                                //if (ma20.Date.Month == 1 && ma20.Date.Day == 7 && ma20.Date.Year == 2025)
                                //{
                                //    var z = 1;
                                //}

                                var side = 0;
                                var cur = lData15m.First(x => x.Date == ma20.Date);
                                var rsi = lrsi.First(x => x.Date == ma20.Date);
                                var maxOpenClose = Math.Max(cur.Open, cur.Close);
                                var minOpenClose = Math.Min(cur.Open, cur.Close);

                                if (cur.Close >= cur.Open
                                    || ma20.Sma is null
                                    || rsi.Rsi > 35
                                    //|| cur.Low >= (decimal)ma20.LowerBand.Value
                                    || Math.Abs(minOpenClose - (decimal)ma20.LowerBand.Value) > Math.Abs((decimal)ma20.Sma.Value - minOpenClose)
                                    )
                                    continue;

                                var rsiPivot = lrsi.FirstOrDefault(x => x.Date > ma20.Date);
                                if (rsiPivot is null || rsiPivot.Rsi > 35 || rsiPivot.Rsi < 25)
                                    continue;

                                var pivot = lData15m.First(x => x.Date > ma20.Date);
                                var bbPivot = lbb.First(x => x.Date > ma20.Date);
                                if (pivot.High >= (decimal)bbPivot.Sma.Value
                                    //|| pivot.Low >= (decimal)bbPivot.LowerBand.Value
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

                                var next = lData15m.FirstOrDefault(x => x.Date > cur.Date);
                                if (next is null)
                                    continue;
                                var rateEntry = Math.Round(100 * (-1 + next.Low / cur.Close), 1);// tỉ lệ từ entry đến giá thấp nhất

                                var eEntry = cur;
                                var eClose = lData15m.FirstOrDefault(x => x.Date >= eEntry.Date.AddDays(hour));
                                if (eClose is null)
                                    continue;

                                var lClose = lData15m.Where(x => x.Date > eEntry.Date && x.Date <= eEntry.Date.AddDays(hour));
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
                                var lRange = lData15m.Where(x => x.Date > eEntry.Date && x.Date <= eClose.Date);
                                var maxH = lRange.Max(x => x.High);
                                var minL = lRange.Min(x => x.Low);

                                var winloss = "W";
                                if (rate <= (decimal)0)
                                {
                                    winloss = "L";
                                }

                                decimal maxTP = 0, maxSL = 0;
                                if (side == 0)
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
                                var mes = $"{item}|{winloss}|BUY|{cur.Date.ToString("dd/MM/yyyy HH:mm")}|{rate}%|TPMax: {maxTP}%|SLMax: {maxSL}%|RateEntry: {rateEntry}%|RSI: {rsiPivot.Rsi}";
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
                        //if (winCount <= lossCount)
                        //    continue;
                        if (winCount + lossCount <= 0)
                            continue;

                        var rateRes = Math.Round(((decimal)winCount / (winCount + lossCount)), 2);
                        if (rateRes > (decimal)0.5)
                        {
                            var sumRate = lModel.Where(x => x.s == item).Sum(x => x.Rate);
                            //if (sumRate <= 1)
                            //{
                            //    var lRemove = lModel.Where(x => x.s == item);
                            //    lModel = lModel.Except(lRemove).ToList();
                            //    continue;
                            //}
                            //Console.WriteLine($"{item}: {rateRes}({winCount}/{lossCount})");
                            lMesAll.AddRange(lMes);
                            foreach (var mes in lMes)
                            {
                                Console.WriteLine(mes);
                            }
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
