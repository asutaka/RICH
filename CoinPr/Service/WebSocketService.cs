using Binance.Net.Objects.Models.Futures.Socket;
using CoinPr.DAL;
using CoinPr.Model;
using CoinPr.Utils;
using MongoDB.Driver;

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
        private const int MIN_VALUE = 15000;
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
                    var val = Math.Round(data.Data.Price * data.Data.Quantity);
                    if(val >= MIN_VALUE && StaticVal._lCoinAnk.Contains(data.Data.Symbol))
                    {
                        var dt = DateTime.Now;
                        var first = _dicRes.FirstOrDefault(x => x.Key == data.Data.Symbol);
                        if(first.Key != null)
                        {
                            var div = (dt - first.Value).TotalSeconds;
                            if(div >= 30)
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
                        if (!string.IsNullOrWhiteSpace(mes))
                        {
                            _teleService.SendMessage(_idChannel, mes).GetAwaiter().GetResult();
                            //Console.WriteLine(mes);
                        }
                    }
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
                var side = ((liquid.Side == Binance.Net.Enums.OrderSide.Buy && !string.IsNullOrWhiteSpace(ext))
                            || (liquid.Side == Binance.Net.Enums.OrderSide.Sell && string.IsNullOrWhiteSpace(ext))) ? 1 : 2;
                var sideText = side == 1 ? "Long" : "Short";

                mes = $"|LIQUID|{liquid.Date.ToString("dd/MM/yyyy HH:mm")}|{liquid.s}|{sideText}{ext}|ENTRY: {liquid.Entry}|TP: {liquid.TP}|SL: {liquid.SL}";
                _tradingRepo.InsertOne(new DAL.Entity.Trading
                {
                    s = liquid.s,
                    d = (int)new DateTimeOffset(liquid.Date).ToUnixTimeSeconds(),
                    Side = side,
                    Entry = (double)liquid.Entry,
                    TP = (double)liquid.TP,
                    SL = (double)liquid.SL,
                    Focus = (double)liquid.Focus
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WebSocketService.HandleMessage|EXCEPTION| {ex.Message}");
            }
            return mes;
        }

        private TradingResponse LiquidBuy(BinanceFuturesStreamLiquidation msg, IEnumerable<List<decimal>> lLiquid, int flag, decimal avgPrice, CoinAnk_LiquidValue dat)
        {
            try
            {
                decimal priceAtMaxLiquid = 0;
                var maxLiquid = lLiquid.Where(x => x.ElementAt(1) < flag - 1).MaxBy(x => x.ElementAt(2));
                if (maxLiquid.ElementAt(2) >= (decimal)0.85 * dat.data.liqHeatMap.maxLiqValue)
                {
                    priceAtMaxLiquid = dat.data.liqHeatMap.priceArray[(int)maxLiquid.ElementAt(1)];
                }

                //Giá hiện tại nằm ở 2/3 từ giá tại điểm thanh lý - giá trung bình(chính giữa màn hình)
                if (priceAtMaxLiquid > 0
                    && msg.Price >= avgPrice)
                {
                    var entry = (2 * priceAtMaxLiquid + avgPrice) / 3;
                    var sl = (priceAtMaxLiquid + 2 * avgPrice) / 3;
                    var liquid = new TradingResponse
                    {
                        s = msg.Symbol,
                        Date = DateTime.Now,
                        Type = (int)TradingResponseType.Liquid,
                        Side = Binance.Net.Enums.OrderSide.Buy,
                        Focus = sl,
                        Entry = entry,
                        TP = priceAtMaxLiquid,
                        SL = sl,
                        Status = (int)LiquidStatus.Prepare
                    };
                    return liquid;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WebSocketService.LiquidBuy|EXCEPTION| {ex.Message}");
            }

            return null;
        }

        private TradingResponse LiquidBuy_Inverse(BinanceFuturesStreamLiquidation msg, IEnumerable<List<decimal>> lLiquid, int flag, decimal avgPrice, CoinAnk_LiquidValue dat)
        {
            try
            {
                decimal priceAtMaxLiquid = 0;
                var maxLiquid = lLiquid.Where(x => x.ElementAt(1) < flag - 1).MaxBy(x => x.ElementAt(2));
                if (maxLiquid.ElementAt(2) >= (decimal)0.85 * dat.data.liqHeatMap.maxLiqValue)
                {
                    priceAtMaxLiquid = dat.data.liqHeatMap.priceArray[(int)maxLiquid.ElementAt(1)];
                }

                //Giá hiện tại nhỏ hơn giá tại điểm thanh lý
                if (priceAtMaxLiquid > 0
                    && msg.Price < priceAtMaxLiquid)
                {
                    var sl = msg.Price - Math.Abs((priceAtMaxLiquid - avgPrice) / 3);
                    var liquid = new TradingResponse
                    {
                        s = msg.Symbol,
                        Date = DateTime.Now,
                        Type = (int)TradingResponseType.Liquid,
                        Side = Binance.Net.Enums.OrderSide.Buy,
                        Focus = priceAtMaxLiquid,
                        Entry = msg.Price,
                        TP = avgPrice,
                        SL = sl,
                        Status = (int)LiquidStatus.Ready
                    };
                    return liquid;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WebSocketService.LiquidBuy_Inverse|EXCEPTION| {ex.Message}");
            }

            return null;
        }

        private TradingResponse LiquidSell(BinanceFuturesStreamLiquidation msg, IEnumerable<List<decimal>> lLiquid, int flag, decimal avgPrice, CoinAnk_LiquidValue dat)
        {
            try
            {
                decimal priceAtMaxLiquid = 0;
                var maxLiquid = lLiquid.Where(x => x.ElementAt(1) > flag).MaxBy(x => x.ElementAt(2));
                if (maxLiquid.ElementAt(2) >= (decimal)0.85 * dat.data.liqHeatMap.maxLiqValue)
                {
                    priceAtMaxLiquid = dat.data.liqHeatMap.priceArray[(int)maxLiquid.ElementAt(1)];
                }

                //Giá hiện tại nằm ở 2/3 từ giá tại điểm thanh lý - giá trung bình(chính giữa màn hình)
                if (priceAtMaxLiquid > 0
                   && msg.Price <= avgPrice)
                {
                    var entry = (2 * priceAtMaxLiquid + avgPrice) / 3;
                    var sl = (priceAtMaxLiquid + 2 * avgPrice) / 3;
                    var liquid = new TradingResponse
                    {
                        s = msg.Symbol,
                        Date = DateTime.Now,
                        Type = (int)TradingResponseType.Liquid,
                        Side = Binance.Net.Enums.OrderSide.Sell,
                        Focus = sl,
                        Entry = entry,
                        TP = priceAtMaxLiquid,
                        SL = sl,
                        Status = (int)LiquidStatus.Prepare
                    };
                    return liquid;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WebSocketService.LiquidSell|EXCEPTION| {ex.Message}");
            }

            return null;
        }

        private TradingResponse LiquidSell_Inverse(BinanceFuturesStreamLiquidation msg, IEnumerable<List<decimal>> lLiquid, int flag, decimal avgPrice, CoinAnk_LiquidValue dat)
        {
            try
            {
                decimal priceAtMaxLiquid = 0;
                var maxLiquid = lLiquid.Where(x => x.ElementAt(1) > flag).MaxBy(x => x.ElementAt(2));
                if (maxLiquid.ElementAt(2) >= (decimal)0.85 * dat.data.liqHeatMap.maxLiqValue)
                {
                    priceAtMaxLiquid = dat.data.liqHeatMap.priceArray[(int)maxLiquid.ElementAt(1)];
                }

                //Giá hiện tại lớn hơn giá tại điểm thanh lý
                if (priceAtMaxLiquid > 0
                    && msg.Price > priceAtMaxLiquid)
                {
                    var liquid = new TradingResponse
                    {
                        s = msg.Symbol,
                        Date = DateTime.Now,
                        Type = (int)TradingResponseType.Liquid,
                        Side = Binance.Net.Enums.OrderSide.Sell,
                        Focus = priceAtMaxLiquid,
                        Entry = msg.Price,
                        TP = avgPrice,
                        SL = msg.Price + Math.Abs((priceAtMaxLiquid - avgPrice) / 3),
                        Status = (int)LiquidStatus.Ready
                    };
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
                for (var i = 0; i < count; i++)
                {
                    var element = dat.data.liqHeatMap.priceArray[i];
                    if (element > msg.Price)
                    {
                        flag = i; break;
                    }
                }
                var avgPrice = (decimal)0.5 * (dat.data.liqHeatMap.priceArray[0] + dat.data.liqHeatMap.priceArray[count - 1]);

                if (flag <= 0)
                    return null;

                var lLiquid = dat.data.liqHeatMap.data.Where(x => x.ElementAt(0) >= 270);
                var lLiquidLast = lLiquid.Where(x => x.ElementAt(0) == 288);
                if (msg.Side == Binance.Net.Enums.OrderSide.Buy)
                {
                    var res = LiquidBuy(msg, lLiquidLast, flag, avgPrice, dat);
                    res ??= LiquidBuy_Inverse(msg, lLiquid, flag, avgPrice, dat);

                    return res;
                }
                else
                {
                    var res = LiquidSell(msg, lLiquidLast, flag, avgPrice, dat);
                    res ??= LiquidSell_Inverse(msg, lLiquid, flag, avgPrice, dat);

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
