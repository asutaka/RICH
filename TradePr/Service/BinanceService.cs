using Binance.Net.Objects.Models.Futures;
using MongoDB.Driver;
using System.Text;
using TradePr.DAL;
using TradePr.DAL.Entity;
using TradePr.Utils;

namespace TradePr.Service
{
    public interface IBinanceService
    {
        Task<BinanceUsdFuturesAccountBalance> GetAccountInfo();
        Task TradeTokenUnlock();
        Task MarketAction();
    }
    public class BinanceService : IBinanceService
    {
        private readonly ILogger<BinanceService> _logger;
        private readonly ICacheService _cacheService;
        private readonly ITradingRepo _tradingRepo;
        private readonly ITokenUnlockTradeRepo _tokenUnlockTradeRepo;
        private readonly IErrorPartnerRepo _errRepo;
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private const long _idUser = 1066022551;
        private const decimal _unit = 50;
        private const decimal _margin = 10;
        public BinanceService(ILogger<BinanceService> logger, ICacheService cacheService,
                            ITradingRepo tradingRepo, IAPIService apiService, ITokenUnlockTradeRepo tokenUnlockTradeRepo, 
                            ITeleService teleService, IErrorPartnerRepo errRepo) 
        { 
            _logger = logger;
            _cacheService = cacheService;
            _tradingRepo = tradingRepo;
            _apiService = apiService;
            _teleService = teleService;
            _tokenUnlockTradeRepo = tokenUnlockTradeRepo;
            _errRepo = errRepo;
        }

        public async Task<BinanceUsdFuturesAccountBalance> GetAccountInfo()
        {
            try
            {
                var resAPI = await StaticVal.BinanceInstance().UsdFuturesApi.Account.GetBalancesAsync();
                return resAPI?.Data?.FirstOrDefault(x => x.Asset == "USDT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.GetAccountInfo|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private async Task<decimal> GetPrice(string symbol)
        {
            try
            {
                var res = await _apiService.GetData(symbol, EInterval.M15);
                return res?.LastOrDefault()?.Close ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.GetPrice|EXCEPTION| {ex.Message}");
            }
            return 0;
        }

        private async Task<TokenUnlockTrade> PlaceOrder(TokenUnlock token)
        {
            try
            {
                var symbol = $"{token.s}USDT";
                var curPrice = await GetPrice(symbol);
                if (curPrice <= 0)
                    return null;

                var curTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                var account = await GetAccountInfo();
                if (account == null)
                {
                    await _teleService.SendMessage(_idUser, "[ERROR] Không lấy được thông tin tài khoản");
                    return null;
                }

                if (account.AvailableBalance * _margin <= _unit)
                    return null;

                var near = 2; if (curPrice < 1)
                {
                    near = 0;
                }
                var soluong = Math.Round(_unit / curPrice, near);
                var res = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.PlaceOrderAsync(symbol, 
                                                                                                    side: Binance.Net.Enums.OrderSide.Sell, 
                                                                                                    type: Binance.Net.Enums.FuturesOrderType.Market,
                                                                                                    positionSide: Binance.Net.Enums.PositionSide.Both,
                                                                                                    reduceOnly: false,
                                                                                                    quantity: soluong);
                Thread.Sleep(500);
                //nếu lỗi return
                if (!res.Success)
                {
                    _errRepo.InsertOne(new ErrorPartner
                    {
                        s = symbol,
                        time = curTime,
                        ty = (int)ETypeBot.TokenUnlock,
                        action = (int)EAction.Short,
                        des = $"side: {Binance.Net.Enums.OrderSide.Sell.ToString()}, type: {Binance.Net.Enums.FuturesOrderType.Market.ToString()}, quantity: {soluong}"
                    });
                    return null;
                }

                var trade = new TokenUnlockTrade
                {
                    s = symbol,
                    timeUnlock = token.time,
                    timeShort = curTime,
                };

                var resPosition = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.GetPositionsAsync(symbol);
                Thread.Sleep(500);
                if (!resPosition.Success)
                {
                    _tokenUnlockTradeRepo.InsertOne(trade);

                    _errRepo.InsertOne(new ErrorPartner
                    {
                        s = symbol,
                        time = curTime,
                        ty = (int)ETypeBot.TokenUnlock,
                        action = (int)EAction.GetPosition
                    });
                   
                    return null;
                }

                if (resPosition.Data.Any())
                {
                    var first = resPosition.Data.First();
                    trade.priceEntry = (double)first.EntryPrice;

                    if (curPrice < 1)
                    {
                        var price = curPrice.ToString().Split('.').Last();
                        price = price.ReverseString();
                        near = long.Parse(price).ToString().Length;
                    }
                    var checkLenght = curPrice.ToString().Split('.').Last();
                    var sl = Math.Round(first.EntryPrice * (decimal)1.016, near);
                    res = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.PlaceOrderAsync(first.Symbol,
                                                                                            side: Binance.Net.Enums.OrderSide.Buy,
                                                                                            type: Binance.Net.Enums.FuturesOrderType.StopMarket,
                                                                                            positionSide: Binance.Net.Enums.PositionSide.Both,
                                                                                            quantity: soluong,
                                                                                            timeInForce: Binance.Net.Enums.TimeInForce.GoodTillExpiredOrCanceled,
                                                                                            reduceOnly: true,
                                                                                            workingType: Binance.Net.Enums.WorkingType.Mark,
                                                                                            stopPrice: sl);
                    Thread.Sleep(500);
                    if (!res.Success)
                    {
                        _tokenUnlockTradeRepo.InsertOne(trade);

                        _errRepo.InsertOne(new ErrorPartner
                        {
                            s = symbol,
                            time = curTime,
                            ty = (int)ETypeBot.TokenUnlock,
                            action = (int)EAction.Short_SL,
                            des = $"side: {Binance.Net.Enums.OrderSide.Buy.ToString()}, type: {Binance.Net.Enums.FuturesOrderType.StopMarket.ToString()}, quantity: {soluong}, stopPrice: {sl}"
                        });
                        
                        return null;
                    }

                    trade.timeStoploss = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                    trade.priceStoploss = (double)sl;

                    _tokenUnlockTradeRepo.InsertOne(trade);
                }
                return trade;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.GetAccountInfo|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private async Task<TokenUnlockTrade> PlaceOrderCloseAll(string symbol, decimal quan)
        {
            try
            {
                var res = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.PlaceOrderAsync(symbol,
                                                                                                    side: Binance.Net.Enums.OrderSide.Buy,
                                                                                                    type: Binance.Net.Enums.FuturesOrderType.Market,
                                                                                                    quantity: quan);
                Thread.Sleep(500);
                if (!res.Success)
                {
                    await _teleService.SendMessage(_idUser, $"[ERROR] Không thể đóng lệnh {symbol}!");
                    return null;
                }
                var dt = DateTime.UtcNow;
                var time = (int)new DateTimeOffset(dt.Year, dt.Month, dt.Day, 0, 0, 0, TimeSpan.Zero).AddDays(1).ToUnixTimeSeconds();

                FilterDefinition<TokenUnlockTrade> filter = null;
                var builder = Builders<TokenUnlockTrade>.Filter;
                var lFilter = new List<FilterDefinition<TokenUnlockTrade>>()
                        {
                            builder.Eq(x => x.timeUnlock, time),
                            builder.Eq(x => x.s, symbol),
                        };
                foreach (var itemFilter in lFilter)
                {
                    if (filter is null)
                    {
                        filter = itemFilter;
                        continue;
                    }
                    filter &= itemFilter;
                }
                var entity = _tokenUnlockTradeRepo.GetEntityByFilter(filter);
                if (entity != null)
                {
                    entity.timeClose = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                    entity.priceClose = (double)(await GetPrice(symbol));

                    var rate = -1 * Math.Round(100 * (-1 + entity.priceClose / entity.priceEntry), 1);
                    entity.rate = rate;
                    _tokenUnlockTradeRepo.Update(entity);
                }

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.PlaceOrderCloseAll|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task TradeTokenUnlock()
        {
            try
            {
                var dt = DateTime.UtcNow;
                if (dt.Hour == 23 && dt.Minute == 58)
                {
                    var sBuilder = new StringBuilder();
                    #region Đóng vị thế
                    var resPosition = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.GetPositionsAsync();
                    if (resPosition?.Data?.Any() ?? false)
                    {
                        //close all
                        foreach (var position in resPosition.Data)
                        {
                            var res = await PlaceOrderCloseAll(position.Symbol, Math.Abs(position.PositionAmt));
                            if(res != null)
                            {
                                var mes = $"[Đóng vị thế] {res.s}|Giá đóng: {res.priceClose}|Rate: {res.rate}%";
                                sBuilder.AppendLine(mes);
                            }
                        }
                    } 
                    #endregion

                    #region Mở vị thế
                    var tokens = _cacheService.GetTokenUnlock(dt);
                    if (!tokens.Any())
                        return;

                    //action  
                    foreach (var item in tokens)
                    {

                        FilterDefinition<TokenUnlockTrade> filter = null;
                        var builder = Builders<TokenUnlockTrade>.Filter;
                        var lFilter = new List<FilterDefinition<TokenUnlockTrade>>()
                        {
                            builder.Eq(x => x.timeUnlock, item.time),
                            builder.Eq(x => x.s, $"{item.s}USDT"),
                        };
                        foreach (var itemFilter in lFilter)
                        {
                            if (filter is null)
                            {
                                filter = itemFilter;
                                continue;
                            }
                            filter &= itemFilter;
                        }
                        var entity = _tokenUnlockTradeRepo.GetEntityByFilter(filter);
                        if (entity != null)
                            continue;

                        var res = await PlaceOrder(item);
                        if (res != null)
                        {
                            var mes = $"[Mở vị thế] {res.s}|Giá mở: {res.priceEntry}|SL: {res.priceStoploss}";
                            sBuilder.AppendLine(mes);
                        }
                    } 
                    #endregion

                    if(sBuilder.Length > 0)
                    {
                        await _teleService.SendMessage(_idUser, sBuilder.ToString());
                    }    
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.TradeTokenUnlock|EXCEPTION| {ex.Message}");
            }
        }

        public async Task MarketAction()
        {
            try
            {
                var time = (int)DateTimeOffset.Now.AddMinutes(-15).ToUnixTimeSeconds();
                var lTrade = _tradingRepo.GetByFilter(Builders<Trading>.Filter.Gte(x => x.d, time));
                if (lTrade?.Any() ?? false)
                {
                    var lSym = lTrade.Select(x => x.s).Distinct();
                    if(lSym.Count() >= 3)
                    {
                        var timeUnlockTrade = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        var lTokenUnlockTrade = _tokenUnlockTradeRepo.GetByFilter(Builders<TokenUnlockTrade>.Filter.Gte(x => x.timeUnlock, timeUnlockTrade));
                        if (lTokenUnlockTrade?.Any() ?? false) 
                        {
                            var resPosition = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.GetPositionsAsync();
                            if (resPosition?.Data?.Any() ?? false)
                            {
                                //close all
                                var sBuilder = new StringBuilder();
                                foreach (var position in resPosition.Data)
                                {
                                    if (lTokenUnlockTrade.Any(x => x.s != position.Symbol))
                                        continue;

                                    var res = await PlaceOrderCloseAll(position.Symbol, Math.Abs(position.PositionAmt));
                                    if (res != null)
                                    {
                                        var mes = $"[Đóng vị thế] {res.s}|Giá đóng: {res.priceClose}|Rate: {res.rate}%";
                                        sBuilder.AppendLine(mes);
                                    }
                                }

                                if (sBuilder.Length > 0)
                                {
                                    await _teleService.SendMessage(_idUser, sBuilder.ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.MarketAction|EXCEPTION| {ex.Message}");
            }
        }
    }
}
