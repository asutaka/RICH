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

                var dot = 5;
                if (liquid.Entry < (decimal)0.001)
                {
                    dot = 8;
                }
                else if (liquid.Entry < (decimal)0.01)
                {
                    dot = 7;
                }
                else if (liquid.Entry < (decimal)0.1)
                {
                    dot = 6;
                }
                
                liquid.Entry = Math.Round(liquid.Entry, dot);
                liquid.TP = Math.Round(liquid.TP, dot);
                liquid.TP_2 = Math.Round(liquid.TP_2, dot);
                liquid.TP_3 = Math.Round(liquid.TP_3, dot);
                liquid.TP25 = Math.Round(liquid.TP25, dot);
                liquid.SL = Math.Round(liquid.SL, dot);
                liquid.SL_2 = Math.Round(liquid.SL_2, dot);
                liquid.SL25 = Math.Round(liquid.SL25, dot);
                liquid.Rsi = Math.Round(liquid.Rsi, dot);

                var sideText =  liquid.Side == Binance.Net.Enums.OrderSide.Buy ? "Long" : "Short";

                mes = $"{liquid.Date.ToString("dd/MM/yyyy HH:mm")}|{liquid.s}|{sideText}|ENTRY: {liquid.Entry}|TP: {liquid.TP25}|SL: {liquid.SL25}\n" +
                    $"Cur: {liquid.CurrentPrice}|Avg: {liquid.AvgPrice}|Liquid: {liquid.Liquid}|Rsi: {liquid.Rsi}";
                _tradingRepo.InsertOne(new DAL.Entity.Trading
                {
                    key = Guid.NewGuid().ToString(),
                    s = liquid.s,
                    d = (int)new DateTimeOffset(liquid.Date).ToUnixTimeSeconds(),
                    Side = (int)liquid.Side,
                    Entry = (double)liquid.Entry,
                    TP = (double)liquid.TP,
                    SL = (double)liquid.SL,
                    Focus = (double)liquid.Focus,
                    AvgPrice = (double)liquid.AvgPrice,
                    Liquid = (double)liquid.Liquid,
                    CurrentPrice = (double)liquid.CurrentPrice,
                    Date = liquid.Date,
                    Rsi = (double)liquid.Rsi,
                    TopFirst = (double)liquid.TopFirst,
                    TopNext = (double)liquid.TopNext,
                    BotFirst = (double)liquid.BotFirst,
                    BotNext = (double)liquid.BotNext,
                    RateVol = (double)liquid.RateVol,
                    TP_2 = (double)liquid.TP_2,
                    TP_3 = (double)liquid.TP_3,
                    TP25 = (double)liquid.TP25,
                    SL_2 = (double)liquid.SL_2,
                    SL25 = (double)liquid.SL25,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WebSocketService.HandleMessage|EXCEPTION| {ex.Message}");
            }
            return mes;
        }

        private async Task<TradingResponse> CheckLiquid(BinanceFuturesStreamLiquidation msg)
        {
            try
            {
                var lData5M = await _apiService.GetData(msg.Symbol, EExchange.Binance, EInterval.M5);
                Thread.Sleep(200);
                var lRsi5M = lData5M.GetRsi();
                var rsi = (decimal)lRsi5M.Last().Rsi;
                if (rsi > 30 && rsi < 70)
                    return null;

                var lTopBot5M = lData5M.GetTopBottom_H(0);
                var lTop = lTopBot5M.Where(x => x.IsTop);
                var lBot = lTopBot5M.Where(x => x.IsBot);
                var topFirst = lTop?.LastOrDefault()?.Value ?? 0;
                var topNext = lTop?.SkipLast(1).LastOrDefault()?.Value ?? 0;
                var botFirst = lBot?.LastOrDefault()?.Value ?? 0;
                var botNext = lBot?.SkipLast(1).LastOrDefault()?.Value ?? 0;

                var avgVol = lData5M.SkipLast(2).TakeLast(5).Select(x => x.Volume).Average();
                var rateVol = Math.Round(lData5M.SkipLast(1).Last().Volume / avgVol, 2);

                var dat = await _apiService.CoinAnk_GetLiquidValue(msg.Symbol);
                if (dat?.data?.liqHeatMap is null)
                    return null;

                var cur = msg.AveragePrice;
                //Giá trung bình của liquid
                var avgPrice = (decimal)0.5 * (dat.data.liqHeatMap.priceArray[0] + dat.data.liqHeatMap.priceArray[dat.data.liqHeatMap.priceArray.Count() - 1]);
                var flag = dat.data.liqHeatMap.priceArray.FindIndex(x => x > avgPrice);
                if (flag <= 0)
                    return null;

                var lLiquid = dat.data.liqHeatMap.data.Where(x => x.ElementAt(0) >= 280 && x.ElementAt(0) <= 287);
                if (cur >= avgPrice)
                {
                    var lMaxliquid = lLiquid.Where(x => x.ElementAt(1) > flag).Where(x => x.ElementAt(2) >= (decimal)0.88 * dat.data.liqHeatMap.maxLiqValue);
                    if (lMaxliquid == null || !lMaxliquid.Any())
                        return null;

                    var priceAtMaxLiquid = dat.data.liqHeatMap.priceArray[(int)lMaxliquid.MaxBy(x => x.ElementAt(1)).ElementAt(1)];
                    if (cur < priceAtMaxLiquid)
                        return null;

                    if(rsi >= 80 ||
                       (rsi >=  68 && rateVol < 1))
                    {
                        var liquid = new TradingResponse
                        {
                            s = msg.Symbol,
                            Date = DateTime.Now,
                            Side = Binance.Net.Enums.OrderSide.Sell,
                            Focus = cur,
                            Entry = cur,
                            TP = priceAtMaxLiquid - Math.Abs((cur - avgPrice) / 3),
                            SL = msg.AveragePrice + Math.Abs((cur - avgPrice) / 3),
                            //for test
                            CurrentPrice = cur,
                            AvgPrice = avgPrice,
                            Liquid = priceAtMaxLiquid,
                            Rsi = rsi,
                            TopFirst = topFirst,
                            TopNext = topNext,
                            BotFirst = botFirst,
                            BotNext = botNext,
                            RateVol = rateVol,
                            //tp
                            TP_2 = priceAtMaxLiquid - Math.Abs(priceAtMaxLiquid - avgPrice) / 3,
                            TP_3 = priceAtMaxLiquid - Math.Abs(priceAtMaxLiquid - avgPrice) / 2,
                            TP25 = cur * (1 - ((decimal)25 / (100 * StaticVal._dicMargin.First(x => x.Key == msg.Symbol).Value))),
                            SL25 = cur * (1 + ((decimal)25 / (100 * StaticVal._dicMargin.First(x => x.Key == msg.Symbol).Value))),
                            SL_2 = msg.AveragePrice + Math.Abs((priceAtMaxLiquid - avgPrice) / 3)
                        };
                        return liquid;
                    }

                    return null;
                }
                else
                {
                    var lMaxliquid = lLiquid.Where(x => x.ElementAt(1) < flag).Where(x => x.ElementAt(2) >= (decimal)0.88 * dat.data.liqHeatMap.maxLiqValue);
                    if (lMaxliquid == null || !lMaxliquid.Any())
                        return null;

                    var priceAtMaxLiquid = dat.data.liqHeatMap.priceArray[(int)lMaxliquid.MinBy(x => x.ElementAt(1)).ElementAt(1)];
                    if (cur > priceAtMaxLiquid)
                        return null;

                    if (rsi <= 20 ||
                       (rsi <= 32 && rateVol < 1))
                    {
                        var liquid = new TradingResponse
                        {
                            s = msg.Symbol,
                            Date = DateTime.Now,
                            Side = Binance.Net.Enums.OrderSide.Buy,
                            Focus = cur,
                            Entry = cur,
                            TP = priceAtMaxLiquid + Math.Abs((cur - avgPrice) / 3),
                            SL = msg.AveragePrice - Math.Abs((cur - avgPrice) / 3),
                            //for test
                            CurrentPrice = cur,
                            AvgPrice = avgPrice,
                            Liquid = priceAtMaxLiquid,
                            Rsi = rsi,
                            TopFirst = topFirst,
                            TopNext = topNext,
                            BotFirst = botFirst,
                            BotNext = botNext,
                            RateVol = rateVol,
                            //tp
                            TP_2 = priceAtMaxLiquid + Math.Abs(priceAtMaxLiquid - avgPrice) / 3,
                            TP_3 = priceAtMaxLiquid + Math.Abs(priceAtMaxLiquid - avgPrice) / 2,
                            TP25 = cur * (1 + (25 / (100 * StaticVal._dicMargin.First(x => x.Key == msg.Symbol).Value))),
                            SL25 = cur * (1 - (25 / (100 * StaticVal._dicMargin.First(x => x.Key == msg.Symbol).Value))),
                            SL_2 = msg.AveragePrice - Math.Abs((priceAtMaxLiquid - avgPrice) / 3)
                        };
                        return liquid;
                    }
                    return null;
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