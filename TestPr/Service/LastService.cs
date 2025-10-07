using CoinUtilsPr;
using CoinUtilsPr.DAL;
using CoinUtilsPr.DAL.Entity;
using Skender.Stock.Indicators;

namespace TestPr.Service
{
    public interface ILastService
    {
        Task RealTemplate();
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
        public async Task RealTemplate()
        {
            try
            {
                var lTake = new List<string>
                {
                    //"BTCUSDT",
                    //"ETHUSDT",
                    //"XRPUSDT",
                    //"BNBUSDT",
                    //"SOLUSDT",
                    //"TRXUSDT",
                    //"ADAUSDT",
                    //"LINKUSDT",
                    //"XLMUSDT",
                    //"BCHUSDT",
                    //"AVAXUSDT",
                    //"CROUSDT",
                    //"HBARUSDT",
                    //"LTCUSDT",
                    //"TONUSDT",
                    //"DOTUSDT",
                    //"UNIUSDT",
                    //"SUIUSDT",
                    //"XMRUSDT",
                    //"ETCUSDT",
                    //"DOGEUSDT",
                    //"SHIBUSDT",
                    //"HYPEUSDT",
                    "QUICKUSDT"
                };
                
                foreach (var item in lTake)
                {
                    try
                    {
                        var l1H = await _apiService.GetData_Binance(item, EInterval.H1);
                        var count = l1H.Count();
                        var lSOS = new List<SOSDTO>();
                        for (int i = 1; i < count; i++)
                        {
                            var lDat = l1H.Take(i);
                            var itemSOS = lDat.Mutation();
                            if (itemSOS != null)
                            {
                                if (!lSOS.Any(x => x.sos.Date == itemSOS.sos.Date))
                                {
                                    lSOS.Add(itemSOS);
                                }
                            }
                        }

                        foreach (var itemSOS in lSOS)
                        {
                            Console.WriteLine($"{item}|{itemSOS.sos.Date.ToString("dd/MM/yyyy HH")}");
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    public static class clsEx
    {
        public static SOSDTO Mutation(this IEnumerable<Quote> lData)
        {
            try
            {
                var SoNenLonHonToiThieu = 9;
                if (lData.Count() < 100)
                    return null;

                var lVol = lData.Select(x => new Quote
                {
                    Date = x.Date,
                    Volume = x.Volume,
                    Close = x.Volume,
                }).GetSma(20);
                var lbb = lData.GetBollingerBands();
                var l1hEx = lData.Select(x => new QuoteEx
                {
                    Open = x.Open,
                    Close = x.Close,
                    High = x.High,
                    Low = x.Low,
                    Volume = x.Volume,
                    Date = x.Date,
                    MA20Vol = lVol.First(y => y.Date == x.Date).Sma,
                    bb = lbb.First(y => y.Date == x.Date)
                });
                /*
                    Nến xanh,
                    Vol lớn hơn 2 lần MAVol,
                    Close > Ma20,
                    50 nến gần nhất không có nến nào vượt SOS,
                    50 nến gần nhất (chỉ lấy nến xanh) không có nến nào vol lớn hơn vol của SOS,
                    Điểm check phải sau nến SOS 9 nến 
                 */
                var lSOS = l1hEx.TakeLast(72);
                foreach (var itemSOS in lSOS.Where(x => x.MA20Vol != null && x.Volume > 1.5m * (decimal)x.MA20Vol
                                                    //&& (x.High > (decimal)x.bb.UpperBand || x.Low < (decimal)x.bb.LowerBand))
                                                    && (x.Low < (decimal)x.bb.LowerBand && x.Open > x.Close))
                                            .OrderByDescending(x => x.Date))
                {
                    //Console.WriteLine($"{itemSOS.Date.ToString("dd/MM/yyyy HH")}");
                    //var lcheck = l1hEx.Where(x => x.Date < itemSOS.Date).TakeLast(50);
                    //if (lcheck.Any(x => x.Close > itemSOS.Close
                    //                || (x.Volume > itemSOS.Volume && x.Close > x.Open)))
                    //    continue;

                    //var lNextCheck = l1hEx.Where(x => x.Date > itemSOS.Date).Take(SoNenLonHonToiThieu);
                    //if (lNextCheck.Count() < SoNenLonHonToiThieu)
                    //    continue;

                    var output = new SOSDTO
                    {
                        sos = lData.First(x => x.Date == itemSOS.Date)
                    };

                    return output;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return null;
        }
    }
}
