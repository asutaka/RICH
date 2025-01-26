using Binance.Net.Objects.Models.Futures.Socket;
using CoinPr.DAL;
using CoinPr.Model;
using CoinPr.Utils;
using MongoDB.Driver;
using Skender.Stock.Indicators;

namespace CoinPr.Service
{
    public interface IWebSocketService
    {
        void BinanceLiquid();
    }
    public class WebSocketService : IWebSocketService
    {
        private readonly long _idChannel = -1002424403434;
        private const long _idUser = 1066022551;
        private readonly ILogger<WebSocketService> _logger;
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private readonly ITradingRepo _tradingRepo;
        private const int MIN_VALUE = 10000;
        private static Dictionary<string, DateTime> _dicRes = new Dictionary<string, DateTime>();
        
        public WebSocketService(ILogger<WebSocketService> logger, IAPIService apiService, ITeleService teleService, ITradingRepo tradingRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _teleService = teleService;
            _tradingRepo = tradingRepo;
        }

        public void BinanceLiquid()
        {
            try
            {
                var liquid = StaticVal.BinanceSocketInstance().UsdFuturesApi.ExchangeData.SubscribeToAllLiquidationUpdatesAsync((data) =>
                {
                    var val = Math.Round(data.Data.AveragePrice * data.Data.Quantity);
                    if (val < MIN_VALUE
                        || !StaticVal._lCoinAnk.Contains(data.Data.Symbol))
                        return;

                    //Console.WriteLine(JsonConvert.SerializeObject(data.Data));
                    var dt = DateTime.Now;
                    var first = _dicRes.FirstOrDefault(x => x.Key == data.Data.Symbol);
                    if (first.Key != null)
                    {
                        var div = (dt - first.Value).TotalSeconds;
                        if (div >= 120)
                        {
                            _dicRes[data.Data.Symbol] = dt;
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        _dicRes.Add(data.Data.Symbol, dt);
                    }

                    var mes = HandleMessage(data.Data).GetAwaiter().GetResult();
                    if (string.IsNullOrWhiteSpace(mes))
                        return;

                    _teleService.SendMessage(_idUser, mes).GetAwaiter().GetResult();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WebSocketService.BinanceLiquid|EXCEPTION| {ex.Message}");
            }
        }

        private async Task<string> HandleMessage(BinanceFuturesStreamLiquidation msg)
        {
            string mes = string.Empty;
            try
            {
                var liquid = await CheckLiquid(msg);
                if (liquid is null)
                    return mes;
                var ext = string.Empty;
                if (liquid.Status == (int)LiquidStatus.Ready)
                    ext = "(INVERT)";

                if(liquid.Entry >= 1)
                {
                    liquid.Entry = Math.Round(liquid.Entry, 2);
                    liquid.TP = Math.Round(liquid.TP, 2);
                    liquid.SL = Math.Round(liquid.SL, 2);
                }
                else
                {
                    liquid.Entry = Math.Round(liquid.Entry, 5);
                    liquid.TP = Math.Round(liquid.TP, 5);
                    liquid.SL = Math.Round(liquid.SL, 5);
                }
                var side = (liquid.Mode == (int)ELiquidMode.MuaCungChieu || liquid.Mode == (int)ELiquidMode.MuaNguocChieu) ? 1 : 2;
                var sideText =  side == 1 ? "Long" : "Short";

                mes = $"|LIQUID|{liquid.Date.ToString("dd/MM/yyyy HH:mm")}|{liquid.s}|{sideText}{ext}|ENTRY: {liquid.Entry}|TP: {liquid.TP}|SL: {liquid.SL}\n" +
                    $"PriceAt: {liquid.PriceAtLiquid}| Avg: {liquid.AvgPrice}| Cur: {liquid.CurrentPrice}|Mode:{((ELiquidMode)liquid.Mode).ToString()}";
                _tradingRepo.InsertOne(new DAL.Entity.Trading
                {
                    key = Guid.NewGuid().ToString(),
                    s = liquid.s,
                    d = (int)new DateTimeOffset(liquid.Date).ToUnixTimeSeconds(),
                    Side = side,
                    Entry = (double)liquid.Entry,
                    TP = (double)liquid.TP,
                    SL = (double)liquid.SL,
                    Focus = (double)liquid.Focus,
                    AvgPrice = (double)liquid.AvgPrice,
                    PriceAtLiquid = (double)liquid.PriceAtLiquid,
                    CurrentPrice = (double)liquid.CurrentPrice,
                    Mode = liquid.Mode,
                    Date = liquid.Date,
                    Rsi_5 = (double)liquid.Rsi_5,
                    Top_1 = (double)liquid.Top_1,
                    Top_2 = (double)liquid.Top_2,
                    Bot_1 = (double)liquid.Bot_1,
                    Bot_2 = (double)liquid.Bot_2,
                    Case = (int)ECase.Case1
                });
                _tradingRepo.InsertOne(new DAL.Entity.Trading
                {
                    key = Guid.NewGuid().ToString(),
                    s = liquid.s,
                    d = (int)new DateTimeOffset(liquid.Date).ToUnixTimeSeconds(),
                    Side = side,
                    Entry = (double)liquid.Entry,
                    TP = (double)liquid.TP_2,
                    SL = (double)liquid.SL,
                    Focus = (double)liquid.Focus,
                    AvgPrice = (double)liquid.AvgPrice,
                    PriceAtLiquid = (double)liquid.PriceAtLiquid,
                    CurrentPrice = (double)liquid.CurrentPrice,
                    Mode = liquid.Mode,
                    Date = liquid.Date,
                    Rsi_5 = (double)liquid.Rsi_5,
                    Top_1 = (double)liquid.Top_1,
                    Top_2 = (double)liquid.Top_2,
                    Bot_1 = (double)liquid.Bot_1,
                    Bot_2 = (double)liquid.Bot_2,
                    Case = (int)ECase.Case2
                });
                _tradingRepo.InsertOne(new DAL.Entity.Trading
                {
                    key = Guid.NewGuid().ToString(),
                    s = liquid.s,
                    d = (int)new DateTimeOffset(liquid.Date).ToUnixTimeSeconds(),
                    Side = side,
                    Entry = (double)liquid.Entry,
                    TP = (double)liquid.TP_3,
                    SL = (double)liquid.SL,
                    Focus = (double)liquid.Focus,
                    AvgPrice = (double)liquid.AvgPrice,
                    PriceAtLiquid = (double)liquid.PriceAtLiquid,
                    CurrentPrice = (double)liquid.CurrentPrice,
                    Mode = liquid.Mode,
                    Date = liquid.Date,
                    Rsi_5 = (double)liquid.Rsi_5,
                    Top_1 = (double)liquid.Top_1,
                    Top_2 = (double)liquid.Top_2,
                    Bot_1 = (double)liquid.Bot_1,
                    Bot_2 = (double)liquid.Bot_2,
                    Case = (int)ECase.Case3
                });
                _tradingRepo.InsertOne(new DAL.Entity.Trading
                {
                    key = Guid.NewGuid().ToString(),
                    s = liquid.s,
                    d = (int)new DateTimeOffset(liquid.Date).ToUnixTimeSeconds(),
                    Side = side,
                    Entry = (double)liquid.Entry,
                    TP = (double)liquid.TP,
                    SL = (double)liquid.SL_2,
                    Focus = (double)liquid.Focus,
                    AvgPrice = (double)liquid.AvgPrice,
                    PriceAtLiquid = (double)liquid.PriceAtLiquid,
                    CurrentPrice = (double)liquid.CurrentPrice,
                    Mode = liquid.Mode,
                    Date = liquid.Date,
                    Rsi_5 = (double)liquid.Rsi_5,
                    Top_1 = (double)liquid.Top_1,
                    Top_2 = (double)liquid.Top_2,
                    Bot_1 = (double)liquid.Bot_1,
                    Bot_2 = (double)liquid.Bot_2,
                    Case = (int)ECase.Case4
                });
                _tradingRepo.InsertOne(new DAL.Entity.Trading
                {
                    key = Guid.NewGuid().ToString(),
                    s = liquid.s,
                    d = (int)new DateTimeOffset(liquid.Date).ToUnixTimeSeconds(),
                    Side = side,
                    Entry = (double)liquid.Entry,
                    TP = (double)liquid.TP_2,
                    SL = (double)liquid.SL_2,
                    Focus = (double)liquid.Focus,
                    AvgPrice = (double)liquid.AvgPrice,
                    PriceAtLiquid = (double)liquid.PriceAtLiquid,
                    CurrentPrice = (double)liquid.CurrentPrice,
                    Mode = liquid.Mode,
                    Date = liquid.Date,
                    Rsi_5 = (double)liquid.Rsi_5,
                    Top_1 = (double)liquid.Top_1,
                    Top_2 = (double)liquid.Top_2,
                    Bot_1 = (double)liquid.Bot_1,
                    Bot_2 = (double)liquid.Bot_2,
                    Case = (int)ECase.Case5
                });
                _tradingRepo.InsertOne(new DAL.Entity.Trading
                {
                    key = Guid.NewGuid().ToString(),
                    s = liquid.s,
                    d = (int)new DateTimeOffset(liquid.Date).ToUnixTimeSeconds(),
                    Side = side,
                    Entry = (double)liquid.Entry,
                    TP = (double)liquid.TP_3,
                    SL = (double)liquid.SL_2,
                    Focus = (double)liquid.Focus,
                    AvgPrice = (double)liquid.AvgPrice,
                    PriceAtLiquid = (double)liquid.PriceAtLiquid,
                    CurrentPrice = (double)liquid.CurrentPrice,
                    Mode = liquid.Mode,
                    Date = liquid.Date,
                    Rsi_5 = (double)liquid.Rsi_5,
                    Top_1 = (double)liquid.Top_1,
                    Top_2 = (double)liquid.Top_2,
                    Bot_1 = (double)liquid.Bot_1,
                    Bot_2 = (double)liquid.Bot_2,
                    Case = (int)ECase.Case6
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WebSocketService.HandleMessage|EXCEPTION| {ex.Message}");
            }
            return mes;
        }

        private TradingResponse LiquidBuy_Inverse(BinanceFuturesStreamLiquidation msg, IEnumerable<List<decimal>> lLiquid, int flag, decimal avgPrice, CoinAnk_LiquidValue dat, double rsi5, decimal top1, decimal top2, decimal bot1, decimal bot2)
        {
            try
            {
                var lMaxliquid = lLiquid.Where(x => x.ElementAt(1) > flag).Where(x => x.ElementAt(2) >= (decimal)0.88 * dat.data.liqHeatMap.maxLiqValue);
                if (lMaxliquid == null || !lMaxliquid.Any())
                    return null;
                var index = lMaxliquid.MaxBy(x => x.ElementAt(1));
                var priceAtMaxLiquid = dat.data.liqHeatMap.priceArray[(int)index.ElementAt(1)];

                //Giá hiện tại nhỏ hơn giá tại điểm thanh lý
                if (msg.AveragePrice > priceAtMaxLiquid && rsi5 > 70)
                {
                    var liquid = new TradingResponse
                    {
                        s = msg.Symbol,
                        Date = DateTime.Now,
                        Type = (int)TradingResponseType.Liquid,
                        Side = Binance.Net.Enums.OrderSide.Sell,
                        Focus = msg.AveragePrice,
                        Entry = msg.AveragePrice,
                        TP = priceAtMaxLiquid - Math.Abs((msg.AveragePrice - avgPrice) / 3),
                        SL = msg.AveragePrice + Math.Abs((msg.AveragePrice - avgPrice) / 3),
                        Status = (int)LiquidStatus.Ready
                    };
                    liquid.PriceAtLiquid = priceAtMaxLiquid;
                    liquid.Mode = (int)ELiquidMode.BanNguocChieu;
                    liquid.Rsi_5 = (decimal)rsi5;
                    liquid.Top_1 = top1;
                    liquid.Top_2 = top2;
                    liquid.Bot_1 = bot1;
                    liquid.Bot_2 = bot2;

                    liquid.TP_2 = priceAtMaxLiquid - Math.Abs(priceAtMaxLiquid - avgPrice) / 3;
                    liquid.TP_3 = priceAtMaxLiquid - Math.Abs(priceAtMaxLiquid - avgPrice) / 2;
                    liquid.SL_2 = msg.AveragePrice + Math.Abs((priceAtMaxLiquid - avgPrice) / 3);
                    return liquid;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WebSocketService.LiquidBuy_Inverse|EXCEPTION| {ex.Message}");
            }

            return null;
        }

        private TradingResponse LiquidSell_Inverse(BinanceFuturesStreamLiquidation msg, IEnumerable<List<decimal>> lLiquid, int flag, decimal avgPrice, CoinAnk_LiquidValue dat, double rsi5, decimal top1, decimal top2, decimal bot1, decimal bot2)
        {
            try
            {
                var lMaxliquid = lLiquid.Where(x => x.ElementAt(1) < flag).Where(x => x.ElementAt(2) >= (decimal)0.88 * dat.data.liqHeatMap.maxLiqValue);
                if (lMaxliquid == null || !lMaxliquid.Any())
                    return null;
                var index = lMaxliquid.MinBy(x => x.ElementAt(1));
                var priceAtMaxLiquid = dat.data.liqHeatMap.priceArray[(int)index.ElementAt(1)];

                //Giá hiện tại nhỏ hơn giá tại điểm thanh lý
                if (msg.AveragePrice < priceAtMaxLiquid && rsi5 < 30)
                {
                    var liquid = new TradingResponse
                    {
                        s = msg.Symbol,
                        Date = DateTime.Now,
                        Type = (int)TradingResponseType.Liquid,
                        Side = Binance.Net.Enums.OrderSide.Buy,
                        Focus = msg.AveragePrice,
                        Entry = msg.AveragePrice,
                        TP = priceAtMaxLiquid + Math.Abs((msg.AveragePrice - avgPrice) / 3),
                        SL = msg.AveragePrice - Math.Abs((msg.AveragePrice - avgPrice) / 3),
                        Status = (int)LiquidStatus.Ready
                    };
                    liquid.Mode = (int)ELiquidMode.MuaNguocChieu;
                    liquid.PriceAtLiquid = priceAtMaxLiquid;
                    liquid.Rsi_5 = (decimal)rsi5;
                    liquid.Top_1 = top1;
                    liquid.Top_2 = top2;
                    liquid.Bot_1 = bot1;
                    liquid.Bot_2 = bot2;

                    liquid.TP_2 = priceAtMaxLiquid + Math.Abs(priceAtMaxLiquid - avgPrice) / 3;
                    liquid.TP_3 = priceAtMaxLiquid + Math.Abs(priceAtMaxLiquid - avgPrice) / 2;
                    liquid.SL_2 = msg.AveragePrice - Math.Abs((priceAtMaxLiquid - avgPrice) / 3);
                    return liquid;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WebSocketService.LiquidSell_Inverse|EXCEPTION| {ex.Message}");
            }

            return null;
        }

        private async Task<TradingResponse> CheckLiquid(BinanceFuturesStreamLiquidation msg)
        {
            try
            {
                var dat = await _apiService.CoinAnk_GetLiquidValue(msg.Symbol);
                if (dat?.data?.liqHeatMap is null)
                    return null;

                var flag = -1;
                var count = dat.data.liqHeatMap.priceArray.Count();
                var avgPrice = (decimal)0.5 * (dat.data.liqHeatMap.priceArray[0] + dat.data.liqHeatMap.priceArray[count - 1]);
                for (var i = 0; i < count; i++)
                {
                    var element = dat.data.liqHeatMap.priceArray[i];
                    if (element >= avgPrice)
                    {
                        flag = i; break;
                    }
                }
               

                if (flag <= 0)
                    return null;
                var lData5M = await _apiService.GetData(msg.Symbol, EExchange.Binance, EInterval.M5);
                var lRsi5M = lData5M.GetRsi();
                var lTopBot5M = lData5M.GetTopBottom_H(0);
                var lTop = lTopBot5M.Where(x => x.IsTop);
                var lBot = lTopBot5M.Where(x => x.IsBot);
                var top1 = lTop?.LastOrDefault()?.Value ?? 0;
                var top2 = lTop?.SkipLast(1).LastOrDefault()?.Value ?? 0;
                var bot1 = lBot?.LastOrDefault()?.Value ?? 0;
                var bot2 = lBot?.SkipLast(1).LastOrDefault()?.Value ?? 0;
                Thread.Sleep(200);

                var lLiquid = dat.data.liqHeatMap.data.Where(x => x.ElementAt(0) >= 280 && x.ElementAt(0) <= 286);
                var lLiquidLast = dat.data.liqHeatMap.data.Where(x => x.ElementAt(0) == 288);
                if (msg.AveragePrice >= avgPrice)
                {
                    var res = LiquidBuy_Inverse(msg, lLiquid, flag, avgPrice, dat, lRsi5M.LastOrDefault()?.Rsi ?? 0, top1, top2, bot1, bot2);

                    if(res != null)
                    {
                        res.CurrentPrice = msg.AveragePrice;
                        res.AvgPrice = avgPrice;
                    }
                   
                    return res;
                }
                else
                {
                    var res = LiquidSell_Inverse(msg, lLiquid, flag, avgPrice, dat, lRsi5M.LastOrDefault()?.Rsi ?? 0, top1, top2, bot1, bot2);
                    
                    if (res != null)
                    {
                        res.CurrentPrice = msg.AveragePrice;
                        res.AvgPrice = avgPrice;
                    }

                    return res;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WebSocketService.CheckLiquid|EXCEPTION| {ex.Message}");
            }

            return null;
        }
    }
}