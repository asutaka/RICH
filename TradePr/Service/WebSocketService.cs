using MongoDB.Driver;
using TradePr.DAL;
using TradePr.DAL.Entity;
using TradePr.Utils;

namespace TradePr.Service
{
    public interface IWebSocketService
    {
        void BinanceAction();
    }
    public class WebSocketService : IWebSocketService
    {
        private const long _idUser = 1066022551;
        private readonly ILogger<WebSocketService> _logger;
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private readonly IBinanceService _binanceService;
        private readonly IPrepareTradeRepo _prepareRepo;
        private const decimal STOP_LOSS = (decimal)0.016;
        private static Dictionary<string, DateTime> _dicRes = new Dictionary<string, DateTime>();
        private object _locker = new object();

        public WebSocketService(ILogger<WebSocketService> logger, IAPIService apiService, ITeleService teleService, IPrepareTradeRepo prepareRepo, IBinanceService binanceService)
        {
            _logger = logger;
            _apiService = apiService;
            _teleService = teleService;
            _prepareRepo = prepareRepo;
            _binanceService = binanceService;
        }

        private void RemoveValue(PrepareTrade val)
        {
            Monitor.Enter(_locker);
            StaticVal._lPrepare.Remove(val);
            Monitor.Exit(_locker);
        }
        private async Task PositionMarket(PrepareTrade val, decimal lastPrice)
        {
            try
            {
                RemoveValue(val);
                //return;
                //action
                var res = await _binanceService.PlaceOrder(new SignalBase
                {
                    s = val.s,
                    Side = val.Side
                }, lastPrice);
                if (res is null)
                    return;

                await _teleService.SendMessage(_idUser, $"[ACTION] {val.s}|{(Binance.Net.Enums.OrderSide)val.Side}|ENTRY: {val.Entry}| Time: {(int)DateTimeOffset.Now.ToUnixTimeSeconds()}");
                Console.WriteLine($"[ACTION] {val.s}|{(Binance.Net.Enums.OrderSide)val.Side}|ENTRY: {val.Entry}| Time: {(int)DateTimeOffset.Now.ToUnixTimeSeconds()}");
                val.Entry_Real = res.priceEntry;
                val.SL_Real = res.priceStoploss;
                val.entryDate = DateTime.Now;
                val.entryTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                val.Status = 1;
                _prepareRepo.Update(val);
                RemoveValue(val);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WebSocketService.PositionMarket|EXCEPTION| {ex.Message}");
            }
        }
        public void BinanceAction()
        {
            try
            {
                var time = (int)DateTimeOffset.Now.AddMinutes(-15).ToUnixTimeSeconds();
                var builderBuy = Builders<PrepareTrade>.Filter;
                var lPrepare = _prepareRepo.GetByFilter(builderBuy.And(
                    builderBuy.Gte(x => x.detectTime, time),
                    builderBuy.Eq(x => x.Status, 0)
                ));
                StaticVal._lPrepare = lPrepare.ToList();

                var liquid = StaticVal.BinanceSocketInstance().UsdFuturesApi.ExchangeData.SubscribeToAllMiniTickerUpdatesAsync(async (data) =>
                {
                    try
                    {
                        Monitor.Enter(_locker);
                        var dat = data.Data.Where(x => StaticVal._lPrepare.Any(y => y.s == x.Symbol));
                        Monitor.Exit(_locker);
                        foreach (var item in dat)
                        {
                            var val = StaticVal._lPrepare.FirstOrDefault(x => x.s == item.Symbol);
                            if (val is null)
                                continue;

                            if ((DateTime.Now - val.detectDate).TotalMinutes >= 15)
                            {
                                RemoveValue(val);
                                continue;
                            }

                            if (val.Side == (int)Binance.Net.Enums.OrderSide.Buy)
                            {
                                if ((double)item.LastPrice <= val.Entry)
                                {
                                    //action
                                    await PositionMarket(val, item.LastPrice);
                                }
                            }
                            else
                            {
                                if ((double)item.LastPrice >= val.Entry)
                                {
                                    //action
                                    await PositionMarket(val, item.LastPrice);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"WebSocketService.BinanceLiquid|EXCEPTION(Detail)| {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WebSocketService.BinanceLiquid|EXCEPTION| {ex.Message}");
            }
        }
    }
}
