using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using Skender.Stock.Indicators;
using TestPr.DAL;
using TestPr.Model;
using TestPr.Utils;

namespace TestPr.Service
{
    public interface ITestService
    {
        Task MethodTest();
        Task MethodTestEntry();
        Task MethodTestTokenUnlock();
    }
    public class TestService : ITestService
    {
        private readonly ILogger<TestService> _logger;
        private readonly IAPIService _apiService;
        private readonly ITradingRepo _tradingRepo;
        public TestService(ILogger<TestService> logger, IAPIService apiService, ITradingRepo tradingRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _tradingRepo = tradingRepo;
        }

        public async Task MethodTest()
        {
            try
            {
                var lBinance = await StaticVal.BinanceInstance().SpotApi.ExchangeData.GetKlinesAsync("BTCUSDT", Binance.Net.Enums.KlineInterval.FourHour, limit: 1000);
                var lDataBinance = lBinance.Data.Select(x => new Quote
                {
                    Date = x.OpenTime,
                    Open = x.OpenPrice,
                    High = x.HighPrice,
                    Low = x.LowPrice,
                    Close = x.ClosePrice,
                    Volume = x.Volume,
                }).ToList();

                var lOrderBlock = new List<OrderBlock>();
                decimal MoneyPerUnit = 15;//15 per unit
                OrderBlock prepare = null;
                var flag = false;
                var indexFlag = -1;
                OutputModel output = null;
                var lRes = new List<OutputModel>();
                var indexPrepare = -1;

                for (var i = 300; i < lDataBinance.Count - 1; i++)
                {
                    var lData = lDataBinance.Take(i).ToList();
                    var cur = lData.Last();
                    //if(cur.Date.Year == 2024 && cur.Date.Month == 11 &&  cur.Date.Day == 4)
                    //{
                    //    var ss = 1;
                    //}

                    if(prepare != null && !flag)
                    {
                        if(prepare.Mode == (int)EOrderBlockMode.TopPinbar 
                            || prepare.Mode == (int)EOrderBlockMode.TopInsideBar)
                        {
                            if (cur.High >= prepare.Entry)
                            {
                                flag = true;
                                indexFlag = i;
                                output = new OutputModel
                                {
                                    BuyTime = cur.Date,
                                    StartPrice = prepare.Entry,
                                    ViThe = "Short",
                                    SignalTime = prepare.Date,
                                    TP = prepare.TP,
                                    SL = prepare.SL,
                                };
                            }
                        }
                        else
                        {
                            if (cur.Low <= prepare.Entry)
                            {
                                flag = true;
                                indexFlag = i;
                                output = new OutputModel
                                {
                                    BuyTime = cur.Date,
                                    StartPrice = prepare.Entry,
                                    ViThe = "Long",
                                    SignalTime = prepare.Date,
                                    TP = prepare.TP,
                                    SL = prepare.SL,
                                };
                            }
                        }
                    }

                    if (flag)
                    {
                        if (prepare.Mode == (int)EOrderBlockMode.TopPinbar
                           || prepare.Mode == (int)EOrderBlockMode.TopInsideBar)
                        {
                            if(cur.Low <= prepare.TP)
                            {
                                output.EndPrice = prepare.TP;
                                output.SellTime = cur.Date;
                                output.NamGiu = i - indexFlag;
                                output.TiLe = Math.Abs(Math.Round((-1 + prepare.TP/prepare.Entry) ,2));
                                output.TienLaiThucTe = (1 + output.TiLe) * MoneyPerUnit;
                                output.LaiLo = "Lai";
                                lRes.Add(output);
                                //reset
                                output = null;
                                flag = false;
                                indexFlag = -1;
                                prepare = null;
                            }
                            else if(cur.High >= prepare.SL)
                            {
                                output.EndPrice = prepare.SL;
                                output.SellTime = cur.Date;
                                output.NamGiu = i - indexFlag;
                                output.TiLe = -Math.Abs(Math.Round((-1 + prepare.SL / prepare.Entry), 2));
                                output.TienLaiThucTe = (1 + output.TiLe) * MoneyPerUnit;
                                output.LaiLo = "Lo";
                                lRes.Add(output);
                                //reset
                                output = null;
                                flag = false;
                                indexFlag = -1;
                                prepare = null;
                            }
                        }
                        else
                        {
                            if (cur.High >= prepare.TP)
                            {
                                output.EndPrice = prepare.TP;
                                output.SellTime = cur.Date;
                                output.NamGiu = i - indexFlag;
                                output.TiLe = Math.Abs(Math.Round((-1 + prepare.TP / prepare.Entry), 2));
                                output.TienLaiThucTe = (1 + output.TiLe) * MoneyPerUnit;
                                output.LaiLo = "Lai";
                                lRes.Add(output);
                                //reset
                                output = null;
                                flag = false;
                                indexFlag = -1;
                                prepare = null;
                            }
                            else if (cur.Low <= prepare.SL)
                            {
                                output.EndPrice = prepare.SL;
                                output.SellTime = cur.Date;
                                output.NamGiu = i - indexFlag;
                                output.TiLe = -Math.Abs(Math.Round((-1 + prepare.SL / prepare.Entry), 2));
                                output.TienLaiThucTe = (1 + output.TiLe) * MoneyPerUnit;
                                output.LaiLo = "Lo";
                                lRes.Add(output);
                                //reset
                                output = null;
                                flag = false;
                                indexFlag = -1;
                                prepare = null;
                            }
                        }
                        continue;
                    }

                    if(indexPrepare > 0 && (i - indexPrepare) > 10)
                    {
                        indexPrepare = -1;
                        prepare = null;
                    }

                    //ob
                    lOrderBlock = lData.GetOrderBlock(10);
                    var checkOrderBlock = cur.IsOrderBlock(lOrderBlock, 100);
                    if(checkOrderBlock.Item1)
                    {
                        prepare = checkOrderBlock.Item2;
                        indexPrepare = i;
                    }
                }

                var tongtien = lRes.Sum(x => x.TienLaiThucTe) - lRes.Count() * MoneyPerUnit;

                var tmp = 1;
                //var lBinance = await StaticVal.BinanceInstance().UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, BinanceInterval, limit: 500);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTest|EXCEPTION| {ex.Message}");
            }
        }

        public async Task MethodTestEntry()
        {
            try
            {
                var lTrading = _tradingRepo.GetAll();
                var lSymbol = lTrading.Select(x => x.s).Distinct();
                foreach (var item in lSymbol)
                {
                    var lval = lTrading.Where(x => x.s == item);
                    var lData = await _apiService.GetData(item, EInterval.M1);
                    foreach (var val in lval)
                    {
                        var lVisible = lData.Where(x => new DateTimeOffset(x.Date).ToUnixTimeSeconds() > val.d);
                        if (!lVisible.Any())
                            continue;
                        if(val.Side == 1)
                        {
                            var first = lVisible.FirstOrDefault(x => x.High >= (decimal)val.Entry);
                            if (first is null)
                                continue;

                            var lLast = lVisible.Where(x => x.Date > first.Date);
                            foreach (var itemlast in lLast)
                            {
                                if(itemlast.Low <= (decimal)val.SL)
                                {
                                    //Loss
                                    Console.WriteLine($"|LOSS| {JsonConvert.SerializeObject(val)}");
                                    break;
                                }
                                else if(itemlast.High >= (decimal)val.TP)
                                {
                                    //Win
                                    Console.WriteLine($"|WIN| {JsonConvert.SerializeObject(val)}");
                                    break;
                                }
                            }
                        }
                        else
                        {
                            var first = lVisible.FirstOrDefault(x => x.Low <= (decimal)val.Entry);
                            if (first is null)
                                continue;

                            var lLast = lVisible.Where(x => x.Date > first.Date);
                            foreach (var itemlast in lLast)
                            {
                                if (itemlast.High >= (decimal)val.SL)
                                {
                                    //Loss
                                    Console.WriteLine($"|LOSS| {JsonConvert.SerializeObject(val)}");
                                    break;
                                }
                                else if (itemlast.Low <= (decimal)val.TP)
                                {
                                    //Win
                                    Console.WriteLine($"|WIN| {JsonConvert.SerializeObject(val)}");
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTestEntry|EXCEPTION| {ex.Message}");
            }
        }

        public async Task MethodTestTokenUnlock()
        {
            try
            {

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TestService.MethodTestTokenUnlock|EXCEPTION| {ex.Message}");
            }
        }

        private List<TokenUnlock> _lTokenUnlock = new List<TokenUnlock>
        {
            new TokenUnlock{ s = "ZTX", noti_time = 20241012 , release_time = 20241016, cap = 22.23, value = 1 },
            new TokenUnlock{ s = "ZBCN", noti_time = 20241012 , release_time = 20241016, cap = 66.86, value = 1.93 },
            new TokenUnlock{ s = "RARE", noti_time = 20241012 , release_time = 20241017, cap = 82.06, value = 0.82 },
            new TokenUnlock{ s = "APE", noti_time = 20241012 , release_time = 20241017, cap = 436.42, value = 14.76 },
            new TokenUnlock{ s = "NEON", noti_time = 20241012 , release_time = 20241017, cap = 18.19, value = 12.59 },
            new TokenUnlock{ s = "ACE", noti_time = 20241012 , release_time = 20241018, cap = 83.57, value = 1.35 },
            new TokenUnlock{ s = "TORN", noti_time = 20241012 , release_time = 20241018, cap = 11.17 , value =  2},
            new TokenUnlock{ s = "SABAI", noti_time = 20241012 , release_time = 20241018, cap = 14.1 , value =  0.456},
            new TokenUnlock{ s = "ZKJ", noti_time = 20241012 , release_time = 20241019, cap = 75.31, value = 31.93 },
            new TokenUnlock{ s = "KARRAT", noti_time = 20241012 , release_time = 20241019, cap = 61.35 , value = 4.187 },
            new TokenUnlock{ s = "NUM", noti_time = 20241012 , release_time = 20241019, cap = 30.21, value = 0.34 },
            new TokenUnlock{ s = "UNFI", noti_time = 20241012 , release_time = 20241019, cap = 23.36, value =  6.6},
            new TokenUnlock{ s = "PIXEL", noti_time = 20241012 , release_time = 20241019, cap = 149.06, value =  3.69},
            new TokenUnlock{ s = "MERL", noti_time = 20241012 , release_time = 20241019, cap = 129.76, value =  3.876},
            new TokenUnlock{ s = "DEAI", noti_time = 20241013 , release_time = 20241020, cap = 51.99, value =  4.2},
            new TokenUnlock{ s = "HTM", noti_time = 20241013 , release_time = 20241020, cap = 17.27, value =  2.42},
            new TokenUnlock{ s = "MPLX", noti_time = 20241013 , release_time = 20241020, cap = 154.95, value =  3.24},
            new TokenUnlock{ s = "COMBO", noti_time = 20241014 , release_time = 20241021, cap = 30.05, value =  0.328},
            new TokenUnlock{ s = "LAZIO", noti_time = 20241015 , release_time = 20241021, cap = 13.95, value =  4.86},
            new TokenUnlock{ s = "VARA", noti_time = 20241015 , release_time = 20241021, cap = 11.85, value = 4.53 },
            new TokenUnlock{ s = "TAO", noti_time = 20241015 , release_time = 20241021, cap = 4650, value = 136 },
            new TokenUnlock{ s = "ML", noti_time = 20241015 , release_time = 20241021, cap = 10.31, value = 1.08 },
            new TokenUnlock{ s = "VRTX", noti_time = 20241015 , release_time = 20241021, cap = 23.05, value = 1.83 },
            new TokenUnlock{ s = "SCR", noti_time = 20241015 , release_time = 20241022, cap = 219.92, value = 220.46 },
        };


        public class TokenUnlock
        {
            public string s { get; set; }
            public long noti_time { get; set; }
            public long release_time { get; set; }
            public double cap { get; set; }
            public double value { get; set; }
        }
    }
}
