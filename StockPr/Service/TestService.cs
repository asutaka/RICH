using Skender.Stock.Indicators;
using StockPr.DAL;
using StockPr.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace StockPr.Service
{
    public interface ITestService
    {
        Task Check2Buy();
        Task Check2Sell();
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
                    "ITA",
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

                                var side = 0;
                                var cur = lData15m.First(x => x.Date == ma20.Date);
                                var rsi = lrsi.First(x => x.Date == ma20.Date);
                                var minOpenClose = Math.Min(cur.Open, cur.Close);

                                if (cur.Close >= cur.Open
                                    || ma20.Sma is null
                                    || rsi.Rsi > 40
                                    //|| cur.Low >= (decimal)ma20.LowerBand.Value
                                    || Math.Abs(minOpenClose - (decimal)ma20.LowerBand.Value) > Math.Abs((decimal)ma20.Sma.Value - minOpenClose)
                                    )
                                    continue;

                                var rsiPivot = lrsi.FirstOrDefault(x => x.Date > ma20.Date);
                                //if (rsiPivot is null || rsiPivot.Rsi > 35 || rsiPivot.Rsi < 25)
                                //    continue;

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

                                var buy = lData15m.FirstOrDefault(x => x.Date > rsiPivot.Date);
                                if (buy is null)
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
                                var mes = $"{item}|{winloss}|BUY|{cur.Date.ToString("dd/MM/yyyy HH:mm")}|E: {eClose.Date.ToString("dd/MM/yyyy HH:mm")}|{rate}%|TPMax: {maxTP}%|SLMax: {maxSL}%|RateEntry: {rateEntry}%|RSI: {rsiPivot.Rsi}";
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
