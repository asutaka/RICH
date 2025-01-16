using Binance.Net.Objects.Models.Futures.Socket;
using CoinPr.DAL;
using CoinPr.DAL.Entity;
using CoinPr.Model;
using CoinPr.Utils;
using MongoDB.Driver;
using Newtonsoft.Json;
using Telegram.Bot.Types;
using Websocket.Client;

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
                        Console.WriteLine($"{data.Data.Symbol}|{data.Data.Side}|{data.Data.Type}|{data.Data.Quantity}|{data.Data.Price}|{val}");
                        HandleMessage(data.Data).GetAwaiter().GetResult();
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WebSocketService.BinanceLiquid|EXCEPTION| {ex.Message}");
            }
        }

        private async Task HandleMessage(BinanceFuturesStreamLiquidation msg)
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
                if(checkOrderBlock.Item1)
                {
                    //Check OrderBlock
                    //tối thiểu 10 nến
                    var recheck = checkOrderBlock.Item2.FirstOrDefault(x => (x.interval == (int)EInterval.W1 && (date - x.Date).TotalHours >= min_1W)
                                                                            || (x.interval == (int)EInterval.D1 && (date - x.Date).TotalHours >= min_1D)
                                                                            || (x.interval == (int)EInterval.H4 && (date - x.Date).TotalHours >= min_4H)
                                                                            || (x.interval == (int)EInterval.H1 && (date - x.Date).TotalHours >= min_1H));
                    if(recheck != null)
                    {

                        var side = (recheck.Mode == (int)EOrderBlockMode.TopInsideBar || recheck.Mode == (int)EOrderBlockMode.TopPinbar) ? "SELL" : "BUY";
                        var interval = ((EInterval)recheck.interval).GetDisplayName();
                        Console.WriteLine($"{date.ToString("dd/MM/yyyy")}|ORDERBLOCK({interval})|{side}|{msg.Symbol}|ENTRY: {recheck.Entry}|SL: {recheck.SL}");
                    }
                    //Check Liquid
                    //var dat = await _apiService.CoinAnk_GetLiquidValue(msg.Symbol);
                    //if (dat?.data?.liqHeatMap != null)
                    //{

                    //}
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WebSocketService.HandleMessage|EXCEPTION| {ex.Message}");
            }
        }


        private async Task HandleMessage(ResponseMessage msg)
        {
            try
            {
                if (msg.Text.Length <= 50)
                    return;
                var res = JsonConvert.DeserializeObject<LiquidResponse>(msg.Text);
                if (res.data is null)
                    return;

                foreach (var item in res.data)
                {
                    if (!"Binance".Equals(item.exchangeName, StringComparison.OrdinalIgnoreCase)
                        //|| !_lSymbol.Contains(item.contractCode)
                        || item.tradeTurnover < 20000)
                        continue;

                    var message = $"{item.posSide}|{item.tradeTurnover.ToString("#,##0.##")}";
                    //Console.WriteLine(message);
                    var dat = await _apiService.CoinAnk_GetLiquidValue(item.contractCode);
                    Thread.Sleep(100);
                    try
                    {
                        if (dat is null || dat.data is null || dat.data.liqHeatMap is null)
                        {
                            if (item.tradeTurnover >= 25000)
                            {
                                await _teleService.SendMessage(_idUser, $"[LOG-nodataliquid] {item.baseCoin}|{message}");
                            }
                            continue;
                        }

                        var lPrice = await _apiService.GetCoinData_Binance(item.contractCode, "1h", DateTimeOffset.Now.AddHours(-12).ToUnixTimeMilliseconds());
                        if (!(lPrice?.Any() ?? false))
                        {
                            if (item.tradeTurnover >= 25000)
                            {
                                await _teleService.SendMessage(_idUser, $"[LOG-nodataprice] {item.baseCoin}|{message}");
                            }
                            continue;
                        }

                        var flag = -1;
                        var curPrice = lPrice.Last().Close;
                        var count = dat.data.liqHeatMap.priceArray.Count();
                        for (var i = 0; i < count; i++)
                        {
                            var element = dat.data.liqHeatMap.priceArray[i];
                            if (element > curPrice)
                            {
                                flag = i; break;
                            }
                        }

                        if (flag < 0)
                        {
                            await _teleService.SendMessage(_idUser, $"[LOG-noflag] {item.baseCoin}({curPrice})|{message}");
                            continue;
                        }

                        var lLiquid = dat.data.liqHeatMap.data.Where(x => x.ElementAt(0) >= 280);
                        var lLiquidLast = lLiquid.Where(x => x.ElementAt(0) == 288);
                        var minPrice = lPrice.Min(x => x.Low);
                        var maxPrice = lPrice.Max(x => x.High);
                        if (item.posSide.Equals("short", StringComparison.OrdinalIgnoreCase))
                        {
                            decimal priceMaxCeil = 0;
                            var maxCeil = lLiquidLast.Where(x => x.ElementAt(1) > flag).MaxBy(x => x.ElementAt(2));
                            if (maxCeil.ElementAt(2) >= (decimal)0.9 * dat.data.liqHeatMap.maxLiqValue)
                            {
                                priceMaxCeil = dat.data.liqHeatMap.priceArray[(int)maxCeil.ElementAt(1)];
                            }
                            if (priceMaxCeil <= 0)
                            {
                                if (item.tradeTurnover >= 25000)
                                {
                                    await _teleService.SendMessage(_idUser, $"[LOG-noliquid] {item.baseCoin}({curPrice})|{message}");
                                }
                                continue;
                            }
                            if ((2 * priceMaxCeil + minPrice) <= 3 * curPrice
                                && (8 * priceMaxCeil + minPrice) > 9 * curPrice
                                && curPrice < maxPrice * (decimal)0.98)
                            {
                                //Buy khi giá gần đến điểm thanh lý trên(2/3)
                                var tp = Math.Round(100 * (-1 + priceMaxCeil / curPrice), 1);

                                var mess = $"|LONG|{item.baseCoin}|Entry: {curPrice} --> Giá tăng gần đến điểm thanh lý({priceMaxCeil})|TP({tp}%)|SL(-2%):{curPrice * (decimal)0.98}";
                                await _teleService.SendMessage(_idUser, mess);
                                continue;
                                //Console.WriteLine(mess);
                            }

                            priceMaxCeil = 0;
                            maxCeil = lLiquid.Where(x => x.ElementAt(1) > flag).MaxBy(x => x.ElementAt(2));
                            if (maxCeil.ElementAt(2) >= (decimal)0.9 * dat.data.liqHeatMap.maxLiqValue)
                            {
                                priceMaxCeil = dat.data.liqHeatMap.priceArray[(int)maxCeil.ElementAt(1)];
                            }
                            if (priceMaxCeil <= 0)
                            {
                                await _teleService.SendMessage(_idUser, $"[LOG-noliquid] {item.baseCoin}({curPrice})|{message}");
                                continue;
                            }

                            if (curPrice > priceMaxCeil)
                            {
                                //Sell khi giá vượt qua điểm thanh lý trên
                                var mess = $"|SHORT|{item.baseCoin}|Entry: {curPrice} --> Giá tăng vượt qua điểm thanh lý điểm thanh lý: {priceMaxCeil}|TP(5%): {curPrice * (decimal)0.95}|SL(3%):{curPrice * (decimal)1.03}";
                                await _teleService.SendMessage(_idUser, mess);
                                //Console.WriteLine(mess);
                            }
                            else
                            {
                                if (item.tradeTurnover >= 25000)
                                {
                                    await _teleService.SendMessage(_idUser, $"[LOG-nopassrule] {item.baseCoin}({curPrice})|{message}");
                                }
                            }
                        }
                        else
                        {
                            decimal priceMaxFloor = 0;
                            var maxFloor = lLiquidLast.Where(x => x.ElementAt(1) < flag - 1).MaxBy(x => x.ElementAt(2));
                            if (maxFloor.ElementAt(2) >= (decimal)0.9 * dat.data.liqHeatMap.maxLiqValue)
                            {
                                priceMaxFloor = dat.data.liqHeatMap.priceArray[(int)maxFloor.ElementAt(1)];
                            }
                            if (priceMaxFloor <= 0)
                            {
                                if (item.tradeTurnover >= 25000)
                                {
                                    await _teleService.SendMessage(_idUser, $"[LOG-noliquid] {item.baseCoin}({curPrice})|{message}");
                                }
                                continue;
                            }

                            if ((maxPrice + 2 * priceMaxFloor) >= 3 * curPrice
                                && (maxPrice + 8 * priceMaxFloor) < 9 * curPrice
                                && curPrice > minPrice * (decimal)1.02)
                            {
                                //Sell khi giá gần đến điểm thanh lý dưới(1/3)
                                var tp = Math.Abs(Math.Round(100 * (-1 + priceMaxFloor / curPrice), 1));
                                var mess = $"|SHORT|{item.baseCoin}|Entry: {curPrice} --> Giá giảm gần đến điểm thanh lý: {priceMaxFloor}|TP({tp}%)|SL(2%):{curPrice * (decimal)1.02}";
                                await _teleService.SendMessage(_idUser, mess);
                                continue;
                                //Console.WriteLine(mess);
                            }

                            priceMaxFloor = 0;
                            maxFloor = lLiquid.Where(x => x.ElementAt(1) < flag - 1).MaxBy(x => x.ElementAt(2));
                            if (maxFloor.ElementAt(2) >= (decimal)0.9 * dat.data.liqHeatMap.maxLiqValue)
                            {
                                priceMaxFloor = dat.data.liqHeatMap.priceArray[(int)maxFloor.ElementAt(1)];
                            }
                            if (priceMaxFloor <= 0)
                            {
                                await _teleService.SendMessage(_idUser, $"[LOG-noliquid] {item.baseCoin}({curPrice})|{message}");
                                continue;
                            }

                            if (curPrice < priceMaxFloor)
                            {
                                //Buy khi giá gần đến điểm thanh lý dưới(1/3)
                                var mess = $"|LONG|{item.baseCoin}|Entry: {curPrice} --> Giá giảm vượt qua điểm thanh lý: {priceMaxFloor}|TP(5%): {curPrice * (decimal)1.05}|SL(3%):{curPrice * (decimal)0.97}";
                                await _teleService.SendMessage(_idUser, mess);
                                //Console.WriteLine(mess);
                            }
                            else
                            {
                                if (item.tradeTurnover >= 25000)
                                {
                                    await _teleService.SendMessage(_idUser, $"[LOG-nopassrule] {item.baseCoin}({curPrice})|{message}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"WebSocketService.HandleMessage|EXCEPTION|INPUT: {JsonConvert.SerializeObject(item)}| {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WebSocketService.HandleMessage|EXCEPTION| {ex.Message}");
            }
        }
    }
}
