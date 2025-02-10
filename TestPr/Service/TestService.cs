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
            new TokenUnlock{ s = "C98", noti_time = 20241016 , release_time = 20241023, cap = 106.62, value = 2.07 },
            new TokenUnlock{ s = "FIDA", noti_time = 20241016 , release_time = 20241023, cap = 51.6, value = 6.42 },
            new TokenUnlock{ s = "GTAI", noti_time = 20241018 , release_time = 20241025, cap = 27.39, value = 2.63 },
            new TokenUnlock{ s = "ALT", noti_time = 20241018 , release_time = 20241025, cap = 255.55, value = 21.87 },
            new TokenUnlock{ s = "RAD", noti_time = 20241018 , release_time = 20241025, cap = 56.76, value = 1.7 },
            new TokenUnlock{ s = "AURY", noti_time = 20241019 , release_time = 20241026, cap = 21.63, value = 0.466 },
            new TokenUnlock{ s = "BREED", noti_time = 20241019 , release_time = 20241026, cap = 11.14, value = 0.61 },
            new TokenUnlock{ s = "NFP", noti_time = 20241020 , release_time = 20241027, cap = 66.97, value = 2.38 },
            new TokenUnlock{ s = "EDU", noti_time = 20241021 , release_time = 20241028, cap = 192.55, value = 11.13 },
            new TokenUnlock{ s = "MAV", noti_time = 20241021 , release_time = 20241028, cap = 83.5, value = 10.1 },
            new TokenUnlock{ s = "VIC", noti_time = 20241022 , release_time = 20241029, cap = 35.18, value =  7.27},
            new TokenUnlock{ s = "ACA", noti_time = 20241022 , release_time = 20241029, cap = 67.08, value =  0.7},
            new TokenUnlock{ s = "PORTAL", noti_time = 20241022 , release_time = 20241029, cap = 103.25, value = 8 },
            new TokenUnlock{ s = "WLTH", noti_time = 20241023 , release_time = 20241030, cap = 11.06, value =  1.44},
            new TokenUnlock{ s = "TIA", noti_time = 20241024 , release_time = 20241031, cap = 1300, value = 1040 },
            new TokenUnlock{ s = "HOOK", noti_time = 20241025 , release_time = 20241101, cap = 84.28, value = 3.6 },
            new TokenUnlock{ s = "BICO", noti_time = 20241025 , release_time = 20241101, cap = 173.44, value = 4.39 },
            new TokenUnlock{ s = "IMX", noti_time = 20241025 , release_time = 20241101, cap = 2400, value = 47.67 },
            new TokenUnlock{ s = "AVA", noti_time = 20241025 , release_time = 20241101, cap = 26.38, value = 0.54 },
            new TokenUnlock{ s = "VANRY", noti_time = 20241025 , release_time = 20241101, cap = 136.19, value = 2.45 },
            new TokenUnlock{ s = "ETHDYDX", noti_time = 20241025 , release_time = 20241101, cap = 241.38 , value = 8.33 },
            new TokenUnlock{ s = "OGN", noti_time = 20241025 , release_time = 20241101, cap = 58.29 , value = 1.36 },
            new TokenUnlock{ s = "ORBR", noti_time = 20241026 , release_time = 20241102, cap = 238.83 , value = 8.8 },
            new TokenUnlock{ s = "TAI", noti_time = 20241026 , release_time = 20241102, cap = 64.96 , value = 3.67 },
            new TokenUnlock{ s = "NTRN", noti_time = 20241027 , release_time = 20241103, cap = 101.37 , value = 3.47 },
            new TokenUnlock{ s = "DAR", noti_time = 20241028 , release_time = 20241104, cap = 91.29 , value = 3.1 },
            new TokenUnlock{ s = "AI", noti_time = 20241028 , release_time = 20241104, cap = 87.1 , value = 4.05 },
            new TokenUnlock{ s = "G", noti_time = 20241029 , release_time = 20241105, cap = 234.59 , value = 9.29 },
            new TokenUnlock{ s = "WIFI", noti_time = 20241031 , release_time = 20241107, cap = 10.79, value = 0.4 },
            new TokenUnlock{ s = "BOSON", noti_time = 20241031 , release_time = 20241107, cap = 30.43 , value = 0.55 },
            new TokenUnlock{ s = "MYRIA", noti_time = 20241031 , release_time = 20241107, cap = 52.86 , value = 1.67 },
            new TokenUnlock{ s = "DEVVE", noti_time = 20241031 , release_time = 20241107, cap = 17.67 , value = 2.32 },
            new TokenUnlock{ s = "ACH", noti_time = 20241031 , release_time = 20241107, cap = 176.78 , value = 2.49 },
            new TokenUnlock{ s = "HFT", noti_time = 20241031 , release_time = 20241107, cap = 64.23, value = 2.18 },
            new TokenUnlock{ s = "ATA", noti_time = 20241031 , release_time = 20241107, cap = 46.5, value = 2.18 },
            new TokenUnlock{ s = "SD", noti_time = 20241101 , release_time = 20241108, cap = 13.18, value = 0.446 },
            new TokenUnlock{ s = "CLY", noti_time = 20241101 , release_time = 20241108, cap = 11.27, value = 0.16 },
            new TokenUnlock{ s = "ENS", noti_time = 20241101 , release_time = 20241108, cap = 558.31, value = 24.55 },
            new TokenUnlock{ s = "SCA", noti_time = 20241101 , release_time = 20241108, cap = 16.2, value =  1.48},
            new TokenUnlock{ s = "GMT", noti_time = 20241102 , release_time = 20241109, cap = 367.77, value = 11.64 },
            new TokenUnlock{ s = "CGPT", noti_time = 20241103 , release_time = 20241110, cap = 85.66, value = 2.33 },
            new TokenUnlock{ s = "CHEEL", noti_time = 20241103 , release_time = 20241110, cap = 621.19, value = 127.53 },
            new TokenUnlock{ s = "CETUS", noti_time = 20241103 , release_time = 20241110, cap = 51.59, value = 3.6 },
            new TokenUnlock{ s = "GODS", noti_time = 20241104 , release_time = 20241111, cap = 51.61, value = 1 },
            new TokenUnlock{ s = "GFI", noti_time = 20241104 , release_time = 20241111, cap = 37.47, value = 1.6 },
            new TokenUnlock{ s = "BOBA", noti_time = 20241105 , release_time = 20241112, cap = 28.83, value = 3.78 },
            new TokenUnlock{ s = "APT", noti_time = 20241105 , release_time = 20241112, cap = 4210, value = 91.79 },
            new TokenUnlock{ s = "OAS", noti_time = 20241105 , release_time = 20241112, cap = 87.44, value = 5.07 },
            new TokenUnlock{ s = "GFAL", noti_time = 20241106 , release_time = 20241113, cap = 33.96, value = 1.26 },
            new TokenUnlock{ s = "SWEAT", noti_time = 20241106 , release_time = 20241113, cap = 54.96, value = 2.93 },
            new TokenUnlock{ s = "VOXEL", noti_time = 20241107 , release_time = 20241114, cap = 31.59, value = 0.6 },
            new TokenUnlock{ s = "ALI", noti_time = 20241107 , release_time = 20241114, cap = 69.06, value = 1.12 },
            new TokenUnlock{ s = "RSS3", noti_time = 20241107 , release_time = 20241114, cap = 78.66, value = 2 },
            new TokenUnlock{ s = "HIGH", noti_time = 20241107 , release_time = 20241114, cap = 88.51, value = 1.47 },
            new TokenUnlock{ s = "CYBER", noti_time = 20241108 , release_time = 20241115, cap = 90.98, value = 9.72 },
            new TokenUnlock{ s = "ALICE", noti_time = 20241108 , release_time = 20241115, cap = 73.22, value = 2.21 },
            new TokenUnlock{ s = "SEI", noti_time = 20241108 , release_time = 20241115, cap = 1690, value = 51.97 },
            new TokenUnlock{ s = "ZTX", noti_time = 20241109 , release_time = 20241116, cap = 21.99, value = 1.26 },
            new TokenUnlock{ s = "PORTO", noti_time = 20241109 , release_time = 20241116, cap = 14.8, value = 7.38 },
            new TokenUnlock{ s = "ZBCN", noti_time = 20241109 , release_time = 20241116, cap = 63.19, value = 2.17 },
            new TokenUnlock{ s = "RARE", noti_time = 20241110 , release_time = 20241117, cap = 87.51, value = 1.63 },
            new TokenUnlock{ s = "APE", noti_time = 20241110 , release_time = 20241117, cap = 826.78, value = 16.89 },
            new TokenUnlock{ s = "NEON", noti_time = 20241110 , release_time = 20241117, cap = 25.47, value = 21.99 },
            new TokenUnlock{ s = "ACE", noti_time = 20241111 , release_time = 20241118, cap = 94.79, value = 3.79 },
            new TokenUnlock{ s = "AURORA", noti_time = 20241111 , release_time = 20241118, cap = 76.92, value = 2.55 },
            new TokenUnlock{ s = "ROSE", noti_time = 20241111 , release_time = 20241118, cap = 583.87, value = 14.45 },
            new TokenUnlock{ s = "TORN", noti_time = 20241111 , release_time = 20241118, cap = 13.17, value = 0.43 },
            new TokenUnlock{ s = "SABAI", noti_time = 20241111 , release_time = 20241118, cap = 10.27, value = 1.15 },
            new TokenUnlock{ s = "QI", noti_time = 20241112 , release_time = 20241119, cap = 78.14, value = 1.67 },
            new TokenUnlock{ s = "ZKJ", noti_time = 20241112 , release_time = 20241119, cap = 82.36, value = 18.61 },
            new TokenUnlock{ s = "KARRAT", noti_time = 20241112 , release_time = 20241119, cap = 73.4, value = 6.35 },
            new TokenUnlock{ s = "NUM", noti_time = 20241112 , release_time = 20241119, cap = 33.12, value = 0.5 },
            new TokenUnlock{ s = "UNFI", noti_time = 20241112 , release_time = 20241119, cap = 10.26, value = 0.1 },
            new TokenUnlock{ s = "PIXEL", noti_time = 20241112 , release_time = 20241119, cap = 237, value = 11.09 },
            new TokenUnlock{ s = "MERL", noti_time = 20241112 , release_time = 20241119, cap = 208.45, value = 11.21 },
            new TokenUnlock{ s = "DEAI", noti_time = 20241113 , release_time = 20241120, cap = 53.92, value = 15.56 },
            new TokenUnlock{ s = "HTM", noti_time = 20241113 , release_time = 20241120, cap = 17.95, value = 2 },
            new TokenUnlock{ s = "SHC", noti_time = 20241113 , release_time = 20241120, cap = 10, value = 0.22 },
            new TokenUnlock{ s = "BCUT", noti_time = 20241113 , release_time = 20241120, cap = 14.47, value = 1.58 },
            new TokenUnlock{ s = "VARA", noti_time = 20241114 , release_time = 20241121, cap = 12.09, value = 4.16 },
            new TokenUnlock{ s = "TAO", noti_time = 20241114 , release_time = 20241121, cap = 3970, value = 116.27 },
            new TokenUnlock{ s = "VRTX", noti_time = 20241114 , release_time = 20241121, cap = 21.62, value = 1.65 },
            new TokenUnlock{ s = "C98", noti_time = 20241116 , release_time = 20241123, cap = 117.66, value = 2.2 },
            new TokenUnlock{ s = "FIDA", noti_time = 20241116 , release_time = 20241123, cap = 108.86, value = 5.6 },
            new TokenUnlock{ s = "GTAI", noti_time = 20241118 , release_time = 20241125, cap = 24.92, value = 1.98 },
            new TokenUnlock{ s = "ALT", noti_time = 20241118 , release_time = 20241125, cap = 254.39, value = 21.68 },
            new TokenUnlock{ s = "RAD", noti_time = 20241118 , release_time = 20241125, cap = 64.79, value = 1.95 },
            new TokenUnlock{ s = "AURY", noti_time = 20241119 , release_time = 20241126, cap = 14.72, value = 0.31 },
            new TokenUnlock{ s = "BREED", noti_time = 20241119 , release_time = 20241126, cap = 12.68, value = 0.22 },
            new TokenUnlock{ s = "NFP", noti_time = 20241120 , release_time = 20241127, cap = 70, value = 2.49 },
            new TokenUnlock{ s = "AXL", noti_time = 20241120 , release_time = 20241127, cap = 623, value = 18.95 },
            new TokenUnlock{ s = "GOAL", noti_time = 20241120 , release_time = 20241127, cap = 12.57, value = 0.5 },
            new TokenUnlock{ s = "EDU", noti_time = 20241121 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "IMX", noti_time = 20241122 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "PORTAL", noti_time = 20241122 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "UDS", noti_time = 20241123 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "JUV", noti_time = 20241124 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "BAR", noti_time = 20241124 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "CFX", noti_time = 20241124 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "HOOK", noti_time = 20241124 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "BICO", noti_time = 20241124 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ACM", noti_time = 20241124 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SKL", noti_time = 20241124 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SANTOS", noti_time = 20241124 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ETHDYDX", noti_time = 20241124 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "1INCH", noti_time = 20241124 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "OGN", noti_time = 20241124 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ORNJ", noti_time = 20241125 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ORBR", noti_time = 20241125 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "TAI", noti_time = 20241125 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "NTRN", noti_time = 20241126 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "DAR", noti_time = 20241127 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "AI", noti_time = 20241127 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "G", noti_time = 20241128 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "THL", noti_time = 20241129 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "NEON", noti_time = 20241130 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "WIFI", noti_time = 20241130 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "BOSON", noti_time = 20241130 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "MYRIA", noti_time = 20241130 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "DEVVE", noti_time = 20241130 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ACH", noti_time = 20241130 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "JTO", noti_time = 20241130 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "HFT", noti_time = 20241130 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "THL", noti_time = 20241201 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "NEON", noti_time = 20241201 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "WIFI", noti_time = 20241201 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "BOSON", noti_time = 20241201 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "MYRIA", noti_time = 20241201 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "DEVVE", noti_time = 20241201 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ACH", noti_time = 20241201 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SLF", noti_time = 20241201 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SD", noti_time = 20241201 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "CLY", noti_time = 20241201 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ENS", noti_time = 20241201 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SCA", noti_time = 20241201 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "GMT", noti_time = 20241202 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "G3", noti_time = 20241202 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "CGPT", noti_time = 20241203 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "PDA", noti_time = 20241203 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "CHEEL", noti_time = 20241203 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "TADA", noti_time = 20241203 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "GODS", noti_time = 20241204 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "GFI", noti_time = 20241204 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "DUEL", noti_time = 20241205 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "APT", noti_time = 20241205 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "DIMO", noti_time = 20241205 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "OAS", noti_time = 20241205 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "NIBI", noti_time = 20241205 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SWEAT", noti_time = 20241206 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "GFAL", noti_time = 20241207 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "VOXEL", noti_time = 20241207 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ALI", noti_time = 20241207 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "RSS3", noti_time = 20241207 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "PUFFER", noti_time = 20241207 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "HIGH", noti_time = 20241207 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SEI", noti_time = 20241208 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ZTX", noti_time = 20241209 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ZBCN", noti_time = 20241209 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "RARE", noti_time = 20241210 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "APE", noti_time = 20241210 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ACE", noti_time = 20241211 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "TORN", noti_time = 20241211 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SABAI", noti_time = 20241211 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "CTRL", noti_time = 20241212 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ZKJ", noti_time = 20241212 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "KARRAT", noti_time = 20241212 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "NUM", noti_time = 20241212 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "PIXEL", noti_time = 20241212 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "LVN", noti_time = 20241212 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "MERL", noti_time = 20241212 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "HTM", noti_time = 20241213 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "BCUT", noti_time = 20241213 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "VARA", noti_time = 20241214 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "TAO", noti_time = 20241214 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "VRTX", noti_time = 20241214 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "XPLA", noti_time = 20241214 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ID", noti_time = 20241215 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "C98", noti_time = 20241216 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "GTAI", noti_time = 20241218 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ALT", noti_time = 20241218 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "RAD", noti_time = 20241218 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "AURY", noti_time = 20241219 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SOVRN", noti_time = 20241219 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "NFP", noti_time = 20241220 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "IMX", noti_time = 20241220 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "AXL", noti_time = 20241220 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "GOAL", noti_time = 20241220 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "EDU", noti_time = 20241221 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "PORTAL", noti_time = 20241222 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "BETA", noti_time = 20241223 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "GEOD", noti_time = 20241224 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "JUV", noti_time = 20241225 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "BAR", noti_time = 20241225 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ATM", noti_time = 20241225 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ASR", noti_time = 20241225 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "CFX", noti_time = 20241225 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "HOOK", noti_time = 20241225 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ACM", noti_time = 20241225 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ETHDYDX", noti_time = 20241225 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "COTI", noti_time = 20241225 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "OGN", noti_time = 20241225 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ORBR", noti_time = 20241226 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "TAI", noti_time = 20241226 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "NTRN", noti_time = 20241227 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "DAR", noti_time = 20241228 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "AI", noti_time = 20241228 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "G", noti_time = 20241229 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "LKI", noti_time = 20241230 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "THL", noti_time = 20241230 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "NEON", noti_time = 20241231 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "WIFI", noti_time = 20241231 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "BOSON", noti_time = 20241231 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "MYRIA", noti_time = 20241231 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ACH", noti_time = 20241231 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "JTO", noti_time = 20241231 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "HFT", noti_time = 20241231 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "NAVX", noti_time = 20240101 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SLF", noti_time = 20240101 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SD", noti_time = 20240101 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ENS", noti_time = 20240101 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SCA", noti_time = 20240101 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "GMT", noti_time = 20240102 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "G3", noti_time = 20240102 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ALPHA", noti_time = 20240103 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "CGPT", noti_time = 20240103 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "CHEEL", noti_time = 20240103 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "TADA", noti_time = 20240104 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "GODS", noti_time = 20240104 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "GFI", noti_time = 20240104 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "APT", noti_time = 20240105 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "DIMO", noti_time = 20240105 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "OAS", noti_time = 20240105 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SWEAT", noti_time = 20240106 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "DUEL", noti_time = 20240107 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "GFAL", noti_time = 20240107 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "VOXEL", noti_time = 20240107 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ALI", noti_time = 20240107 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "RSS3", noti_time = 20240107 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "PUFFER", noti_time = 20240107 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SEI", noti_time = 20240108 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "BURGER", noti_time = 20240108 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ZTX", noti_time = 20240109 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ZBCN", noti_time = 20240109 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "RARE", noti_time = 20240110 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "APE", noti_time = 20240110 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ACE", noti_time = 20240111 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "MANTA", noti_time = 20240111 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "GENE", noti_time = 20240111 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ONDO", noti_time = 20240112 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ZKJ", noti_time = 20240112 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "KARRAT", noti_time = 20240112 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "NUM", noti_time = 20240112 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "PIXEL", noti_time = 20240112 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "MERL", noti_time = 20240112 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "HTM", noti_time = 20240113 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "BCUT", noti_time = 20240113 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "VARA", noti_time = 20240114 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "TAO", noti_time = 20240114 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "VRTX", noti_time = 20240114 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "C98", noti_time = 20240116 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "IMX", noti_time = 20240117 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "GTAI", noti_time = 20240118 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ALT", noti_time = 20240118 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "RAD", noti_time = 20240118 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "AURY", noti_time = 20240119 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SOVRN", noti_time = 20240119 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "NFP", noti_time = 20240120 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "AXL", noti_time = 20240120 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "EDU", noti_time = 20240121 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "MAV", noti_time = 20240121 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "PORTAL", noti_time = 20240122 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "BTT", noti_time = 20240124 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "WLTH", noti_time = 20240125 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "JUV", noti_time = 20240125 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "BAR", noti_time = 20240125 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ATM", noti_time = 20240125 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ASR", noti_time = 20240125 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "CFX", noti_time = 20240125 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "HOOK", noti_time = 20240125 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "AVA", noti_time = 20240125 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ETHDYDX", noti_time = 20240125 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "OGN", noti_time = 20240125 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ORBR", noti_time = 20240126 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "NTRN", noti_time = 20240127 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "D", noti_time = 20240128 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "AI", noti_time = 20240128 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "G", noti_time = 20240129 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "MAVIA", noti_time = 20240130 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "THL", noti_time = 20240130 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "NEON", noti_time = 20240131 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "WIFI", noti_time = 20240131 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "BOSON", noti_time = 20240131 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "MYRIA", noti_time = 20240131 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ACH", noti_time = 20240131 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "TKO", noti_time = 20240131 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "JTO", noti_time = 20240131 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "HFT", noti_time = 20240131 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ATA", noti_time = 20240131 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SLF", noti_time = 20240201 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SD", noti_time = 20240201 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ENS", noti_time = 20240201 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SCA", noti_time = 20240201 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "GMT", noti_time = 20240202 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "CGPT", noti_time = 20240203 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "CHEEL", noti_time = 20240203 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "GODS", noti_time = 20240203 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "GFI", noti_time = 20240203 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "BOBA", noti_time = 20240205 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "DIMO", noti_time = 20240205 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "OAS", noti_time = 20240205 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "NIBI", noti_time = 20240205 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SWEAT", noti_time = 20240205 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "GFAL", noti_time = 20240207 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SAND", noti_time = 20240207 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "VOXEL", noti_time = 20240207 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ALI", noti_time = 20240207 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "RSS3", noti_time = 20240207 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "PUFFER", noti_time = 20240207 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "CYBER", noti_time = 20240207 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "MLN", noti_time = 20240207 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "SEI", noti_time = 20240207 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ALPINE", noti_time = 20240207 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ZTX", noti_time = 20240209 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "ZBCN", noti_time = 20240209 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "RARE", noti_time = 20240210 , release_time = , cap = , value =  },
            new TokenUnlock{ s = "APE", noti_time = 20240210 , release_time = , cap = , value =  },
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
