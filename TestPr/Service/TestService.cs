using Skender.Stock.Indicators;
using System.Xml.Linq;
using TestPr.Model;
using TestPr.Utils;

namespace TestPr.Service
{
    public interface ITestService
    {
        Task MethodTest();
    }
    public class TestService : ITestService
    {
        private readonly ILogger<TestService> _logger;
        private readonly IAPIService _apiService;
        public TestService(ILogger<TestService> logger, IAPIService apiService)
        {
            _logger = logger;
            _apiService = apiService;
        }

        public async Task MethodTest()
        {
            try
            {
                var lBinance = await StaticVal.BinanceInstance().SpotApi.ExchangeData.GetKlinesAsync("ETHUSDT", Binance.Net.Enums.KlineInterval.OneDay, limit: 1000);
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
                _logger.LogError(ex, $"BinanceService.GetAccountInfo|EXCEPTION| {ex.Message}");
            }
        }
    }
}
