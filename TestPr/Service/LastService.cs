using Bybit.Net.Enums;
using CoinUtilsPr;
using CoinUtilsPr.DAL;
using CoinUtilsPr.DAL.Entity;
using CoinUtilsPr.Model;
using MongoDB.Driver;
using Skender.Stock.Indicators;

namespace TestPr.Service
{
    public interface ILastService
    {
        Task RealTemplate();
        Task ProTradeEx();
        Task fake();
    }
    //Phải dùng vol là tín hiệu đầu tiên vì nó là tín hiệu nhạy nhất 
    public class LastService : ILastService
    {
        private readonly ILogger<TestService> _logger;
        private readonly IAPIService _apiService;
        private readonly ISymbolRepo _symRepo;
        private int _COUNT = 0;
        public LastService(ILogger<TestService> logger, IAPIService apiService, ISymbolRepo symRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _symRepo = symRepo;
        }

        public async Task fake()
        {
            var quotes = await _apiService.GetData_Binance("BTCUSDT", EInterval.H1);
            decimal capital = 30m;
            decimal peak = capital;
            int win = 0, total = 0;
            List<decimal> equity = new() { capital };

            for (int i = 50; i < quotes.Count - 1; i++)
            {
                var window = quotes.Take(i + 1).ToList();
                var entry = window.GetEntry();
                if (entry == null) continue;

                Console.WriteLine($"{entry.entity.Date.ToString("dd/MM/yyyy HH")}|{entry.ratio}%");

                //total++;
                var result = SimulateTrade2(entry, i, quotes);
                if (result > 0)
                {
                    win++;
                }
                total++;

                //capital += result;
                //peak = Math.Max(peak, capital);
                //equity.Add(capital);

                //if (result > 0) win++;
                //Console.WriteLine($"{entry.entity.Date:dd/MM HH:mm} | {(SignalStrength)entry.Strength} | Lời ${result:F2} | Vốn ${capital:F1}");
            }

            decimal maxDD = equity.Count > 1 ? 100 * (peak - equity.Min()) / peak : 0;

            Console.WriteLine("==================================================");
            Console.WriteLine($"TỔNG LỆNH: {total}");
            Console.WriteLine($"WINRATE: {100.0 * win / total:F1}%");
            Console.WriteLine($"MAX DD: {maxDD:F1}%");
            Console.WriteLine($"30$ → {capital:F0}$ SAU 60 NGÀY");
            Console.WriteLine("==================================================");
            Console.WriteLine("NHẤN PHÍM BẤT KÌ ĐỂ THOÁT...");
            Console.ReadKey();
        }

        static decimal SimulateTrade2(ProModel entry, int index, List<Quote> futureQuotes)
        {
            decimal sl = entry.sl;
            decimal profit = 0m;                         
            bool tp1Done = false, tp2Done = false;

            var bbList = futureQuotes.GetBollingerBands(20, 2).ToList();
            //decimal fib = futureQuotes.Take(50).Min(q => q.Low) + (futureQuotes.Take(50).Max(q => q.High) - futureQuotes.Take(50).Min(q => q.Low)) * 1.618m;

            var isFlagSellAll = false;
            for (int i = index + 1; i < futureQuotes.Count(); i++)
            {
                var q = futureQuotes.ElementAt(i);

                // SL
                if (q.Low <= sl) return 0;

                //// TP1 – MA20
                if (!tp1Done && q.Close >= (decimal)bbList[i].Sma.Value)
                {
                    //profit += risk * 2.03m;  // 40%
                    //tp1Done = true;
                    isFlagSellAll = true;
                    return 1;
                }

                //// TP2 – UpperBand
                //if (!tp2Done && q.High >= (decimal)bbList[i].UpperBand.Value * 0.999m)
                //{
                //    //profit += risk * 2.03m;  // 40%
                //    //tp2Done = true;
                //    return 1;
                //}

                //// TP3 – Fib 1.618
                //if (q.High >= fib)
                //{
                //    profit += risk * 3.1m;   // 20%
                //    return profit;
                //}
            }
            //return profit;
            return 0;
        }

        //static decimal SimulateTrade(ProModel entry, IEnumerable<Quote> futureQuotes)
        //{
        //    var future = futureQuotes.ToList();
        //    if (future.Count < 50) return -entry.risk;

        //    decimal sl = entry.sl;
        //    decimal risk = entry.risk;
        //    decimal profit = 0m;                         // ĐÃ SỬA: không mặc định thua
        //    bool tp1Done = false, tp2Done = false;

        //    var smaList = future.GetSma(20).ToList();
        //    var bbList = future.GetBollingerBands(20, 2).ToList();
        //    decimal fib = future.Take(50).Min(q => q.Low) + (future.Take(50).Max(q => q.High) - future.Take(50).Min(q => q.Low)) * 1.618m;

        //    for (int i = 0; i < future.Count && i < 200; i++)
        //    {
        //        var q = future[i];

        //        // SL
        //        if (q.Low <= sl) return -risk;

        //        // TP1 – MA20
        //        if (!tp1Done && i < smaList.Count && smaList[i].Sma.HasValue && q.High >= (decimal)smaList[i].Sma.Value)
        //        {
        //            profit += risk * 2.03m;  // 40%
        //            tp1Done = true;
        //        }

        //        // TP2 – UpperBand
        //        if (!tp2Done && i < bbList.Count && bbList[i].UpperBand.HasValue && q.High >= (decimal)bbList[i].UpperBand.Value * 0.999m)
        //        {
        //            profit += risk * 2.03m;  // 40%
        //            tp2Done = true;
        //        }

        //        // TP3 – Fib 1.618
        //        if (q.High >= fib)
        //        {
        //            profit += risk * 3.1m;   // 20%
        //            return profit;
        //        }
        //    }
        //    return profit;
        //}

        static decimal CalculateFib(IEnumerable<Quote> q)
        {
            var low = q.Min(x => x.Low);
            var high = q.Max(x => x.High);
            return low + (high - low) * 1.618m;
        }

        public async Task ProTradeEx()
        {
            try
            {
                var l1H = await _apiService.GetData_Binance("BTCUSDT", EInterval.H1);
                var count = l1H.Count;
                for (int i = 0; i < count; i++)
                {
                    var lDat = l1H.Take(i).ToList();
                    var entry = lDat.GetEntry();
                    if(entry != null)
                    {
                        var mes = $"{entry.entity.Date.ToString("dd/MM/yyyy HH")}|Entry: {entry.entity.Close}|SL: {entry.sl}";
                        Console.WriteLine(mes);
                        Console.WriteLine($"    ==> {entry.mes}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("END");
        }

        public async Task RealTemplate()
        {
            try
            {
                //var lAll = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, limit: 1000);
                //var lUsdt = lAll.Data.List.Where(x => x.QuoteAsset == "USDT" && !x.Name.StartsWith("1000")).Select(x => x.Name);
                //var lTake = lUsdt.Skip(0).Take(1000);
                var lTake = new List<string>
                {
                    "AAVEUSDT",
"ADAUSDT",
"AERGOUSDT",
"AI16ZUSDT",
"AINUSDT",
"AIUSDT",
"ALICEUSDT",
"APTUSDT",
"ARKMUSDT",
"ARKUSDT",
"ARPAUSDT",
"ASRUSDT",
"ASTERUSDT",
"AUDIOUSDT",
"AVAXUSDT",
"AWEUSDT",
"B2USDT",
"BABYUSDT",
"BANANAUSDT",
"BBUSDT",
"BCHUSDT",
"BERAUSDT",
"BLESSUSDT",
"BMTUSDT",
"BSVUSDT",
"BTCUSDT",
"CATIUSDT",
"CFXUSDT",
"CGPTUSDT",
"CKBUSDT",
"COSUSDT",
"COWUSDT",
"CROSSUSDT",
"CVCUSDT",
"DEGENUSDT",
"DIAUSDT",
"DOGSUSDT",
"DOLOUSDT",
"DRIFTUSDT",
"EGLDUSDT",
"ENJUSDT",
"ENSUSDT",
"ESPORTSUSDT",
"ETHUSDT",
"FIDAUSDT",
"FIOUSDT",
"FLOWUSDT",
"FORTHUSDT",
"GASUSDT",
"GLMUSDT",
"GRIFFAINUSDT",
"GUSDT",
"HAEDALUSDT",
"HFTUSDT",
"HIGHUSDT",
"HIPPOUSDT",
"HIVEUSDT",
"HOOKUSDT",
"HUSDT",
"ICPUSDT",
"IDUSDT",
"INITUSDT",
"INJUSDT",
"INUSDT",
"IOSTUSDT",
"IOTXUSDT",
"IOUSDT",
"JELLYJELLYUSDT",
"JOEUSDT",
"KAVAUSDT",
"KERNELUSDT",
"KNCUSDT",
"LIGHTUSDT",
"LTCUSDT",
"MANAUSDT",
"MANTAUSDT",
"MBOXUSDT",
"MELANIAUSDT",
"MEWUSDT",
"MOCAUSDT",
"MOVEUSDT",
"MOVRUSDT",
"MTLUSDT",
"MUSDT",
"NEARUSDT",
"NEOUSDT",
"NKNUSDT",
"NMRUSDT",
"NXPCUSDT",
"OBOLUSDT",
"OGNUSDT",
"OLUSDT",
"OMUSDT",
"ONEUSDT",
"ONGUSDT",
"OPUSDT",
"ORCAUSDT",
"OXTUSDT",
"PAXGUSDT",
"PENGUUSDT",
"PEOPLEUSDT",
"PERPUSDT",
"PHAUSDT",
"PIPPINUSDT",
"POLYXUSDT",
"POWRUSDT",
"PROMPTUSDT",
"PROMUSDT",
"PUNDIXUSDT",
"QUICKUSDT",
"RESOLVUSDT",
"REZUSDT",
"RLCUSDT",
"RONINUSDT",
"ROSEUSDT",
"RPLUSDT",
"RUNEUSDT",
"SANDUSDT",
"SCRTUSDT",
"SEIUSDT",
"SKLUSDT",
"SKYUSDT",
"SLPUSDT",
"SOLUSDT",
"SOONUSDT",
"SPELLUSDT",
"SPXUSDT",
"STEEMUSDT",
"STORJUSDT",
"SUSHIUSDT",
"SWARMSUSDT",
"SXPUSDT",
"SXTUSDT",
"SYRUPUSDT",
"SYSUSDT",
"TAOUSDT",
"THETAUSDT",
"TOKENUSDT",
"TONUSDT",
"TOWNSUSDT",
"TRUMPUSDT",
"TRUUSDT",
"UMAUSDT",
"VANRYUSDT",
"VELODROMEUSDT",
"VETUSDT",
"VFYUSDT",
"VINEUSDT",
"WCTUSDT",
"YFIUSDT",
"YGGUSDT",
"ZECUSDT",
"ZEREBROUSDT",
                };

                decimal total = 0;
                int win = 0, loss = 0;
                var lCoin = new List<string>();
                foreach (var item in lTake)
                {
                    try
                    {
                        var l1H = await _apiService.GetData_Binance(item, EInterval.H1);
                        var lbb = l1H.GetBollingerBands();
                        var count = l1H.Count();
                        var lSOS = new List<SOSDTO>();
                        for (int i = 1; i < count; i++)
                        {
                            var lDat = l1H.Take(i);
                            var itemSOS = lDat.LAST_Mutation();
                            if (itemSOS != null)
                            {
                                if (!lSOS.Any(x => x.sos.Date == itemSOS.Date))
                                {
                                    //Console.WriteLine($"{item}|{itemSOS.Date.ToString("dd/MM/yyyy HH")}");
                                    lSOS.Add(new SOSDTO
                                    {
                                        sos = itemSOS,
                                    });
                                }
                            }
                        }

                        var lDetect = new List<SOSDTO>();
                        foreach (var itemSOS in lSOS)
                        {
                            var lDat = l1H.Where(x => x.Date <= itemSOS.sos.Date.AddHours(6)).TakeLast(8);
                            var countCheck = lDat.Count();
                            for (int i = 0; i < countCheck - 3; i++) 
                            {
                                var item1 = lDat.ElementAt(i);
                                var item2 = lDat.ElementAt(i + 1);
                                var item3 = lDat.ElementAt(i + 2);
                                var itemDetect = itemSOS.LAST_DetectTopBOT(item1, item2, item3);
                                if(itemDetect != null)
                                {
                                    itemSOS.sos_real = itemDetect;
                                    lDetect.Add(itemSOS);
                                    break;
                                }
                            }
                        }

                        var lEntry = new List<SOSDTO>();
                        SOSDTO prev = null;
                        foreach (var itemSOS in lDetect.OrderBy(x => x.sos_real.Date))
                        {
                            if (prev != null && prev.sos_real.Date == itemSOS.sos_real.Date)
                                continue;
                            prev = itemSOS;
                            if (itemSOS.sos_real.Close > itemSOS.sos_real.Open)
                            {

                            }
                            else
                            {
                                //Console.WriteLine($"{item}|{itemSOS.sos.Date.ToString("dd/MM/yyyy HH")}|{itemSOS.sos_real.Date.ToString("dd/MM/yyyy HH")}");
                                var lDat = l1H.Where(x => x.Date > itemSOS.sos_real.Date).Skip(5).Take(15);
                                var countCheck = lDat.Count();
                                for (int i = 0; i < countCheck - 3; i++)
                                {
                                    var item1 = lDat.ElementAt(i);
                                    var item2 = lDat.ElementAt(i + 1);
                                    var item3 = lDat.ElementAt(i + 2);
                                    var itemDetect = itemSOS.LAST_DetectEntry(item1, item2, item3);
                                    if (itemDetect != null)
                                    {
                                        itemSOS.signal = itemDetect;
                                        itemSOS.entry = item3;
                                        //Kiểm tra volume của đáy 2 phải nhỏ hơn 2 lần đáy 1 
                                        //Trong khoảng 2 đáy ko được có nến vol đỏ vượt đáy 1
                                        //Đáy 2 không được vượt MA20

                                        //entry tới upper > 2%
                                        //đáy 2 cách đáy 1 tối thiểu 5 nến
                                        //đáy 2 <= 1/2 (H + L) đáy 1

                                        var max = Math.Max(itemSOS.sos.Volume, itemSOS.sos_real.Volume);
                                        var lFilter = l1H.Where(x => x.Open >= x.Close && x.Date > itemSOS.sos_real.Date && x.Date <= itemSOS.signal.Date);
                                        if(!lFilter.Any())
                                        {
                                            break;
                                        }    
                                        var maxRange = lFilter.Max(x => x.Volume);
                                        var bb = lbb.First(x => x.Date == itemSOS.entry.Date);
                                        var rateEntry = Math.Round(100 * (-1 + (decimal)bb.UpperBand / itemSOS.entry.Close), 2);
                                        var count2Day = l1H.Count(x => x.Date >= itemSOS.sos_real.Date && x.Date < itemSOS.signal.Date);
                                        var avgPrice1 = 0.5m * (Math.Max(itemSOS.sos.High, itemSOS.sos_real.High) + Math.Min(itemSOS.sos.Low, itemSOS.sos_real.Low));

                                        //Kiểm tra nếu 20 nến liền trước không có nến nào vượt MA20 thì bỏ qua
                                        var lCheck20 = l1H.Where(x => x.Date < item3.Date).TakeLast(20);
                                        var isPassCheck20 = false;
                                        foreach (var item20 in lCheck20)
                                        {
                                            var bb20 = lbb.First(x => x.Date == item20.Date);
                                            if (item20.Close > (decimal)bb20.Sma)
                                            {
                                                isPassCheck20 = true;
                                                break;
                                            }
                                        }
                                        if (!isPassCheck20)
                                        {
                                            break;
                                        }

                                        if (max > 2 * itemSOS.signal.Volume
                                            && maxRange < max
                                            && itemSOS.signal.Close < (decimal)bb.Sma
                                            && rateEntry > 2
                                            && count2Day >= 5
                                            && itemSOS.signal.Close <= avgPrice1)
                                        {
                                            lEntry.Add(itemSOS);
                                            //Console.WriteLine($"{item}|{itemSOS.signal.Date.ToString("dd/MM/yyyy HH")}|{itemSOS.entry.Date.ToString("dd/MM/yyyy HH")}");
                                        }
                                        

                                        
                                        ////Chỉ lấy nến nếu close vượt ra ngoài Bollingerband
                                        ////if(itemDetect.sos_real.Close > (decimal)bb.UpperBand
                                        ////    || itemDetect.sos_real.Close < (decimal)bb.LowerBand)
                                        ////{
                                        //lDetect.Add(itemDetect);
                                        //}
                                        break;
                                    }
                                }
                            }
                        }

                        int tongWin = 0, tongLoss = 0;
                        decimal tongRate = 0;
                        var lMes = new List<string>();
                        foreach (var itemEntry in lEntry)
                        {
                            var lTP = l1H.Where(x => x.Date > itemEntry.entry.Date).Take(30);
                            foreach (var itemTP in lTP)
                            {
                                var bb = lbb.First(x => x.Date == itemTP.Date);
                                var eEntry = itemEntry.entry;

                                var rate = Math.Round(100 * (-1 + itemTP.Low / eEntry.Close), 2);
                                if(rate < -5)
                                {
                                    tongRate -= 5;
                                    tongLoss++;
                                    loss++;
                                    total -= 5;
                                    lMes.Add($"{item}|SOS: {itemEntry.sos_real.Date.ToString("dd/MM/yyyy HH")}|Signal: {itemEntry.signal.Date.ToString("dd/MM/yyyy HH")}|SL: {itemTP.Date.ToString("dd/MM/yyyy HH")}|Rate: {rate}%");
                                    break;
                                }

                                if(itemTP.Close > (decimal)bb.UpperBand)
                                {
                                    tongRate += rate;
                                    tongWin++;
                                    win++;
                                    rate = Math.Round(100 * (-1 + itemTP.Close / eEntry.Close), 2);
                                    total += rate;
                                    lMes.Add($"{item}|Entry: {eEntry.Date.ToString("dd/MM/yyyy HH")}|TP: {itemTP.Date.ToString("dd/MM/yyyy HH")}|Rate: {rate}%");
                                    break;
                                }
                            }
                        }

                        foreach (var itemMes in lMes)
                        {
                            Console.WriteLine(itemMes);
                        }
                        if(tongWin >= tongLoss
                            && tongWin > 0
                            && tongRate > 0)
                        {
                            lCoin.Add(item);
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }

                Console.WriteLine($"Total: {total}%|W/L: {win}/{loss}");
                //foreach (var item in lCoin)
                //{
                //    Console.WriteLine($"\"{item}\",");
                //}
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    public static class Ext
    {
        public static decimal? Sma(this Quote q, int p) => q.Date == DateTime.UtcNow ? 0 : null; // dummy
        public static decimal? UpperBand(this Quote q, int p, int s) => q.Date == DateTime.UtcNow ? 0 : null;
    }
}
