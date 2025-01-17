using Binance.Net.Objects.Models.Futures.Socket;
using CoinPr.DAL;
using CoinPr.DAL.Entity;
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
        private readonly IOrderBlockRepo _orderBlockRepo;
        
        public WebSocketService(ILogger<WebSocketService> logger, IAPIService apiService, ITeleService teleService, IOrderBlockRepo orderBlockRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _teleService = teleService;
            _orderBlockRepo = orderBlockRepo;
        }

        public void BinanceLiquid()
        {
            try
            {
                var liquid = StaticVal.BinanceSocketInstance().UsdFuturesApi.ExchangeData.SubscribeToAllLiquidationUpdatesAsync((data) =>
                {
                    var val = Math.Round(data.Data.Price * data.Data.Quantity);
                    if(val >= 20000)
                    {
                        Console.WriteLine($"{data.Data.Symbol}|{data.Data.Side}|{data.Data.Price}|{val}");
                        var mes = HandleMessage(data.Data).GetAwaiter().GetResult();
                        if (!string.IsNullOrWhiteSpace(mes))
                        {
                            Console.WriteLine(mes);
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
            try
            {
                var date = DateTime.UtcNow;
                var ob = CheckOrderBlock(msg);
                var liquid = await CheckLiquid(msg);

                //show
                if (ob is null && liquid is null)
                    return null;

                string mes = string.Empty;

                if(ob != null && liquid is null)
                {
                    mes = $"{date.ToString("dd/MM/yyyy HH:mm")}|ORDERBLOCK({((EInterval)ob.Interval).GetDisplayName()})|{ob.Side}|{ob.s}|ENTRY: {ob.Entry}|SL: {ob.SL}";
                }
                else if(liquid != null)
                {
                    //add To list follow
                    if (liquid.Status == (int)LiquidStatus.Prepare)
                    {
                        mes = $"{date.ToString("dd/MM/yyyy HH:mm")}|ORDERBLOCK({((EInterval)ob.Interval).GetDisplayName()})-PREPARE|{ob.Side}|{ob.s}|ENTRY: {ob.Entry}|SL: {ob.SL}";
                    }
                    else
                    {
                        if(ob is null)
                        {
                            mes = $"{date.ToString("dd/MM/yyyy HH:mm")}|LIQUID|{liquid.Side}|{liquid.s}|ENTRY: {liquid.Entry}|SL: {liquid.SL}";
                        }
                        else
                        {
                            var div = ob.Entry - liquid.Entry;
                            var isOb = (ob.Side == Binance.Net.Enums.OrderSide.Buy && div < 0)
                                    || (ob.Side == Binance.Net.Enums.OrderSide.Sell && div > 0);
                            if (isOb)
                            {
                                mes = $"{date.ToString("dd/MM/yyyy HH:mm")}|ORDERBLOCK({((EInterval)ob.Interval).GetDisplayName()})-LIQUID|{ob.Side}|{ob.s}|ENTRY: {ob.Entry}|SL: {ob.SL}";
                            }
                            else
                            {
                                mes = $"{date.ToString("dd/MM/yyyy HH:mm")}|LIQUID-ORDERBLOCK({((EInterval)ob.Interval).GetDisplayName()})|{liquid.Side}|{liquid.s}|ENTRY: {liquid.Entry}|SL: {liquid.SL}";
                            }
                        }
                    }
                }

                return mes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WebSocketService.HandleMessage|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private TradingResponse CheckOrderBlock(BinanceFuturesStreamLiquidation msg)
        {
            try
            {
                var date = DateTime.Now;
                var min_1W = 240 * 7;
                var min_1D = 240;
                var min_4H = 40;
                var min_1H = 10;

                var lOrderBlock = _orderBlockRepo.GetByFilter(Builders<OrderBlock>.Filter.Eq(x => x.s, msg.Symbol))
                                          .OrderByDescending(x => x.interval).ToList();
                var checkOrderBlock = msg.Price.IsOrderBlock(lOrderBlock, 0);
                if (checkOrderBlock.Item1)
                {
                    //tối thiểu 10 nến
                    var recheck = checkOrderBlock.Item2.FirstOrDefault(x => (x.interval == (int)EInterval.W1 && (date - x.Date).TotalHours >= min_1W)
                                                                            || (x.interval == (int)EInterval.D1 && (date - x.Date).TotalHours >= min_1D)
                                                                            || (x.interval == (int)EInterval.H4 && (date - x.Date).TotalHours >= min_4H)
                                                                            || (x.interval == (int)EInterval.H1 && (date - x.Date).TotalHours >= min_1H));
                    if (recheck != null)
                    {
                        var ob = new TradingResponse
                        {
                            s = msg.Symbol,
                            Date = date,
                            Type = (int)TradingResponseType.OrderBlock,
                            Side = (recheck.Mode == (int)EOrderBlockMode.TopInsideBar || recheck.Mode == (int)EOrderBlockMode.TopPinbar) ? Binance.Net.Enums.OrderSide.Sell : Binance.Net.Enums.OrderSide.Buy,
                            Interval = recheck.interval,
                            Entry = recheck.Entry,
                            SL = recheck.SL
                        };
                        return ob;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WebSocketService.CheckOrderBlock|EXCEPTION| {ex.Message}");
            }
            return null;
        }
        private async Task<TradingResponse> CheckLiquid(BinanceFuturesStreamLiquidation msg)
        {
            try
            {
                var date = DateTime.Now;
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

                var lLiquid = dat.data.liqHeatMap.data.Where(x => x.ElementAt(0) >= 280);
                var lLiquidLast = lLiquid.Where(x => x.ElementAt(0) == 288);
                if (msg.Side == Binance.Net.Enums.OrderSide.Buy)
                {
                    decimal priceAtMaxLiquid = 0;
                    var maxLiquid = lLiquidLast.Where(x => x.ElementAt(1) < flag - 1).MaxBy(x => x.ElementAt(2));
                    if (maxLiquid.ElementAt(2) >= (decimal)0.9 * dat.data.liqHeatMap.maxLiqValue)
                    {
                        priceAtMaxLiquid = dat.data.liqHeatMap.priceArray[(int)maxLiquid.ElementAt(1)];
                    }
                    if (priceAtMaxLiquid > 0
                        && (2 * priceAtMaxLiquid + avgPrice) >= 3 * msg.Price
                        && (8 * priceAtMaxLiquid + avgPrice) < 9 * msg.Price)
                    {
                        var liquid = new TradingResponse
                        {
                            s = msg.Symbol,
                            Date = date,
                            Type = (int)TradingResponseType.Liquid,
                            Side = Binance.Net.Enums.OrderSide.Buy,
                            Price = priceAtMaxLiquid,
                            Status = (int)LiquidStatus.Prepare
                        };
                        return liquid;
                    }

                    //
                    priceAtMaxLiquid = 0;
                    maxLiquid = lLiquid.Where(x => x.ElementAt(1) < flag - 1).MaxBy(x => x.ElementAt(2));
                    if (maxLiquid.ElementAt(2) >= (decimal)0.9 * dat.data.liqHeatMap.maxLiqValue)
                    {
                        priceAtMaxLiquid = dat.data.liqHeatMap.priceArray[(int)maxLiquid.ElementAt(1)];
                    }

                    if (priceAtMaxLiquid > 0
                        && msg.Price < priceAtMaxLiquid)
                    {
                        var sl = msg.Price - Math.Abs((priceAtMaxLiquid - avgPrice) / 3);
                        var liquid = new TradingResponse
                        {
                            s = msg.Symbol,
                            Date = date,
                            Type = (int)TradingResponseType.Liquid,
                            Side = Binance.Net.Enums.OrderSide.Buy,
                            Price = msg.Price,
                            Entry = msg.Price,
                            SL = sl,
                            Status = (int)LiquidStatus.Ready
                        };
                        return liquid;
                    }
                }
                else
                {
                    decimal priceAtMaxLiquid = 0;
                    var maxLiquid = lLiquidLast.Where(x => x.ElementAt(1) > flag).MaxBy(x => x.ElementAt(2));
                    if (maxLiquid.ElementAt(2) >= (decimal)0.9 * dat.data.liqHeatMap.maxLiqValue)
                    {
                        priceAtMaxLiquid = dat.data.liqHeatMap.priceArray[(int)maxLiquid.ElementAt(1)];
                    }

                    if (priceAtMaxLiquid > 0
                        && (2 * priceAtMaxLiquid + avgPrice) <= 3 * msg.Price
                        && (8 * priceAtMaxLiquid + avgPrice) > 9 * msg.Price)
                    {
                        var liquid = new TradingResponse
                        {
                            s = msg.Symbol,
                            Date = date,
                            Type = (int)TradingResponseType.Liquid,
                            Side = Binance.Net.Enums.OrderSide.Sell,
                            Price = priceAtMaxLiquid,
                            Status = (int)LiquidStatus.Prepare
                        };
                        return liquid;
                    }
                    //
                    priceAtMaxLiquid = 0;
                    maxLiquid = lLiquid.Where(x => x.ElementAt(1) > flag).MaxBy(x => x.ElementAt(2));
                    if (maxLiquid.ElementAt(2) >= (decimal)0.9 * dat.data.liqHeatMap.maxLiqValue)
                    {
                        priceAtMaxLiquid = dat.data.liqHeatMap.priceArray[(int)maxLiquid.ElementAt(1)];
                    }

                    if (priceAtMaxLiquid > 0
                        && msg.Price > priceAtMaxLiquid)
                    {
                        var sl = msg.Price + (priceAtMaxLiquid - avgPrice) / 3;
                        var liquid = new TradingResponse
                        {
                            s = msg.Symbol,
                            Date = date,
                            Type = (int)TradingResponseType.Liquid,
                            Side = Binance.Net.Enums.OrderSide.Sell,
                            Price = msg.Price,
                            Entry = msg.Price,
                            SL = sl,
                            Status = (int)LiquidStatus.Ready
                        };
                        return liquid;
                    }
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
