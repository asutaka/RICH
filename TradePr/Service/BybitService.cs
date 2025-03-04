using Bybit.Net.Objects.Models.V5;
using MongoDB.Driver;
using Skender.Stock.Indicators;
using System.Linq;
using TradePr.DAL;
using TradePr.DAL.Entity;
using TradePr.Utils;

namespace TradePr.Service
{
    public interface IBybitService
    {
        Task<BybitAssetBalance> GetAccountInfo();
        Task TradeSignal();
    }
    public class BybitService : IBybitService
    {
        private readonly ILogger<BybitService> _logger;
        private readonly ICacheService _cacheService;
        private readonly ITradingRepo _tradingRepo;
        private readonly ITokenUnlockTradeRepo _tokenUnlockTradeRepo;
        private readonly ISignalTradeRepo _signalTradeRepo;
        private readonly IErrorPartnerRepo _errRepo;
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private readonly IConfigDataRepo _configRepo;
        private const long _idUser = 1066022551;
        private const decimal _unit = 50;
        private const decimal _margin = 10;
        public BybitService(ILogger<BybitService> logger, ICacheService cacheService,
                            ITradingRepo tradingRepo, IAPIService apiService, ITokenUnlockTradeRepo tokenUnlockTradeRepo,
                            ISignalTradeRepo signalTradeRepo, ITeleService teleService, IErrorPartnerRepo errRepo, IConfigDataRepo configRepo)
        {
            _logger = logger;
            _cacheService = cacheService;
            _tradingRepo = tradingRepo;
            _apiService = apiService;
            _teleService = teleService;
            _tokenUnlockTradeRepo = tokenUnlockTradeRepo;
            _signalTradeRepo = signalTradeRepo;
            _errRepo = errRepo;
            _configRepo = configRepo;
        }
        public async Task<BybitAssetBalance> GetAccountInfo()
        {
            try
            {
                var resAPI = await StaticVal.ByBitInstance().V5Api.Account.GetBalancesAsync( Bybit.Net.Enums.AccountType.Unified);
                return resAPI?.Data?.List?.FirstOrDefault().Assets.FirstOrDefault(x => x.Asset == "USDT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BybitService.GetAccountInfo|EXCEPTION| {ex.Message}");
            }
            return null;
        }
        public async Task TradeSignal()
        {
            try
            {
                var time = (int)DateTimeOffset.Now.AddMinutes(-60).ToUnixTimeSeconds();
                var lTrade = _tradingRepo.GetByFilter(Builders<Trading>.Filter.Gte(x => x.d, time));
                if (!(lTrade?.Any() ?? false))
                    return;

                var lSignal = _signalTradeRepo.GetByFilter(Builders<SignalTrade>.Filter.Gte(x => x.timeFlag, time));

                var lSym = lTrade.Select(x => x.s).Distinct();
                foreach (var item in lSym)
                {
                    if (StaticVal._lIgnoreThreeSignal.Contains(item))
                        continue;

                    var lTradeSym = lTrade.Where(x => x.s == item).OrderByDescending(x => x.d);
                    if (lTradeSym.Count() < 2)
                        continue;                  

                    var first = lTradeSym.First();
                    var second = lTradeSym.FirstOrDefault(x => x.Date < first.Date && x.Side == first.Side);
                    if (second is null)
                        continue;

                    if (lSignal.Any(x => x.s == item && x.Side == first.Side))
                        continue;

                    var divTime = (first.Date - second.Date).TotalMinutes;
                    if (divTime > 15)
                        continue;

                    //gia
                    var lData15m = await _apiService.GetData(item, EInterval.M15);
                    var itemCheck = lData15m.SkipLast(1).Last();
                    if (itemCheck.Open >= itemCheck.Close)
                        continue;

                    //Save Record
                    var entity = new SignalTrade
                    {
                        s = item,
                        Side = first.Side,
                        timeFlag = (int)DateTimeOffset.Now.ToUnixTimeSeconds()
                    };

                    _signalTradeRepo.InsertOne(entity);

                    //Trade
                    var res = await PlaceOrder(entity, lData15m.Last());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BybitService.TradeSignal|EXCEPTION| {ex.Message}");
            }
            return;
        }

        private async Task<TokenUnlockTrade> PlaceOrder(SignalTrade entity, Quote quote)
        {
            try
            {
                var curTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                var account = await GetAccountInfo();
                if (account == null)
                {
                    await _teleService.SendMessage(_idUser, "[ERROR] Không lấy được thông tin tài khoản");
                    return null;
                }

                //if (account.AvailableBalance * _margin <= _unit)
                //    return null;

                //var near = 2; if (quote.Close < 1)
                //{
                //    near = 0;
                //}
                //var soluong = Math.Round(_unit / quote.Close, near);
                //var res = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.PlaceOrderAsync(entity.s,
                //                                                                                    side: Binance.Net.Enums.OrderSide.Sell,
                //                                                                                    type: Binance.Net.Enums.FuturesOrderType.Market,
                //                                                                                    positionSide: Binance.Net.Enums.PositionSide.Both,
                //                                                                                    reduceOnly: false,
                //                                                                                    quantity: soluong);
                //Thread.Sleep(500);
                ////nếu lỗi return
                //if (!res.Success)
                //{
                //    _errRepo.InsertOne(new ErrorPartner
                //    {
                //        s = entity.s,
                //        time = curTime,
                //        ty = (int)ETypeBot.TokenUnlock,
                //        action = (int)EAction.Short,
                //        des = $"side: {Binance.Net.Enums.OrderSide.Sell.ToString()}, type: {Binance.Net.Enums.FuturesOrderType.Market.ToString()}, quantity: {soluong}"
                //    });
                //    return null;
                //}

                //var trade = new TokenUnlockTrade
                //{
                //    s = entity.s,
                //    timeUnlock = token.time,
                //    timeShort = curTime,
                //};

                //var resPosition = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.GetPositionsAsync(entity.s);
                //Thread.Sleep(500);
                //if (!resPosition.Success)
                //{
                //    _tokenUnlockTradeRepo.InsertOne(trade);

                //    _errRepo.InsertOne(new ErrorPartner
                //    {
                //        s = entity.s,
                //        time = curTime,
                //        ty = (int)ETypeBot.TokenUnlock,
                //        action = (int)EAction.GetPosition
                //    });

                //    return null;
                //}

                //if (resPosition.Data.Any())
                //{
                //    var first = resPosition.Data.First();
                //    trade.priceEntry = (double)first.EntryPrice;

                //    if (quote.Close < 1)
                //    {
                //        var price = quote.Close.ToString().Split('.').Last();
                //        price = price.ReverseString();
                //        near = long.Parse(price).ToString().Length;
                //    }
                //    var checkLenght = quote.Close.ToString().Split('.').Last();
                //    var sl = Math.Round(first.EntryPrice * (decimal)1.016, near);
                //    res = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.PlaceOrderAsync(first.Symbol,
                //                                                                            side: Binance.Net.Enums.OrderSide.Buy,
                //                                                                            type: Binance.Net.Enums.FuturesOrderType.StopMarket,
                //                                                                            positionSide: Binance.Net.Enums.PositionSide.Both,
                //                                                                            quantity: soluong,
                //                                                                            timeInForce: Binance.Net.Enums.TimeInForce.GoodTillExpiredOrCanceled,
                //                                                                            reduceOnly: true,
                //                                                                            workingType: Binance.Net.Enums.WorkingType.Mark,
                //                                                                            stopPrice: sl);
                //    Thread.Sleep(500);
                //    if (!res.Success)
                //    {
                //        _tokenUnlockTradeRepo.InsertOne(trade);

                //        _errRepo.InsertOne(new ErrorPartner
                //        {
                //            s = entity.s,
                //            time = curTime,
                //            ty = (int)ETypeBot.TokenUnlock,
                //            action = (int)EAction.Short_SL,
                //            des = $"side: {Binance.Net.Enums.OrderSide.Buy.ToString()}, type: {Binance.Net.Enums.FuturesOrderType.StopMarket.ToString()}, quantity: {soluong}, stopPrice: {sl}"
                //        });

                //        return null;
                //    }

                //    trade.timeStoploss = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                //    trade.priceStoploss = (double)sl;

                //    _tokenUnlockTradeRepo.InsertOne(trade);
                //}
                //return trade;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BybitService.PlaceOrder|EXCEPTION| {ex.Message}");
            }
            return null;
        }
    }
}
