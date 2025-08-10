using Binance.Net.Objects.Models.Futures.Socket;
using CoinPr.DAL;
using CoinPr.DAL.Entity;
using CoinUtilsPr;
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
        private const decimal STOP_LOSS = (decimal)0.016;
        
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

                    var builder = Builders<Trading>.Filter;
                    var entity = _tradingRepo.GetEntityByFilter(builder.And(
                        builder.Eq(x => x.s, data.Data.Symbol),
                        builder.Gte(x => x.d, (int)DateTimeOffset.Now.AddHours(-1).ToUnixTimeSeconds())
                    ));
                    if (entity != null)
                        return;

                    var mes = HandleMessage(data.Data).GetAwaiter().GetResult();
                    if (string.IsNullOrWhiteSpace(mes))
                        return;

                    //_teleService.SendMessage(_idUser, mes).GetAwaiter().GetResult();
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

               
                _tradingRepo.InsertOne(liquid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WebSocketService.HandleMessage|EXCEPTION| {ex.Message}");
            }
            return mes;
        }

        private async Task<Trading> CheckLiquid(BinanceFuturesStreamLiquidation msg)
        {
            try
            {
                var lData15M = await _apiService.GetData_Binance(msg.Symbol, EInterval.M15);
                Thread.Sleep(200);
                if (lData15M == null)
                    return null;

                var lRsi15M = lData15M.GetRsi();
                var rsi = lRsi15M.Last().Rsi;
                if(rsi > 35 && rsi < 65) //rsi không nằm trong vùng tín hiệu
                {
                    return null;
                }

                var dat = await _apiService.CoinAnk_GetLiquidValue(msg.Symbol);
                if (dat?.data?.liqHeatMap is null)//Không lấy đc liquid data
                    return null;

                var cur = msg.AveragePrice;
                //Giá trung bình của liquid
                var avgPrice = (decimal)0.5 * (dat.data.liqHeatMap.priceArray[0] + dat.data.liqHeatMap.priceArray[dat.data.liqHeatMap.priceArray.Count() - 1]);
                var flag = dat.data.liqHeatMap.priceArray.FindIndex(x => x > avgPrice);
                if (flag <= 0)
                    return null;

                var lLiquid = dat.data.liqHeatMap.data.Where(x => x.ElementAt(0) >= 286);
                if (cur >= avgPrice)
                {
                    var lMaxliquid = lLiquid.Where(x => x.ElementAt(1) > flag).Where(x => x.ElementAt(2) >= (decimal)0.88 * dat.data.liqHeatMap.maxLiqValue);
                    if (lMaxliquid == null || !lMaxliquid.Any())
                        return null;

                    var priceAtMaxLiquid = dat.data.liqHeatMap.priceArray[(int)lMaxliquid.MaxBy(x => x.ElementAt(1)).ElementAt(1)];
                    if (cur < priceAtMaxLiquid)
                        return null;

                    var liquid = new Trading
                    {
                        s = msg.Symbol,
                        d = (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                        Date = DateTime.Now,
                        Side = (int)Binance.Net.Enums.OrderSide.Sell,
                        Price = (double)cur,
                        Liquid = (double)priceAtMaxLiquid,
                        Status = 0
                    };
                    return liquid;
                }
                else
                {
                    var lMaxliquid = lLiquid.Where(x => x.ElementAt(1) < flag).Where(x => x.ElementAt(2) >= (decimal)0.88 * dat.data.liqHeatMap.maxLiqValue);
                    if (lMaxliquid == null || !lMaxliquid.Any())
                        return null;

                    var priceAtMaxLiquid = dat.data.liqHeatMap.priceArray[(int)lMaxliquid.MinBy(x => x.ElementAt(1)).ElementAt(1)];
                    if (cur > priceAtMaxLiquid)
                        return null;

                    var liquid = new Trading
                    {
                        s = msg.Symbol,
                        d = (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                        Date = DateTime.Now,
                        Side = (int)Binance.Net.Enums.OrderSide.Buy,
                        Price = (double)cur,
                        Liquid = (double)priceAtMaxLiquid,
                        Status = 0
                    };
                    return liquid;
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