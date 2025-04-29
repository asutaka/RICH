using Microsoft.AspNetCore.Mvc.Razor.Infrastructure;
using Skender.Stock.Indicators;
using StockPr.DAL;
using System.Net.WebSockets;

namespace StockPr.Service
{
    public interface ITestService
    {
        Task Check2Buy();
        Task Check2Sell();
        Task CheckSomething();
        Task CheckCurrentDay();
        Task CheckAllDay();
        Task CheckAllDay_OnlyVolume();
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
                decimal SL_RATE = 10m;//1.5,1.6,1.8,1.9,2
                int hour = 10;//1h,2h,3h,4h

                var lMesAll = new List<string>();
                var lModel = new List<LongMa20>();

                var winTotal = 0;
                var lossTotal = 0;
                lTake.Clear();
                var lTmp = new List<string>
                {
                    "DC4",
                    "GIL",
                    "GVR",
                    "DPG",
                    "CTG",
                    "BFC",
                    "VRE",
                    "PVB",
                    "GEX",
                    "SZC",
                    "HDG",
                    "BMP",
                    "TLG",
                    "VPB",
                    "DIG",
                    "KBC",
                    "HSG",
                    "PET",
                    "TNG",
                    "SBT",
                    "MSH",
                    "NAB",
                    "VGC",
                    "CSV",
                    "VCS",
                    "CSM",
                    "PHR",
                    "PVT",
                    "PC1",
                    "ASM",
                    "LAS",
                    "DXG",
                    "HCM",
                    "CTI",
                    "NHA",
                    "DPR",
                    "ANV",
                    "OCB",
                    "TVB",
                    "STB",
                    "HDC",
                    "POW",
                    "VSC",
                    "L18",
                    "DDV",
                    "VCI",
                    "GMD",
                    "NTP",
                    "KSV",
                    "TTF",
                    "NT2",
                    "TCM",
                    "LSS",
                    "GEG",
                    "HHS",
                    "MSB",
                    "TCH",
                    "VHC",
                    "PVD",
                    "FOX",
                    "SSI",
                    "NKG",
                    "BSI",
                    "ACB",
                    "REE",
                    "VHM",
                    "PAN",
                    "SIP",
                    "PTB",
                    "BSR",
                    "BID",
                    "PVS",
                    "CTS",
                    "FTS",
                    "HPG",
                    "DBC",
                    "MSR",
                    "THG",
                    "CTD",
                    "VOS",
                    "FMC",
                    "PHP",
                    "GAS",
                    "DCM",
                    "KSB",
                    "MSN",
                    "BVB",
                    "MBB",
                    "TRC",
                    "VPI",
                    "EIB",
                    "KDH",
                    "VCB",
                    "FPT",
                    "DRC",
                    "CMG",
                    "HAG",
                    "SHB",
                    "CII",
                    "CTR",
                    "IDC",
                    "GEE",
                    "NVB",
                    "BVS",
                    "BWE",
                    "HAX",
                    "QNS",
                    "VEA",
                    "TVS",
                    "DGC",
                    "HAH",
                    "NVL",
                    "PAC",
                    "AAA",
                    "TNH",
                    "ACV",
                    "BCC",
                    "FRT",
                    "HT1",
                    "SCS",
                    "TLH",
                    "MIG",
                    "SKG",
                    "DGC",
                    "VAB",
                    "NLG",
                    "HVN",
                    "HNG",
                    "PDR",
                    "VDS",
                    "SJE",
                    "PNJ",
                    "CEO",
                    "YEG",
                    "KLB",
                    "BCM",
                    "BVH",
                    "NTL",
                    "TDH",
                    "MBS",
                    "HUT",
                    "VIB",
                    "BAF",
                    "HHV",
                    "NDN",
                    "SGP",
                    "MCH",
                    "FCN",
                    "SCR",
                    "TCB",
                    "LPB",
                    "VTP",
                    "AGR",
                    "VCG",
                    "DPM",
                    "IDJ",
                    "DXS",
                    "OIL",
                    "AGG",
                    "VND",
                    "PSI",
                    "DHA",
                    "VIC",
                    "BCG",
                    "TPB",
                    "VIX",
                    "IJC",
                    "DGW",
                    "SBS",
                    "MFS",
                    "PLX",
                    "DRI",
                    "EVF",
                    "ORS",
                    "SAB",
                    "TDC",
                    "VNM",
                    "TV2",
                    "C4G",
                    "MWG",
                    "JVC",
                    "GDA",
                    "VGI",
                    "DSC",
                    "SMC",
                    "DTD",
                    "QCG",
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
                                if (dtFlag >= ma20.Date 
                                    || ma20.Date >= DateTime.Now.AddDays(-3))
                                    continue;

                                //if (ma20.Date.Month == 1 && ma20.Date.Day == 7 && ma20.Date.Year == 2025)
                                //{
                                //    var z = 1;
                                //}

                                var side = 0;
                                var cur = lData15m.First(x => x.Date == ma20.Date);
                                var rsi = lrsi.First(x => x.Date == ma20.Date);
                                var minOpenClose = Math.Min(cur.Open, cur.Close);
                                var maVol = lMaVol.First(x => x.Date == ma20.Date);

                                //var 
                                if (ma20.Sma is null 
                                    || cur.Close >= cur.Open
                                    || (decimal)ma20.Sma.Value <= cur.High
                                    || cur.Volume < (decimal)(maVol.Sma.Value * 1.5)
                                    || Math.Abs(minOpenClose - (decimal)ma20.LowerBand.Value) > Math.Abs((decimal)ma20.Sma.Value - minOpenClose)
                                    )
                                    continue;

                                var pivot = lData15m.First(x => x.Date > ma20.Date);
                                var bbPivot = lbb.First(x => x.Date > ma20.Date);
                                if (pivot.High >= (decimal)bbPivot.Sma.Value
                                    || (pivot.Low >= cur.Low && pivot.High <= cur.High))
                                    continue;

                                var rateVol = Math.Round(pivot.Volume / cur.Volume, 1);
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

                                if (Math.Abs(100 * (-1 + cur.Close / pivot.Close)) >= (decimal)6.8)
                                    continue;

                                var buy = lData15m.FirstOrDefault(x => x.Date > pivot.Date);
                                if (buy is null)
                                    continue;

                                //if ((decimal)bbPivot.Sma.Value - buy.Open < buy.Open - (decimal)bbPivot.LowerBand.Value)
                                //    continue;

                                if (buy.Open > Math.Max(pivot.Open, pivot.Close) * (decimal)1.01)
                                    continue;

                                cur = buy;

                                var next = lData15m.FirstOrDefault(x => x.Date > cur.Date);
                                if (next is null)
                                    continue;
                                var rateEntry = Math.Round(100 * (-1 + next.Low / cur.Open), 1);// tỉ lệ từ entry đến giá thấp nhất

                                var eEntry = cur;
                                var eClose = lData15m.Where(x => x.Date >= eEntry.Date).Skip(hour).FirstOrDefault();
                                if (eClose is null)
                                    continue;

                                var lClose = lData15m.Where(x => x.Date > eEntry.Date && x.Date <= eClose.Date).Skip(2);
                                foreach (var itemClose in lClose)
                                {
                                    var ma = lbb.First(x => x.Date == itemClose.Date);
                                    if (itemClose.Close > (decimal)ma.UpperBand)
                                    {
                                        eClose = lData15m.FirstOrDefault(x => x.Date > itemClose.Date);
                                        break;
                                    }
                                }

                                dtFlag = eClose.Date;
                                var rate = Math.Round(100 * (-1 + eClose.Open / eEntry.Open), 1);
                                var lRange = lData15m.Where(x => x.Date >= eEntry.Date && x.Date <= eClose.Date).Skip(2);
                                var maxH = lRange.Max(x => x.High);
                                var minL = lRange.Min(x => x.Low);

                                var winloss = "W";
                                if (rate <= (decimal)0)
                                {
                                    winloss = "L";
                                }

                                decimal maxTP = 0, maxSL = 0;
                                maxTP = Math.Round(100 * (-1 + maxH / eEntry.Open), 1);
                                maxSL = Math.Round(100 * (-1 + minL / eEntry.Open), 1);

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
                                var mes = $"{item}|{winloss}|BUY|{cur.Date.ToString("dd/MM/yyyy HH:mm")}|E: {eClose.Date.ToString("dd/MM/yyyy HH:mm")}|{rate}%|TPMax: {maxTP}%|SLMax: {maxSL}%|RateEntry: {rateEntry}%";
                                lMes.Add(mes);
                            }
                            catch (Exception ex)
                            {
                                break;
                                //_logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
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
                        if (true)
                        //if (rateRes > (decimal)0.5)
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
        public async Task Check2Sell()
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
                decimal SL_RATE = 10m;//1.5,1.6,1.8,1.9,2
                int hour = 10;//1h,2h,3h,4h

                var lMesAll = new List<string>();
                var lModel = new List<LongMa20>();

                var winTotal = 0;
                var lossTotal = 0;
                #region Comment
                lTake.Clear();
                var lTmp = new List<string>
                {
                    //Tier 1
                    "DPG",
                    "IDJ",
                    "PC1",
                    "TNH",
                    "SSB",
                    "YEG",
                    "NVB",
                    "HQC",
                    "G36",
                    "BOT",
                    "PXS",
                    "QBS",
                    "DTI",
                    "RDP",
                    "MGC",
                    "SRA",
                    "CVN",
                    "DFF",
                    "ITQ",
                    "API",
                    "DCL",
                    "DAG",
                    "GKM",
                    "DAH",
                    "PV2",
                    "PVC",
                    "AFX",
                    "DGT",
                    //Tire 2
                    "VHM",
                    "DXG",
                    "L18",
                    "HUT",
                    "PVT",
                    "CRE",
                    "DRC",
                    "PET",
                    "AGG",
                    "FCN",
                    "CTI",
                    "OCB",
                    "DPM",
                    "BFC",
                    "DTD",
                    "THG",
                    "FMC",
                    "VPI",
                    "BVB",
                    "GIL",
                    "PVD",
                    "HT1",
                    "VGT",
                    "PAC",
                    "OIL",
                    "CMG",
                    "SAM",
                    "NAB",
                    "SCR",
                    "LSS",
                    "SBS",
                    "TLH",
                    "PSH",
                    "PVP",
                    "KHG",
                    "HHG",
                    "CTF",
                    "HRT",
                    "VC7",
                    "PPC",
                    "ST8",
                    "AMS",
                    "HHP",
                    "ABS",
                    "TEG",
                    "TSC",
                    "APS",
                    "BWE",
                    "NHH",
                    "FCM",
                    "VIG",
                    "OCH",
                    "TC6",
                    "NBC",
                    "TCL",
                    "VAB",
                    //Tire 3
                    "SAB",
                    "BID",
                    "VCB",
                    "VPB",
                    "EIB",
                    "KLB",
                    "VRE",
                    "HHV",
                    "BCM",
                    "TCM",
                    "TNG",
                    "GDA",
                    "POW",
                    "PTB",
                    "IJC",
                    "CEO",
                    "BSR",
                    "VSC",
                    "C4G",
                    "SBT",
                    "PSI",
                    "TTF",
                    "DXP",
                    "BIG",
                    "GPC",
                    "DVM",
                    "SHI",
                    "SBG",
                    "VTD",
                    "CCL",
                    "CRC",
                    "BMC",
                    "VTO",
                    "ASM",
                    "LCG",
                    "HCD",
                    "JVC",
                    "AAS",
                    "TDP",
                    "TCD",
                    "TIP",
                    "ACC",
                    "VGS",
                };
                lTake.AddRange(lTmp);
                #endregion
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

                        DateTime dtFlag = DateTime.MinValue;
                        //var count = 0;
                        foreach (var ma20 in lbb)
                        {
                            try
                            {
                                if (dtFlag >= ma20.Date)
                                    continue;

                                if (ma20.Date >= DateTime.Now.AddDays(-3))
                                    continue;

                                //if (ma20.Date.Month == 1 && ma20.Date.Day == 7 && ma20.Date.Year == 2025)
                                //{
                                //    var z = 1;
                                //}

                                var side = 1;
                                var cur = lData15m.First(x => x.Date == ma20.Date);
                                var rsi = lrsi.First(x => x.Date == ma20.Date);
                                var maxOpenClose = Math.Max(cur.Open, cur.Close);

                                if (cur.Open >= cur.Close
                                    || ma20.Sma is null
                                    || rsi.Rsi < 60
                                    //|| cur.Low >= (decimal)ma20.LowerBand.Value
                                    || Math.Abs(maxOpenClose - (decimal)ma20.UpperBand.Value) > Math.Abs((decimal)ma20.Sma.Value - maxOpenClose)
                                    )
                                    continue;

                                var rsiPivot = lrsi.FirstOrDefault(x => x.Date > ma20.Date);
                                //if (rsiPivot is null || rsiPivot.Rsi > 35 || rsiPivot.Rsi < 25)
                                //    continue;

                                var pivot = lData15m.First(x => x.Date > ma20.Date);
                                var bbPivot = lbb.First(x => x.Date > ma20.Date);
                                if (pivot.High <= (decimal)bbPivot.UpperBand.Value
                                    || pivot.Low <= (decimal)bbPivot.Sma.Value
                                    //|| (pivot.Low >= cur.Low && pivot.High <= cur.High)
                                    )
                                    continue;

                                var rateVol = Math.Round(pivot.Volume / cur.Volume, 1);
                                //if (rateVol > (decimal)0.6 || rateVol < (decimal)0.4) //Vol hiện tại phải nhỏ hơn hoặc bằng 0.6 lần vol của nến liền trước
                                if (rateVol > (decimal)0.6) //Vol hiện tại phải nhỏ hơn hoặc bằng 0.6 lần vol của nến liền trước
                                    continue;

                                //độ dài nến hiện tại
                                var rateCur = Math.Abs((cur.Open - cur.Close) / (cur.High - cur.Low));
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

                                var buy = lData15m.FirstOrDefault(x => x.Date > rsiPivot.Date);
                                if (buy is null)
                                    continue;

                                cur = buy;

                                var next = lData15m.FirstOrDefault(x => x.Date > cur.Date);
                                if (next is null)
                                    continue;
                                var rateEntry = Math.Round(100 * (-1 + cur.Close / next.High), 1);// tỉ lệ từ entry đến giá thấp nhất

                                var eEntry = cur;
                                var eClose = lData15m.Where(x => x.Date >= eEntry.Date).Skip(hour).FirstOrDefault();
                                if (eClose is null)
                                    continue;

                                var lClose = lData15m.Where(x => x.Date > eEntry.Date && x.Date <= eClose.Date).Skip(2);
                                foreach (var itemClose in lClose)
                                {
                                    var ma = lbb.First(x => x.Date == itemClose.Date);
                                    if (itemClose.Close < (decimal)ma.LowerBand)
                                    {
                                        eClose = lData15m.FirstOrDefault(x => x.Date > itemClose.Date);
                                        break;
                                    }
                                }

                                dtFlag = eClose.Date;
                                var rate = Math.Round(100 * (-1 + eEntry.Open / eClose.Open ), 1);
                                var lRange = lData15m.Where(x => x.Date >= eEntry.Date && x.Date <= eClose.Date).Skip(2);
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
                                var mes = $"{item}|{winloss}|SELL|{cur.Date.ToString("dd/MM/yyyy HH:mm")}|{rate}%|TPMax: {maxTP}%|SLMax: {maxSL}%|RateEntry: {rateEntry}%|RSI: {rsiPivot.Rsi}";
                                lMes.Add(mes);
                            }
                            catch (Exception ex)
                            {
                                break;
                                //_logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
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
                        if (winCount + lossCount <= 0)
                            continue;

                        var rateRes = Math.Round(((decimal)winCount / (winCount + lossCount)), 2);
                        if (rateRes > (decimal)0.5)
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
                            var perRate = Math.Round((float)sumRate / count, 1);
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

        public async Task CheckSomething()
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
                decimal SL_RATE = 10m;//1.5,1.6,1.8,1.9,2
                int hour = 10;//1h,2h,3h,4h

                var lMesAll = new List<string>();
                var lModel = new List<LongMa20>();

                var winTotal = 0;
                var lossTotal = 0;
                lTake.Clear();
                var lTmp = new List<string>
                {
                    "VNINDEX",
                    "DC4",
                    "GIL",
                    "GVR",
                    "DPG",
                    "CTG",
                    "BFC",
                    "VRE",
                    "PVB",
                    "GEX",
                    "SZC",
                    "HDG",
                    "BMP",
                    "TLG",
                    "VPB",
                    "DIG",
                    "KBC",
                    "HSG",
                    "PET",
                    "TNG",
                    "SBT",
                    "MSH",
                    "NAB",
                    "VGC",
                    "CSV",
                    "VCS",
                    "CSM",
                    "PHR",
                    "PVT",
                    "PC1",
                    "ASM",
                    "LAS",
                    "DXG",
                    "HCM",
                    "CTI",
                    "NHA",
                    "DPR",
                    "ANV",
                    "OCB",
                    "TVB",
                    "STB",
                    "HDC",
                    "POW",
                    "VSC",
                    "L18",
                    "DDV",
                    "VCI",
                    "GMD",
                    "NTP",
                    "KSV",
                    "TTF",
                    "NT2",
                    "TCM",
                    "LSS",
                    "GEG",
                    "HHS",
                    "MSB",
                    "TCH",
                    "VHC",
                    "PVD",
                    "FOX",
                    "SSI",
                    "NKG",
                    "BSI",
                    "ACB",
                    "REE",
                    "VHM",
                    "PAN",
                    "SIP",
                    "PTB",
                    "BSR",
                    "BID",
                    "PVS",
                    "CTS",
                    "FTS",
                    "HPG",
                    "DBC",
                    "MSR",
                    "THG",
                    "CTD",
                    "VOS",
                    "FMC",
                    "PHP",
                    "GAS",
                    "DCM",
                    "KSB",
                    "MSN",
                    "BVB",
                    "MBB",
                    "TRC",
                    "VPI",
                    "EIB",
                    "KDH",
                    "VCB",
                    "FPT",
                    "DRC",
                    "CMG",
                    "HAG",
                    "SHB",
                    "CII",
                    "CTR",
                    "IDC",
                    "GEE",
                    "NVB",
                    "BVS",
                    "BWE",
                    "HAX",
                    "QNS",
                    "VEA",
                    "TVS",
                    "DGC",
                    "HAH",
                    "NVL",
                    "PAC",
                    "AAA",
                    "TNH",
                    "ACV",
                    "BCC",
                    "FRT",
                    "HT1",
                    "SCS",
                    "TLH",
                    "MIG",
                    "SKG",
                    "DGC",
                    "VAB",
                    "NLG",
                    "HVN",
                    "HNG",
                    "PDR",
                    "VDS",
                    "SJE",
                    "PNJ",
                    "CEO",
                    "YEG",
                    "KLB",
                    "BCM",
                    "BVH",
                    "NTL",
                    "TDH",
                    "MBS",
                    "HUT",
                    "VIB",
                    "BAF",
                    "HHV",
                    "NDN",
                    "SGP",
                    "MCH",
                    "FCN",
                    "SCR",
                    "TCB",
                    "LPB",
                    "VTP",
                    "AGR",
                    "VCG",
                    "DPM",
                    "IDJ",
                    "DXS",
                    "OIL",
                    "AGG",
                    "VND",
                    "PSI",
                    "DHA",
                    "VIC",
                    "BCG",
                    "TPB",
                    "VIX",
                    "IJC",
                    "DGW",
                    "SBS",
                    "MFS",
                    "PLX",
                    "DRI",
                    "EVF",
                    "ORS",
                    "SAB",
                    "TDC",
                    "VNM",
                    "TV2",
                    "C4G",
                    "MWG",
                    "JVC",
                    "GDA",
                    "VGI",
                    "DSC",
                    "SMC",
                    "DTD",
                    "QCG",
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
                        var near = lData15m.SkipLast(1).Last();
                        var lbb = lData15m.GetBollingerBands();
                        var lrsi = lData15m.GetRsi();
                        var lVol = lData15m.Select(x => new Quote
                        {
                            Date = x.Date,
                            Close = x.Volume
                        }).ToList();
                        var lMaVol = lVol.GetSma(20);
                        var near_MaVol = lMaVol.SkipLast(1).Last();

                        DateTime dtFlag = DateTime.MinValue;
                        //var count = 0;
                        foreach (var ma20 in lbb)
                        {
                            try
                            {
                                if (ma20.Sma is null)
                                    continue;
                                //if (dtFlag >= ma20.Date
                                //    || ma20.Date >= DateTime.Now.AddDays(-3))
                                //    continue;

                                //if (ma20.Date.Day == 7 && ma20.Date.Month == 3 && ma20.Date.Year == 2025)
                                //{
                                //    var z = 1;
                                //}

                                //SIGNAL
                                var side = 0;
                                var entity_Sig = lData15m.First(x => x.Date == ma20.Date);
                                var bb_Sig = lbb.First(x => x.Date == ma20.Date);
                                //var rsi_Sig = lrsi.First(x => x.Date == ma20.Date);
                                var maVol_Sig = lMaVol.First(x => x.Date == ma20.Date);
                                
                                //PIVOT
                                var entity_Pivot = lData15m.First(x => x.Date > ma20.Date);
                                var bb_Pivot = lbb.First(x => x.Date > ma20.Date);
                                var maVol_Pivot = lMaVol.First(x => x.Date > ma20.Date);
                                //PreSig
                                var entity_Pre = lData15m.Last(x => x.Date < ma20.Date);

                                var rateVol = entity_Pivot.Volume / entity_Sig.Volume;
                                if (rateVol > (decimal)0.6
                                    || (decimal)(maVol_Sig.Sma.Value * 0.9)> entity_Sig.Volume)
                                    continue;


                                var isGreen_Sig = entity_Sig.Close >= entity_Sig.Open;
                                if (isGreen_Sig)
                                {
                                    if (entity_Sig.Close > (decimal)bb_Sig.Sma.Value
                                        && (entity_Sig.Close - (decimal)bb_Sig.Sma.Value) >= ((decimal)bb_Sig.UpperBand.Value - entity_Sig.Close)
                                        && entity_Pivot.Low > (decimal)bb_Pivot.Sma.Value
                                        && (entity_Pivot.Close - (decimal)bb_Pivot.Sma.Value) >= ((decimal)bb_Pivot.UpperBand.Value - entity_Pivot.Close))
                                    {
                                        Console.WriteLine($"{item}|1.SELL: {bb_Pivot.Date.ToString("dd/MM/yyyy")}");
                                    }
                                    else if (entity_Sig.Open < (decimal)bb_Sig.Sma.Value
                                           && ((decimal)bb_Sig.Sma.Value - entity_Sig.Close) <= (entity_Sig.Close - (decimal)bb_Sig.LowerBand.Value)
                                           && ((decimal)bb_Sig.UpperBand.Value - entity_Sig.Close) >= (entity_Sig.Close - (decimal)bb_Sig.Sma.Value)
                                           && entity_Pivot.Close < (decimal)bb_Pivot.Sma.Value
                                           && ((decimal)bb_Pivot.Sma.Value - entity_Pivot.Close) <= (entity_Pivot.Close - (decimal)bb_Pivot.LowerBand.Value)
                                           && Math.Max(entity_Pre.Open, entity_Pre.Close) < (decimal)bb_Sig.Sma.Value)
                                    {
                                        Console.WriteLine($"{item}|2.SELL: {bb_Pivot.Date.ToString("dd/MM/yyyy")}");
                                    }
                                }
                                else
                                {
                                    if(entity_Sig.Close < (decimal)bb_Sig.Sma.Value
                                        && (entity_Sig.Close - (decimal)bb_Sig.LowerBand.Value) <= ((decimal)bb_Sig.Sma.Value - entity_Sig.Close)
                                        && entity_Pivot.High < (decimal)bb_Pivot.Sma.Value
                                        && (entity_Pivot.Close - (decimal)bb_Pivot.LowerBand.Value) <= ((decimal)bb_Pivot.Sma.Value - entity_Pivot.Close))
                                    {
                                        Console.WriteLine($"{item}|1.BUY: {bb_Pivot.Date.ToString("dd/MM/yyyy")}");
                                    }
                                    else if(entity_Sig.Open > (decimal)bb_Sig.Sma.Value
                                            && (entity_Sig.Close - (decimal)bb_Sig.Sma.Value) <= ((decimal)bb_Sig.UpperBand.Value - entity_Sig.Close)
                                            && (entity_Sig.Close - (decimal)bb_Sig.LowerBand.Value) >= ((decimal)bb_Sig.Sma.Value - entity_Sig.Close)
                                            && entity_Pivot.Close > (decimal)bb_Pivot.Sma.Value
                                            && (entity_Pivot.Close - (decimal)bb_Pivot.Sma.Value) <= ((decimal)bb_Pivot.UpperBand.Value - entity_Pivot.Close)
                                            && Math.Min(entity_Pre.Open, entity_Pre.Close) > (decimal)bb_Sig.Sma.Value)
                                    {
                                        Console.WriteLine($"{item}|2.BUY: {bb_Pivot.Date.ToString("dd/MM/yyyy")}");
                                    }
                                }




                            }
                            catch (Exception ex)
                            {
                                break;
                                //_logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
                            }

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

        public async Task CheckCurrentDay()
        {
            try
            {
                decimal SL_RATE = 10m;//1.5,1.6,1.8,1.9,2
                int hour = 10;//1h,2h,3h,4h

                var lMesAll = new List<string>();
                var lModel = new List<LongMa20>();

                var winTotal = 0;
                var lossTotal = 0;
                var lTake = new List<string>
                {
                    "VNINDEX",
                    "DC4",
                    "GIL",
                    "GVR",
                    "DPG",
                    "CTG",
                    "BFC",
                    "VRE",
                    "PVB",
                    "GEX",
                    "SZC",
                    "HDG",
                    "BMP",
                    "TLG",
                    "VPB",
                    "DIG",
                    "KBC",
                    "HSG",
                    "PET",
                    "TNG",
                    "SBT",
                    "MSH",
                    "NAB",
                    "VGC",
                    "CSV",
                    "VCS",
                    "CSM",
                    "PHR",
                    "PVT",
                    "PC1",
                    "ASM",
                    "LAS",
                    "DXG",
                    "HCM",
                    "CTI",
                    "NHA",
                    "DPR",
                    "ANV",
                    "OCB",
                    "TVB",
                    "STB",
                    "HDC",
                    "POW",
                    "VSC",
                    "L18",
                    "DDV",
                    "VCI",
                    "GMD",
                    "NTP",
                    "KSV",
                    "NT2",
                    "TCM",
                    "LSS",
                    "GEG",
                    "HHS",
                    "MSB",
                    "TCH",
                    "VHC",
                    "PVD",
                    "FOX",
                    "SSI",
                    "NKG",
                    "BSI",
                    "ACB",
                    "REE",
                    "VHM",
                    "PAN",
                    "SIP",
                    "PTB",
                    "BSR",
                    "BID",
                    "PVS",
                    "CTS",
                    "FTS",
                    "HPG",
                    "DBC",
                    "MSR",
                    "THG",
                    "CTD",
                    "VOS",
                    "FMC",
                    "PHP",
                    "GAS",
                    "DCM",
                    "KSB",
                    "MSN",
                    "BVB",
                    "MBB",
                    "TRC",
                    "VPI",
                    "EIB",
                    "KDH",
                    "VCB",
                    "FPT",
                    "DRC",
                    "CMG",
                    "HAG",
                    "SHB",
                    "CII",
                    "CTR",
                    "IDC",
                    "GEE",
                    "NVB",
                    "BVS",
                    "BWE",
                    "HAX",
                    "QNS",
                    "VEA",
                    "TVS",
                    "DGC",
                    "HAH",
                    "NVL",
                    "PAC",
                    "AAA",
                    "TNH",
                    "ACV",
                    "BCC",
                    "FRT",
                    "HT1",
                    "SCS",
                    "TLH",
                    "MIG",
                    "SKG",
                    "DGC",
                    "VAB",
                    "NLG",
                    "HVN",
                    "HNG",
                    "PDR",
                    "VDS",
                    "SJE",
                    "PNJ",
                    "CEO",
                    "YEG",
                    "KLB",
                    "BCM",
                    "BVH",
                    "NTL",
                    "TDH",
                    "MBS",
                    "HUT",
                    "VIB",
                    "BAF",
                    "HHV",
                    "NDN",
                    "SGP",
                    "MCH",
                    "FCN",
                    "SCR",
                    "TCB",
                    "LPB",
                    "VTP",
                    "AGR",
                    "VCG",
                    "DPM",
                    "IDJ",
                    "DXS",
                    "OIL",
                    "AGG",
                    "VND",
                    "PSI",
                    "DHA",
                    "VIC",
                    "BCG",
                    "TPB",
                    "VIX",
                    "IJC",
                    "DGW",
                    "SBS",
                    "MFS",
                    "PLX",
                    "DRI",
                    "EVF",
                    "ORS",
                    "SAB",
                    "TDC",
                    "VNM",
                    "TV2",
                    "C4G",
                    "MWG",
                    "JVC",
                    "GDA",
                    "VGI",
                    "DSC",
                    "SMC",
                    "DTD",
                    "QCG",
                };
                var lPoint = new List<clsPoint>();
                foreach (var item in lTake)
                {
                    try
                    {
                        //if (item != "CTS")
                        //    continue;

                        var lMes = new List<string>();

                        var lData15m = await _apiService.SSI_GetDataStock(item);
                        Thread.Sleep(200);
                        if (lData15m == null || !lData15m.Any() || lData15m.Count() < 250 || lData15m.Last().Volume < 10000)
                            continue;
                       
                        var lbb = lData15m.GetBollingerBands();
                        var lrsi = lData15m.GetRsi();
                        var lMaVol = lData15m.Select(x => new Quote
                        {
                            Date = x.Date,
                            Close = x.Volume
                        }).GetSma(20);


                        Quote entity_Sig = null;
                        SmaResult maVol_Sig = null;
                        BollingerBandsResult bb_Sig = null;

                        Quote entity_Pivot = null;
                        SmaResult maVol_Pivot = null;
                        BollingerBandsResult bb_Pivot = null;
                        RsiResult rsi_Pivot = null;

                        Quote entity_NearSig = null;
                        BollingerBandsResult bb_NearSig = null;

                        var passSignal = false;
                        var lCheckSignal = lData15m.TakeLast(6);
                        foreach (var itemCheckSignal in lCheckSignal)
                        {
                            var ma = lbb.First(x => x.Date == itemCheckSignal.Date);
                            if (itemCheckSignal.Close >= (decimal)ma.Sma.Value)
                                continue;

                            if (itemCheckSignal.Close >= itemCheckSignal.Open)
                                continue;

                            var maVol = lMaVol.First(x => x.Date == itemCheckSignal.Date);
                            if (itemCheckSignal.Volume <= (decimal)maVol.Sma.Value) //Chỉ cần > ma20 hay cần > 1.5 * ma20?
                                continue;

                            var next = lData15m.FirstOrDefault(x => x.Date > itemCheckSignal.Date);
                            if (next is null)
                                break;

                            if ((next.Volume / itemCheckSignal.Volume) > 0.6m)
                                continue;
                            //Sig
                            entity_Sig = itemCheckSignal;
                            maVol_Sig = lMaVol.First(x => x.Date == entity_Sig.Date);
                            bb_Sig = lbb.First(x => x.Date == entity_Sig.Date);

                            //Pivot
                            entity_Pivot = next;
                            maVol_Pivot = lMaVol.First(x => x.Date == entity_Pivot.Date);
                            bb_Pivot = lbb.First(x => x.Date == entity_Pivot.Date);
                            rsi_Pivot = lrsi.First(x => x.Date == entity_Pivot.Date);

                            //Near Sig
                            entity_NearSig = lData15m.Last(x => x.Date < entity_Sig.Date);
                            bb_NearSig = lbb.First(x => x.Date == entity_NearSig.Date);

                            passSignal = true;
                            break;
                        }
                        if (!passSignal) 
                            continue;

                        //var entityLast = lData15m.Last();
                        //var bbLast = lbb.Last();
                        //var pos_Last = Math.Abs((entityLast.Close - (decimal)bbLast.Sma.Value) / (entityLast.Close - (decimal)bbLast.LowerBand.Value));
                        //if (pos_Last < 2)
                        //    continue;

                        var rateMaVol = Math.Round(entity_Sig.Volume / (decimal)maVol_Sig.Sma.Value, 2);
                        var rateNear = Math.Round(entity_Sig.Volume / entity_NearSig.Volume, 2);

                        var mes = entity_Sig.Date.ToString("dd/MM/yyyy");
                        var point = 0;
                        if (rateNear >= 1.5m)
                        {
                            point += 25;
                            mes += $"|Vol Near < 1.5";
                        }

                        if (rateMaVol >= 1.5m)
                        {
                            point += 20;
                            mes += $"|Vol Ma20 < 1.5";
                        }

                        if(entity_Sig.Close < Math.Min(entity_NearSig.Open, entity_NearSig.Close))
                        {
                            point += 15;
                            mes += $"|Sig below Near";
                        }    

                        var lCheck = lData15m.Where(x => x.Date > entity_Sig.Date).TakeLast(2);
                        foreach (var itemCheck in lCheck)
                        {
                            var isPinbar = (Math.Min(itemCheck.Open, itemCheck.Close) - itemCheck.Low) >= 3 * (itemCheck.High - Math.Min(itemCheck.Open, itemCheck.Close));
                            if (isPinbar || itemCheck.Close >= itemCheck.Open)
                            {
                                point += 17;
                                mes += "|Nến xanh hoặc Pinbar";

                                if (itemCheck.Low < entity_Sig.Low)
                                {
                                    point += 20;
                                    mes += "|Entry LOW< Signal";
                                }

                                if (itemCheck.Close <= 0.5m * (entity_Sig.Open + entity_Sig.Close))
                                {
                                    point += 15;
                                    mes += "|Entry < trung bình nến Signal";
                                }    
                                    

                                var rsiCheck = lrsi.First(x => x.Date == itemCheck.Date);
                                if (rsiCheck.Rsi.Value <= 30)
                                {
                                    point += 10;
                                    mes += "|Entry RSI < 30";
                                }    

                                var bbCheck = lbb.First(x => x.Date == itemCheck.Date);
                                if (itemCheck.Low < (decimal)bbCheck.LowerBand.Value)
                                {
                                    point += 15;
                                    mes += "|Entry < BB Lower";
                                }    

                                if(itemCheck.Close < (decimal)bbCheck.Sma.Value)
                                {
                                    point += 15;
                                    mes += "|Entry < BB MA20";
                                }    

                                lPoint.Add(new clsPoint
                                {
                                    s = item,
                                    TotalPoint = point,
                                    mes = mes
                                });

                                break;
                            }
                        }

                        
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{item}| {ex.Message}");
                    }
                }


                foreach (var item in lPoint.Where(x => x.TotalPoint > 50).OrderByDescending(x => x.TotalPoint))
                {
                    Console.WriteLine($"{item.s}: {item.TotalPoint} => {item.mes}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
            }
        }

        //public async Task CheckCurrentDay()
        //{
        //    try
        //    {
        //        decimal SL_RATE = 10m;//1.5,1.6,1.8,1.9,2
        //        int hour = 10;//1h,2h,3h,4h

        //        var lMesAll = new List<string>();
        //        var lModel = new List<LongMa20>();

        //        var winTotal = 0;
        //        var lossTotal = 0;
        //        var lTake = new List<string>
        //        {
        //            "VNINDEX",
        //            "DC4",
        //            "GIL",
        //            "GVR",
        //            "DPG",
        //            "CTG",
        //            "BFC",
        //            "VRE",
        //            "PVB",
        //            "GEX",
        //            "SZC",
        //            "HDG",
        //            "BMP",
        //            "TLG",
        //            "VPB",
        //            "DIG",
        //            "KBC",
        //            "HSG",
        //            "PET",
        //            "TNG",
        //            "SBT",
        //            "MSH",
        //            "NAB",
        //            "VGC",
        //            "CSV",
        //            "VCS",
        //            "CSM",
        //            "PHR",
        //            "PVT",
        //            "PC1",
        //            "ASM",
        //            "LAS",
        //            "DXG",
        //            "HCM",
        //            "CTI",
        //            "NHA",
        //            "DPR",
        //            "ANV",
        //            "OCB",
        //            "TVB",
        //            "STB",
        //            "HDC",
        //            "POW",
        //            "VSC",
        //            "L18",
        //            "DDV",
        //            "VCI",
        //            "GMD",
        //            "NTP",
        //            "KSV",
        //            "NT2",
        //            "TCM",
        //            "LSS",
        //            "GEG",
        //            "HHS",
        //            "MSB",
        //            "TCH",
        //            "VHC",
        //            "PVD",
        //            "FOX",
        //            "SSI",
        //            "NKG",
        //            "BSI",
        //            "ACB",
        //            "REE",
        //            "VHM",
        //            "PAN",
        //            "SIP",
        //            "PTB",
        //            "BSR",
        //            "BID",
        //            "PVS",
        //            "CTS",
        //            "FTS",
        //            "HPG",
        //            "DBC",
        //            "MSR",
        //            "THG",
        //            "CTD",
        //            "VOS",
        //            "FMC",
        //            "PHP",
        //            "GAS",
        //            "DCM",
        //            "KSB",
        //            "MSN",
        //            "BVB",
        //            "MBB",
        //            "TRC",
        //            "VPI",
        //            "EIB",
        //            "KDH",
        //            "VCB",
        //            "FPT",
        //            "DRC",
        //            "CMG",
        //            "HAG",
        //            "SHB",
        //            "CII",
        //            "CTR",
        //            "IDC",
        //            "GEE",
        //            "NVB",
        //            "BVS",
        //            "BWE",
        //            "HAX",
        //            "QNS",
        //            "VEA",
        //            "TVS",
        //            "DGC",
        //            "HAH",
        //            "NVL",
        //            "PAC",
        //            "AAA",
        //            "TNH",
        //            "ACV",
        //            "BCC",
        //            "FRT",
        //            "HT1",
        //            "SCS",
        //            "TLH",
        //            "MIG",
        //            "SKG",
        //            "DGC",
        //            "VAB",
        //            "NLG",
        //            "HVN",
        //            "HNG",
        //            "PDR",
        //            "VDS",
        //            "SJE",
        //            "PNJ",
        //            "CEO",
        //            "YEG",
        //            "KLB",
        //            "BCM",
        //            "BVH",
        //            "NTL",
        //            "TDH",
        //            "MBS",
        //            "HUT",
        //            "VIB",
        //            "BAF",
        //            "HHV",
        //            "NDN",
        //            "SGP",
        //            "MCH",
        //            "FCN",
        //            "SCR",
        //            "TCB",
        //            "LPB",
        //            "VTP",
        //            "AGR",
        //            "VCG",
        //            "DPM",
        //            "IDJ",
        //            "DXS",
        //            "OIL",
        //            "AGG",
        //            "VND",
        //            "PSI",
        //            "DHA",
        //            "VIC",
        //            "BCG",
        //            "TPB",
        //            "VIX",
        //            "IJC",
        //            "DGW",
        //            "SBS",
        //            "MFS",
        //            "PLX",
        //            "DRI",
        //            "EVF",
        //            "ORS",
        //            "SAB",
        //            "TDC",
        //            "VNM",
        //            "TV2",
        //            "C4G",
        //            "MWG",
        //            "JVC",
        //            "GDA",
        //            "VGI",
        //            "DSC",
        //            "SMC",
        //            "DTD",
        //            "QCG",
        //        };
        //        var lPoint = new List<clsPoint>();
        //        foreach (var item in lTake)
        //        {
        //            var winCount = 0;
        //            var lossCount = 0;
        //            try
        //            {
        //                var lMes = new List<string>();

        //                var lData15m = await _apiService.SSI_GetDataStock(item);
        //                Thread.Sleep(200);
        //                if (lData15m == null || !lData15m.Any() || lData15m.Count() < 250 || lData15m.Last().Volume < 50000)
        //                    continue;

        //                var lbb = lData15m.GetBollingerBands();
        //                var lrsi = lData15m.GetRsi();
        //                var lMaVol = lData15m.Select(x => new Quote
        //                {
        //                    Date = x.Date,
        //                    Close = x.Volume
        //                }).GetSma(20);

        //                var entity_Pivot = lData15m.Last();
        //                var maVol_Pivot = lMaVol.Last();
        //                var bb_Pivot = lbb.Last();

        //                var entity_Sig = lData15m.SkipLast(1).Last();
        //                var maVol_Sig = lMaVol.SkipLast(1).Last();
        //                var bb_Sig = lbb.SkipLast(1).Last();

        //                var near2 = lData15m.SkipLast(2).Last();
        //                var near3 = lData15m.SkipLast(3).Last();
        //                var near4 = lData15m.SkipLast(4).Last();
        //                var near5 = lData15m.SkipLast(5).Last();

        //                var bb2 = lbb.SkipLast(2).Last();
        //                var bb3 = lbb.SkipLast(3).Last();
        //                var bb4 = lbb.SkipLast(4).Last();
        //                var bb5 = lbb.SkipLast(5).Last();

        //                var ma20_Sig = (decimal)bb_Sig.Sma.Value;
        //                var upper_Sig = (decimal)bb_Sig.UpperBand.Value;
        //                var lower_Sig = (decimal)bb_Sig.LowerBand.Value;
        //                var close_Sig = entity_Sig.Close;
        //                var UpOrLow_Sig = close_Sig > ma20_Sig ? upper_Sig : lower_Sig;
        //                var mes = close_Sig > ma20_Sig ? "SELL" : "BUY";
        //                var pos_Sig = Math.Abs((close_Sig - ma20_Sig) / (close_Sig - UpOrLow_Sig));
        //                var rateMaVol = Math.Round(entity_Sig.Volume / (decimal)maVol_Sig.Sma.Value, 2);
        //                var rateVol = entity_Pivot.Volume / entity_Sig.Volume;

        //                if (pos_Sig >= 2
        //                    //&& rateVol <= 0.5m
        //                    && rateMaVol >= 2m)
        //                //&& rateMaVol >= 2.5m)
        //                {
        //                    Console.WriteLine($"{mes}|CHUAN|{item}");
        //                }

        //                var ma20_Pivot = (decimal)bb_Pivot.Sma.Value;
        //                var upper_Pivot = (decimal)bb_Pivot.UpperBand.Value;
        //                var lower_Pivot = (decimal)bb_Pivot.LowerBand.Value;
        //                var close_Pivot = entity_Pivot.Close;
        //                var UpOrLow_Pivot = close_Pivot > ma20_Pivot ? upper_Pivot : lower_Pivot;
        //                var mes_Pivot = close_Pivot > ma20_Pivot ? "SELL" : "BUY";
        //                var pos_Pivot = Math.Abs((close_Pivot - ma20_Pivot) / (close_Pivot - UpOrLow_Pivot));
        //                var rateMaVol_Pivot = Math.Round(entity_Pivot.Volume / (decimal)maVol_Pivot.Sma.Value, 2);
        //                if (pos_Pivot >= 2
        //                  && rateMaVol_Pivot >= 2m)
        //                //&& rateMaVol_Pivot >= 2.5m)
        //                {
        //                    Console.WriteLine($"{mes}|TheoDoi|{item}");
        //                }
        //                continue;


        //                if (rateVol > (decimal)0.6)
        //                    continue;

        //                var sig_lower = Math.Abs(entity_Sig.Close - (decimal)bb_Sig.LowerBand.Value);
        //                var sig_ma = Math.Abs(entity_Sig.Close - (decimal)bb_Sig.Sma.Value);
        //                var sig_upper = Math.Abs(entity_Sig.Close - (decimal)bb_Sig.UpperBand.Value);
        //                var min = Math.Min(Math.Min(sig_lower, sig_ma), sig_upper);

        //                if (min == sig_lower)
        //                {
        //                    var maxPivot = Math.Max(entity_Pivot.Open, entity_Pivot.Close);
        //                    if (maxPivot < Math.Max(entity_Sig.Open, entity_Sig.Close)
        //                        && ((decimal)bb_Pivot.Sma.Value - maxPivot) > (maxPivot - (decimal)bb_Pivot.LowerBand.Value))
        //                    {
        //                        //good 
        //                        Console.WriteLine($"LONG_BB: {item}");
        //                    }
        //                }
        //                else if (min == sig_upper)
        //                {
        //                    Console.WriteLine($"SHORT_BB: {item}");
        //                }
        //                else
        //                {
        //                    if (entity_Sig.Close < (decimal)bb_Sig.Sma.Value)
        //                    {
        //                        if (entity_Pivot.Close <= (decimal)bb_Pivot.Sma.Value
        //                            && near2.Close < (decimal)bb2.Sma.Value
        //                            && near3.Close < (decimal)bb3.Sma.Value
        //                            )
        //                        {
        //                            Console.WriteLine($"SHORT_MA: {item}");
        //                        }
        //                    }
        //                    else
        //                    {
        //                        if (entity_Pivot.Close >= (decimal)bb_Pivot.Sma.Value
        //                            && near2.Close > (decimal)bb2.Sma.Value
        //                            && near3.Close > (decimal)bb3.Sma.Value
        //                            )
        //                        {
        //                            Console.WriteLine($"LONG_MA: {item}");
        //                        }
        //                    }
        //                }

        //                //if (entity_Sig.Volume <= (decimal)maVol_Sig.Sma.Value * 1.1m)
        //                //    continue;

        //                //Console.WriteLine(item);

        //                //DateTime dtFlag = DateTime.MinValue;
        //                ////var count = 0;
        //                //foreach (var ma20 in lbb)
        //                //{
        //                //    //try
        //                //    //{
        //                //    //    if (ma20.Sma is null)
        //                //    //        continue;
        //                //    //    //if (dtFlag >= ma20.Date
        //                //    //    //    || ma20.Date >= DateTime.Now.AddDays(-3))
        //                //    //    //    continue;

        //                //    //    //if (ma20.Date.Day == 7 && ma20.Date.Month == 3 && ma20.Date.Year == 2025)
        //                //    //    //{
        //                //    //    //    var z = 1;
        //                //    //    //}

        //                //    //    //SIGNAL
        //                //    //    var side = 0;
        //                //    //    var entity_Sig = lData15m.First(x => x.Date == ma20.Date);
        //                //    //    var bb_Sig = lbb.First(x => x.Date == ma20.Date);
        //                //    //    //var rsi_Sig = lrsi.First(x => x.Date == ma20.Date);
        //                //    //    var maVol_Sig = lMaVol.First(x => x.Date == ma20.Date);

        //                //    //    //PIVOT
        //                //    //    var entity_Pivot = lData15m.First(x => x.Date > ma20.Date);
        //                //    //    var bb_Pivot = lbb.First(x => x.Date > ma20.Date);
        //                //    //    var maVol_Pivot = lMaVol.First(x => x.Date > ma20.Date);
        //                //    //    //PreSig
        //                //    //    var entity_Pre = lData15m.Last(x => x.Date < ma20.Date);

        //                //    //    var rateVol = entity_Pivot.Volume / entity_Sig.Volume;
        //                //    //    if (rateVol > (decimal)0.6
        //                //    //        || (decimal)(maVol_Sig.Sma.Value * 0.9) > entity_Sig.Volume)
        //                //    //        continue;


        //                //    //    var isGreen_Sig = entity_Sig.Close >= entity_Sig.Open;
        //                //    //    if (isGreen_Sig)
        //                //    //    {
        //                //    //        if (entity_Sig.Close > (decimal)bb_Sig.Sma.Value
        //                //    //            && (entity_Sig.Close - (decimal)bb_Sig.Sma.Value) >= ((decimal)bb_Sig.UpperBand.Value - entity_Sig.Close)
        //                //    //            && entity_Pivot.Low > (decimal)bb_Pivot.Sma.Value
        //                //    //            && (entity_Pivot.Close - (decimal)bb_Pivot.Sma.Value) >= ((decimal)bb_Pivot.UpperBand.Value - entity_Pivot.Close))
        //                //    //        {
        //                //    //            Console.WriteLine($"{item}|1.SELL: {bb_Pivot.Date.ToString("dd/MM/yyyy")}");
        //                //    //        }
        //                //    //        else if (entity_Sig.Open < (decimal)bb_Sig.Sma.Value
        //                //    //               && ((decimal)bb_Sig.Sma.Value - entity_Sig.Close) <= (entity_Sig.Close - (decimal)bb_Sig.LowerBand.Value)
        //                //    //               && ((decimal)bb_Sig.UpperBand.Value - entity_Sig.Close) >= (entity_Sig.Close - (decimal)bb_Sig.Sma.Value)
        //                //    //               && entity_Pivot.Close < (decimal)bb_Pivot.Sma.Value
        //                //    //               && ((decimal)bb_Pivot.Sma.Value - entity_Pivot.Close) <= (entity_Pivot.Close - (decimal)bb_Pivot.LowerBand.Value)
        //                //    //               && Math.Max(entity_Pre.Open, entity_Pre.Close) < (decimal)bb_Sig.Sma.Value)
        //                //    //        {
        //                //    //            Console.WriteLine($"{item}|2.SELL: {bb_Pivot.Date.ToString("dd/MM/yyyy")}");
        //                //    //        }
        //                //    //    }
        //                //    //    else
        //                //    //    {
        //                //    //        if (entity_Sig.Close < (decimal)bb_Sig.Sma.Value
        //                //    //            && (entity_Sig.Close - (decimal)bb_Sig.LowerBand.Value) <= ((decimal)bb_Sig.Sma.Value - entity_Sig.Close)
        //                //    //            && entity_Pivot.High < (decimal)bb_Pivot.Sma.Value
        //                //    //            && (entity_Pivot.Close - (decimal)bb_Pivot.LowerBand.Value) <= ((decimal)bb_Pivot.Sma.Value - entity_Pivot.Close))
        //                //    //        {
        //                //    //            Console.WriteLine($"{item}|1.BUY: {bb_Pivot.Date.ToString("dd/MM/yyyy")}");
        //                //    //        }
        //                //    //        else if (entity_Sig.Open > (decimal)bb_Sig.Sma.Value
        //                //    //                && (entity_Sig.Close - (decimal)bb_Sig.Sma.Value) <= ((decimal)bb_Sig.UpperBand.Value - entity_Sig.Close)
        //                //    //                && (entity_Sig.Close - (decimal)bb_Sig.LowerBand.Value) >= ((decimal)bb_Sig.Sma.Value - entity_Sig.Close)
        //                //    //                && entity_Pivot.Close > (decimal)bb_Pivot.Sma.Value
        //                //    //                && (entity_Pivot.Close - (decimal)bb_Pivot.Sma.Value) <= ((decimal)bb_Pivot.UpperBand.Value - entity_Pivot.Close)
        //                //    //                && Math.Min(entity_Pre.Open, entity_Pre.Close) > (decimal)bb_Sig.Sma.Value)
        //                //    //        {
        //                //    //            Console.WriteLine($"{item}|2.BUY: {bb_Pivot.Date.ToString("dd/MM/yyyy")}");
        //                //    //        }
        //                //    //    }




        //                //    //}
        //                //    //catch (Exception ex)
        //                //    //{
        //                //    //    break;
        //                //    //    //_logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
        //                //    //}

        //                //}
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"{item}| {ex.Message}");
        //            }
        //        }

        //        //foreach (var mes in lMesAll)
        //        //{
        //        //    Console.WriteLine(mes);
        //        //}
        //        Console.WriteLine($"Tong: {lModel.Sum(x => x.Rate)}%|W/L: {winTotal}/{lossTotal}");

        //        // Note:
        //        // + Nến xanh cắt lên MA20
        //        // + 2 nến ngay phía trước đều nằm dưới MA20
        //        // + Vol nến hiện tại > ít nhất 8/9 nến trước đó
        //        // + Giữ 2 tiếng? hoặc nến chạm BB trên
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
        //    }
        //}


        public async Task CheckAllDay()
        {
            try
            {
                decimal SL_RATE = 10m;//1.5,1.6,1.8,1.9,2
                int hour = 10;//1h,2h,3h,4h

                var lMesAll = new List<string>();
                var lModel = new List<LongMa20>();

                var winTotal = 0;
                var lossTotal = 0;
                var lTake = new List<string>
                {
                    "VNINDEX",
                    "DC4",
                    "GIL",
                    "GVR",
                    "DPG",
                    "CTG",
                    "BFC",
                    "VRE",
                    "PVB",
                    "GEX",
                    "SZC",
                    "HDG",
                    "BMP",
                    "TLG",
                    "VPB",
                    "DIG",
                    "KBC",
                    "HSG",
                    "PET",
                    "TNG",
                    "SBT",
                    "MSH",
                    "NAB",
                    "VGC",
                    "CSV",
                    "VCS",
                    "CSM",
                    "PHR",
                    "PVT",
                    "PC1",
                    "ASM",
                    "LAS",
                    "DXG",
                    "HCM",
                    "CTI",
                    "NHA",
                    "DPR",
                    "ANV",
                    "OCB",
                    "TVB",
                    "STB",
                    "HDC",
                    "POW",
                    "VSC",
                    "L18",
                    "DDV",
                    "VCI",
                    "GMD",
                    "NTP",
                    "KSV",
                    "NT2",
                    "TCM",
                    "LSS",
                    "GEG",
                    "HHS",
                    "MSB",
                    "TCH",
                    "VHC",
                    "PVD",
                    "FOX",
                    "SSI",
                    "NKG",
                    "BSI",
                    "ACB",
                    "REE",
                    "VHM",
                    "PAN",
                    "SIP",
                    "PTB",
                    "BSR",
                    "BID",
                    "PVS",
                    "CTS",
                    "FTS",
                    "HPG",
                    "DBC",
                    "MSR",
                    "THG",
                    "CTD",
                    "VOS",
                    "FMC",
                    "PHP",
                    "GAS",
                    "DCM",
                    "KSB",
                    "MSN",
                    "BVB",
                    "MBB",
                    "TRC",
                    "VPI",
                    "EIB",
                    "KDH",
                    "VCB",
                    "FPT",
                    "DRC",
                    "CMG",
                    "HAG",
                    "SHB",
                    "CII",
                    "CTR",
                    "IDC",
                    "GEE",
                    "NVB",
                    "BVS",
                    "BWE",
                    "HAX",
                    "QNS",
                    "VEA",
                    "TVS",
                    "DGC",
                    "HAH",
                    "NVL",
                    "PAC",
                    "AAA",
                    "TNH",
                    "ACV",
                    "BCC",
                    "FRT",
                    "HT1",
                    "SCS",
                    "TLH",
                    "MIG",
                    "SKG",
                    "DGC",
                    "VAB",
                    "NLG",
                    "HVN",
                    "HNG",
                    "PDR",
                    "VDS",
                    "SJE",
                    "PNJ",
                    "CEO",
                    "YEG",
                    "KLB",
                    "BCM",
                    "BVH",
                    "NTL",
                    "TDH",
                    "MBS",
                    "HUT",
                    "VIB",
                    "BAF",
                    "HHV",
                    "NDN",
                    "SGP",
                    "MCH",
                    "FCN",
                    "SCR",
                    "TCB",
                    "LPB",
                    "VTP",
                    "AGR",
                    "VCG",
                    "DPM",
                    "IDJ",
                    "DXS",
                    "OIL",
                    "AGG",
                    "VND",
                    "PSI",
                    "DHA",
                    "VIC",
                    "BCG",
                    "TPB",
                    "VIX",
                    "IJC",
                    "DGW",
                    "SBS",
                    "MFS",
                    "PLX",
                    "DRI",
                    "EVF",
                    "ORS",
                    "SAB",
                    "TDC",
                    "VNM",
                    "TV2",
                    "C4G",
                    "MWG",
                    "JVC",
                    "GDA",
                    "VGI",
                    "DSC",
                    "SMC",
                    "DTD",
                    "QCG",
                };
                foreach (var item in lTake.Skip(0).Take(5))
                {
                    var winCount = 0;
                    var lossCount = 0;
                    try
                    {
                        var lMes = new List<string>();

                        var lData = await _apiService.SSI_GetDataStock(item);
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

                            var near2 = lData15m.SkipLast(2).Last();
                            var near3 = lData15m.SkipLast(3).Last();
                            var near4 = lData15m.SkipLast(4).Last();
                            var near5 = lData15m.SkipLast(5).Last();

                            var bb2 = lbb.SkipLast(2).Last();
                            var bb3 = lbb.SkipLast(3).Last();
                            var bb4 = lbb.SkipLast(4).Last();
                            var bb5 = lbb.SkipLast(5).Last();

                            var rateVol = Math.Round(entity_Pivot.Volume / entity_Sig.Volume, 2);
                            if (rateVol > (decimal)0.6)
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
                var lTake = new List<string>
                {
                    "VNINDEX",
                    "DC4",
                    "GIL",
                    "GVR",
                    "DPG",
                    "CTG",
                    "BFC",
                    "VRE",
                    "PVB",
                    "GEX",
                    "SZC",
                    "HDG",
                    "BMP",
                    "TLG",
                    "VPB",
                    "DIG",
                    "KBC",
                    "HSG",
                    "PET",
                    "TNG",
                    "SBT",
                    "MSH",
                    "NAB",
                    "VGC",
                    "CSV",
                    "VCS",
                    "CSM",
                    "PHR",
                    "PVT",
                    "PC1",
                    "ASM",
                    "LAS",
                    "DXG",
                    "HCM",
                    "CTI",
                    "NHA",
                    "DPR",
                    "ANV",
                    "OCB",
                    "TVB",
                    "STB",
                    "HDC",
                    "POW",
                    "VSC",
                    "L18",
                    "DDV",
                    "VCI",
                    "GMD",
                    "NTP",
                    "KSV",
                    "NT2",
                    "TCM",
                    "LSS",
                    "GEG",
                    "HHS",
                    "MSB",
                    "TCH",
                    "VHC",
                    "PVD",
                    "FOX",
                    "SSI",
                    "NKG",
                    "BSI",
                    "ACB",
                    "REE",
                    "VHM",
                    "PAN",
                    "SIP",
                    "PTB",
                    "BSR",
                    "BID",
                    "PVS",
                    "CTS",
                    "FTS",
                    "HPG",
                    "DBC",
                    "MSR",
                    "THG",
                    "CTD",
                    "VOS",
                    "FMC",
                    "PHP",
                    "GAS",
                    "DCM",
                    "KSB",
                    "MSN",
                    "BVB",
                    "MBB",
                    "TRC",
                    "VPI",
                    "EIB",
                    "KDH",
                    "VCB",
                    "FPT",
                    "DRC",
                    "CMG",
                    "HAG",
                    "SHB",
                    "CII",
                    "CTR",
                    "IDC",
                    "GEE",
                    "NVB",
                    "BVS",
                    "BWE",
                    "HAX",
                    "QNS",
                    "VEA",
                    "TVS",
                    "DGC",
                    "HAH",
                    "NVL",
                    "PAC",
                    "AAA",
                    "TNH",
                    "ACV",
                    "BCC",
                    "FRT",
                    "HT1",
                    "SCS",
                    "TLH",
                    "MIG",
                    "SKG",
                    "DGC",
                    "VAB",
                    "NLG",
                    "HVN",
                    "HNG",
                    "PDR",
                    "VDS",
                    "SJE",
                    "PNJ",
                    "CEO",
                    "YEG",
                    "KLB",
                    "BCM",
                    "BVH",
                    "NTL",
                    "TDH",
                    "MBS",
                    "HUT",
                    "VIB",
                    "BAF",
                    "HHV",
                    "NDN",
                    "SGP",
                    "MCH",
                    "FCN",
                    "SCR",
                    "TCB",
                    "LPB",
                    "VTP",
                    "AGR",
                    "VCG",
                    "DPM",
                    "IDJ",
                    "DXS",
                    "OIL",
                    "AGG",
                    "VND",
                    "PSI",
                    "DHA",
                    "VIC",
                    "BCG",
                    "TPB",
                    "VIX",
                    "IJC",
                    "DGW",
                    "SBS",
                    "MFS",
                    "PLX",
                    "DRI",
                    "EVF",
                    "ORS",
                    "SAB",
                    "TDC",
                    "VNM",
                    "TV2",
                    "C4G",
                    "MWG",
                    "JVC",
                    "GDA",
                    "VGI",
                    "DSC",
                    "SMC",
                    "DTD",
                    "QCG",
                };
                foreach (var item in lTake.Skip(0))
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
}
