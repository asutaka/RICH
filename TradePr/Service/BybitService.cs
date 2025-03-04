using Bybit.Net.Objects.Models.V5;
using MongoDB.Driver;
using Newtonsoft.Json;
using Skender.Stock.Indicators;
using System.Collections.Generic;
using System.Text;
using TradePr.DAL;
using TradePr.DAL.Entity;
using TradePr.Utils;

namespace TradePr.Service
{
    public interface IBybitService
    {
        Task<BybitAssetBalance> Bybit_GetAccountInfo();
        Task Bybit_TradeSignal();
        Task Bybit_TradeThreeSignal();
        Task Bybit_TradeTokenUnlock();
        Task Bybit_MarketAction();
    }
    public class BybitService : IBybitService
    {
        private readonly ILogger<BybitService> _logger;
        private readonly ITradingRepo _tradingRepo;
        private readonly ISignalTradeRepo _signalTradeRepo;
        private readonly IThreeSignalTradeRepo _threeSignalTradeRepo;
        private readonly ITokenUnlockTradeRepo _tokenUnlockTradeRepo;
        private readonly IErrorPartnerRepo _errRepo;
        private readonly IConfigDataRepo _configRepo;
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private readonly ICacheService _cacheService;
        private const long _idUser = 1066022551;
        private const decimal _unit = 50;
        private const decimal _margin = 10;
        private readonly int _exchange = (int)EExchange.Bybit;
        public BybitService(ILogger<BybitService> logger, ITradingRepo tradingRepo, ISignalTradeRepo signalTradeRepo, IErrorPartnerRepo errRepo,
                            IConfigDataRepo configRepo, IThreeSignalTradeRepo threeSignalTradeRepo, ITokenUnlockTradeRepo tokenUnlockTradeRepo,
                            IAPIService apiService, ITeleService teleService, ICacheService cacheService)
        {
            _logger = logger;
            _tradingRepo = tradingRepo;
            _signalTradeRepo = signalTradeRepo;
            _errRepo = errRepo;
            _configRepo = configRepo;
            _threeSignalTradeRepo = threeSignalTradeRepo;
            _tokenUnlockTradeRepo = tokenUnlockTradeRepo;
            _apiService = apiService;
            _teleService = teleService;
            _cacheService = cacheService;
        }
        public async Task<BybitAssetBalance> Bybit_GetAccountInfo()
        {
            try
            {
                var resAPI = await StaticVal.ByBitInstance().V5Api.Account.GetBalancesAsync( Bybit.Net.Enums.AccountType.Unified);
                return resAPI?.Data?.List?.FirstOrDefault().Assets.FirstOrDefault(x => x.Asset == "USDT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BybitService.Bybit_GetAccountInfo|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task Bybit_TradeSignal()
        {
            try
            {
                var config = _configRepo.GetAll();
                if (config.FirstOrDefault(x => x.ex == (int)EExchange.Bybit && x.op == (int)EOption.Signal && x.status > 0) is null)
                    return;

                var time = (int)DateTimeOffset.Now.AddMinutes(-60).ToUnixTimeSeconds();
                var lTrade = _tradingRepo.GetByFilter(Builders<Trading>.Filter.Gte(x => x.d, time));
                if (!(lTrade?.Any() ?? false))
                    return;

                var builder = Builders<SignalTrade>.Filter;
                var lSignal = _signalTradeRepo.GetByFilter(builder.And(
                    builder.Eq(x => x.ex, _exchange),
                    builder.Gte(x => x.timeFlag, time)
                ));
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

                    //Trade
                    var res = await PlaceOrder(new SignalBase
                    {
                        s = item,
                        ex = _exchange,
                        Side = first.Side,
                        timeFlag = (int)DateTimeOffset.Now.ToUnixTimeSeconds()
                    }, lData15m.Last());

                    if(res != null)
                    {
                        _signalTradeRepo.InsertOne(new SignalTrade
                        {
                            s = res.s,
                            ex = res.ex,
                            Side = res.Side,
                            timeFlag = res.timeFlag,
                            timeStoploss = res.timeStoploss,
                            priceEntry = res.priceEntry,
                            priceStoploss = res.priceStoploss,
                        });
                        var mes = $"[SIGNAL][Mở vị thế {((Bybit.Net.Enums.OrderSide)res.Side).ToString().ToUpper()}] {res.s}|Giá mở: {res.priceEntry}|SL: {res.priceStoploss}";
                        await _teleService.SendMessage(_idUser, mes);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BybitService.Bybit_TradeSignal|EXCEPTION| {ex.Message}");
            }
            return;
        }

        public async Task Bybit_TradeThreeSignal()
        {
            try
            {
                var config = _configRepo.GetAll();
                if (config.FirstOrDefault(x => x.ex == (int)EExchange.Bybit && x.op == (int)EOption.ThreeSignal && x.status > 0) is null)
                    return;

                var time = (int)DateTimeOffset.Now.AddMinutes(-15).ToUnixTimeSeconds();
                var lTrade = _tradingRepo.GetByFilter(Builders<Trading>.Filter.Gte(x => x.d, time));
                if (!(lTrade?.Any() ?? false))
                    return;

                var lGroup = lTrade.GroupBy(c => new
                {
                    c.s,
                    c.Side
                })
                .Select(gcs => new
                {
                    gcs.Key.s,
                    gcs.Key.Side,
                    Count = gcs.Count(),
                })
                .Where(x => x.Count >= 3);

                if (!lGroup.Any())
                    return;

                var timeFlag = (int)DateTimeOffset.Now.AddHours(-1).ToUnixTimeSeconds();
                var builder = Builders<ThreeSignalTrade>.Filter;
                var lThreeSignal = _threeSignalTradeRepo.GetByFilter(builder.And(
                    builder.Eq(x => x.ex, _exchange),
                    builder.Gte(x => x.timeFlag, timeFlag)
                ));
                if (lThreeSignal is null)
                    lThreeSignal = new List<ThreeSignalTrade>();

                foreach (var item in lGroup)
                {
                    try
                    {
                        if (lThreeSignal.Any(x => x.s == item.s && x.Side == item.Side))
                            continue;

                        var lData15m = await _apiService.GetData(item.s, EInterval.M15);
                        var res = await PlaceOrder(new SignalBase
                        {
                            s = item.s,
                            ex = _exchange,
                            Side = item.Side,
                            timeFlag = (int)DateTimeOffset.Now.ToUnixTimeSeconds()
                        }, lData15m.Last());
                        if (res != null)
                        {
                            _threeSignalTradeRepo.InsertOne(new ThreeSignalTrade
                            {
                                s = res.s,
                                ex = res.ex,
                                Side = res.Side,
                                timeFlag = res.timeFlag,
                                timeStoploss = res.timeStoploss,
                                priceEntry = res.priceEntry,
                                priceStoploss = res.priceStoploss
                            });
                            var mes = $"[THREE][Mở vị thế {((Bybit.Net.Enums.OrderSide)res.Side).ToString().ToUpper()}] {res.s}|Giá mở: {res.priceEntry}|SL: {res.priceStoploss}";
                            await _teleService.SendMessage(_idUser, mes);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"BybitService.TradeThreeSignal|EXCEPTION|INPUT: {JsonConvert.SerializeObject(item)}| {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BybitService.Bybit_TradeThreeSignal|EXCEPTION| {ex.Message}");
            }
        }

        public async Task Bybit_TradeTokenUnlock()
        {
            try
            {
                var config = _configRepo.GetAll();
                if (config.FirstOrDefault(x => x.ex == (int)EExchange.Bybit && x.op == (int)EOption.Unlock && x.status > 0) is null)
                    return;

                var dt = DateTime.UtcNow;
                if (dt.Hour == 23 && dt.Minute == 58)
                {
                    var sBuilder = new StringBuilder();
                    #region Đóng vị thế
                    var resPosition = await StaticVal.ByBitInstance().V5Api.Trading.GetPositionsAsync(Bybit.Net.Enums.Category.Linear);
                    if (resPosition?.Data?.List?.Any()?? false)
                    {
                        //close all
                        foreach (var position in resPosition.Data.List)
                        {
                            var res = await PlaceOrderClose(position.Symbol, Math.Abs(position.Quantity), Bybit.Net.Enums.PositionSide.Buy);
                            if (res)
                            {
                                var time = (int)new DateTimeOffset(dt.Year, dt.Month, dt.Day, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
                                var builder = Builders<TokenUnlockTrade>.Filter;
                                var first = _tokenUnlockTradeRepo.GetEntityByFilter(builder.And(
                                    builder.Eq(x => x.ex, _exchange),
                                    builder.Eq(x => x.timeUnlock, time)
                                ));
                                if (first is null)
                                {
                                    var mes = $"[UNLOCK][Đóng vị thế SHORT] {position.Symbol}|Giá đóng: {position.MarkPrice}";
                                    sBuilder.AppendLine(mes);
                                }
                                else
                                {
                                    first.rate = Math.Round(100 * (-1 + first.priceEntry / (double)position.MarkPrice.Value), 1);
                                    first.timeClose = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                                    _tokenUnlockTradeRepo.Update(first);
                                    var mes = $"[UNLOCK][Đóng vị thế SHORT] {position.Symbol}|Giá đóng: {position.MarkPrice}|Rate: {first.rate}%";
                                    sBuilder.AppendLine(mes);
                                }
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
                        var builder = Builders<TokenUnlockTrade>.Filter;
                        var entityCheck = _tokenUnlockTradeRepo.GetEntityByFilter(builder.And(
                                    builder.Eq(x => x.ex, _exchange),
                                    builder.Eq(x => x.timeUnlock, item.time),
                                    builder.Eq(x => x.s, $"{item.s}USDT")
                                )); 
                        if (entityCheck != null)
                            continue;

                        var lData15m = await _apiService.GetData(item.s, EInterval.M15);
                        var res = await PlaceOrder(new SignalBase
                        {
                            s = item.s,
                            ex = _exchange,
                            Side = (int)Bybit.Net.Enums.PositionSide.Sell,
                            timeFlag = (int)DateTimeOffset.Now.ToUnixTimeSeconds()
                        }, lData15m.Last());
                        if (res != null)
                        {
                            _tokenUnlockTradeRepo.InsertOne(new TokenUnlockTrade
                            {
                                s = res.s,
                                ex = res.ex,
                                timeUnlock = item.time,
                                timeShort = (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                                timeStoploss = res.timeStoploss,
                                priceEntry = res.priceEntry,
                                priceStoploss = res.priceStoploss
                            });

                            var mes = $"[UNLOCK][Mở vị thế SHORT] {res.s}|{((Bybit.Net.Enums.OrderSide)res.Side)}|Giá mở: {res.priceEntry}|SL: {res.priceStoploss}";
                            sBuilder.AppendLine(mes);
                        }
                    }
                    #endregion

                    if (sBuilder.Length > 0)
                    {
                        await _teleService.SendMessage(_idUser, sBuilder.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BybitService.Bybit_TradeTokenUnlock|EXCEPTION| {ex.Message}");
            }
        }

        public async Task Bybit_MarketAction()
        {
            try
            {
                var sBuilder = new StringBuilder();

                var resPosition = await StaticVal.ByBitInstance().V5Api.Trading.GetPositionsAsync(Bybit.Net.Enums.Category.Linear);
                if (!resPosition.Data.List.Any())
                    return;

                //Force Sell - Khi trong 1 khoảng thời gian ngắn có một loạt các lệnh thanh lý ngược chiều vị thế
                var timeForce = (int)DateTimeOffset.Now.AddMinutes(-15).ToUnixTimeSeconds();
                var lForce = _tradingRepo.GetByFilter(Builders<Trading>.Filter.Gte(x => x.d, timeForce));
                var countForceSell = lForce.Count(x => x.Side == (int)Bybit.Net.Enums.PositionSide.Sell);
                var countForceBuy = lForce.Count(x => x.Side == (int)Bybit.Net.Enums.PositionSide.Buy);
                if (countForceSell >= 5)
                {
                    var lSell = resPosition.Data.List.Where(x => x.Side == Bybit.Net.Enums.PositionSide.Buy);
                    var mesSell = await ForceMarket(lSell);
                    if (!string.IsNullOrWhiteSpace(mesSell))
                    {
                        sBuilder.AppendLine(mesSell);
                    }
                }
                if (countForceBuy >= 5)
                {
                    var lBuy = resPosition.Data.List.Where(x => x.Side == Bybit.Net.Enums.PositionSide.Sell);
                    var mesBuy = await ForceMarket(lBuy);
                    if (!string.IsNullOrWhiteSpace(mesBuy))
                    {
                        sBuilder.AppendLine(mesBuy);
                    }
                }

                //Signal
                var builderSignal = Builders<SignalTrade>.Filter;
                var lSignal = _signalTradeRepo.GetByFilter(builderSignal.And(
                                   builderSignal.Eq(x => x.ex, _exchange),
                                   builderSignal.Gte(x => x.timeFlag, (int)DateTimeOffset.Now.AddHours(3).ToUnixTimeSeconds()),
                                   builderSignal.Lte(x => x.timeFlag, (int)DateTimeOffset.Now.AddHours(2).ToUnixTimeSeconds()),
                                   builderSignal.Eq(x => x.status, 0)
                               ));
                if (lSignal?.Any() ?? false)
                {
                    var mes = await ForceMarket(resPosition.Data.List);
                    if (!string.IsNullOrWhiteSpace(mes))
                    {
                        sBuilder.AppendLine(mes);
                    }
                }
                //Three
                var builderThree = Builders<ThreeSignalTrade>.Filter;
                var lThree = _threeSignalTradeRepo.GetByFilter(builderThree.And(
                                   builderThree.Eq(x => x.ex, _exchange),
                                   builderThree.Gte(x => x.timeFlag, (int)DateTimeOffset.Now.AddHours(3).ToUnixTimeSeconds()),
                                   builderThree.Lte(x => x.timeFlag, (int)DateTimeOffset.Now.AddHours(2).ToUnixTimeSeconds()),
                                   builderThree.Eq(x => x.status, 0)
                               ));
                if (lThree?.Any() ?? false)
                {
                    var mes = await ForceMarket(resPosition.Data.List);
                    if (!string.IsNullOrWhiteSpace(mes))
                    {
                        sBuilder.AppendLine(mes);
                    }
                }

                if (sBuilder.Length > 0)
                {
                    await _teleService.SendMessage(_idUser, sBuilder.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BybitService.Bybit_MarketAction|EXCEPTION| {ex.Message}");
            }
        }

        private async Task<string> ForceMarket(IEnumerable<BybitPosition> lData)
        {
            var sBuilder = new StringBuilder();
            try
            {
                foreach (var item in lData)
                {
                    var side = (item.Side == Bybit.Net.Enums.PositionSide.Buy) ? Bybit.Net.Enums.PositionSide.Sell : Bybit.Net.Enums.PositionSide.Buy;
                    var res = await PlaceOrderClose(item.Symbol, item.Quantity, side);
                    if (!res)
                        continue;

                    var mes = $"[Đóng vị thế {item.Side}] {item.Symbol}|Giá đóng: {item.MarkPrice}";
                    sBuilder.AppendLine(mes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BybitService.ForceMarket|EXCEPTION| {ex.Message}");
            }

            return sBuilder.ToString();
        }

        private async Task<SignalBase> PlaceOrder(SignalBase entity, Quote quote)
        {
            try
            {
                var SL_RATE = 0.017;
                var curTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                var account = await Bybit_GetAccountInfo();
                if (account == null)
                {
                    await _teleService.SendMessage(_idUser, "[ERROR] Không lấy được thông tin tài khoản");
                    return null;
                }

                if (account.WalletBalance * _margin <= _unit)
                    return null;


                var side = (Bybit.Net.Enums.OrderSide)entity.Side;
                var SL_side = side == Bybit.Net.Enums.OrderSide.Buy ? Bybit.Net.Enums.OrderSide.Sell : Bybit.Net.Enums.OrderSide.Buy;

                var near = 2; 
                if (quote.Close < 1)
                {
                    near = 0;
                }
                var soluong = Math.Round(_unit / quote.Close, near);
                var res = await StaticVal.ByBitInstance().V5Api.Trading.PlaceOrderAsync(Bybit.Net.Enums.Category.Linear,
                                                                                        entity.s,
                                                                                        side: side,
                                                                                        type: Bybit.Net.Enums.NewOrderType.Market,
                                                                                        reduceOnly: false,
                                                                                        quantity: soluong);
                Thread.Sleep(500);
                //nếu lỗi return
                if (!res.Success)
                {
                    _errRepo.InsertOne(new ErrorPartner
                    {
                        s = entity.s,
                        time = curTime,
                        ty = (int)ETypeBot.TokenUnlock,
                        action = (int)EAction.Short,
                        des = $"side: {side}, type: {Bybit.Net.Enums.NewOrderType.Market}, quantity: {soluong}"
                    });
                    return null;
                }

                var resPosition = await StaticVal.ByBitInstance().V5Api.Trading.GetPositionsAsync(Bybit.Net.Enums.Category.Linear, entity.s);
                Thread.Sleep(500);
                if (!resPosition.Success)
                {
                    _errRepo.InsertOne(new ErrorPartner
                    {
                        s = entity.s,
                        time = curTime,
                        ty = (int)ETypeBot.TokenUnlock,
                        action = (int)EAction.GetPosition
                    });

                    return entity;
                }

                if (resPosition.Data.List.Any())
                {
                    var first = resPosition.Data.List.First();
                    entity.priceEntry = (double)first.MarkPrice;

                    if (quote.Close < 1)
                    {
                        var price = quote.Close.ToString().Split('.').Last();
                        price = price.ReverseString();
                        near = long.Parse(price).ToString().Length;
                    }
                    var checkLenght = quote.Close.ToString().Split('.').Last();
                    decimal sl = 0;
                    if (side == Bybit.Net.Enums.OrderSide.Buy)
                    {
                        sl = Math.Round(first.MarkPrice.Value * (decimal)(1 - SL_RATE), near);
                    }
                    else
                    {
                        sl = Math.Round(first.MarkPrice.Value * (decimal)(1 + SL_RATE), near);
                    }

                    res = await StaticVal.ByBitInstance().V5Api.Trading.PlaceOrderAsync(Bybit.Net.Enums.Category.Linear,
                                                                                            first.Symbol,
                                                                                            side: SL_side,
                                                                                            type: Bybit.Net.Enums.NewOrderType.Market,
                                                                                            quantity: soluong,
                                                                                            timeInForce: Bybit.Net.Enums.TimeInForce.GoodTillCanceled,
                                                                                            reduceOnly: true,
                                                                                            stopLossLimitPrice: sl);
                    Thread.Sleep(500);
                    if (!res.Success)
                    {
                        _errRepo.InsertOne(new ErrorPartner
                        {
                            s = entity.s,
                            time = curTime,
                            ty = (int)ETypeBot.TokenUnlock,
                            action = (int)EAction.Short_SL,
                            des = $"side: {SL_side}, type: {Bybit.Net.Enums.NewOrderType.Market}, quantity: {soluong}, stopPrice: {sl}"
                        });

                        return null;
                    }

                    entity.timeStoploss = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                    entity.priceStoploss = (double)sl;
                }
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BybitService.PlaceOrder|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private async Task<bool> PlaceOrderClose(string symbol, decimal quan, Bybit.Net.Enums.PositionSide side)
        {
            try
            {
                var res = await StaticVal.ByBitInstance().V5Api.Trading.PlaceOrderAsync(Bybit.Net.Enums.Category.Linear,
                                                                                        symbol,
                                                                                        side: (Bybit.Net.Enums.OrderSide)((int)side),
                                                                                        type: Bybit.Net.Enums.NewOrderType.Market,
                                                                                        quantity: quan);
                if (res.Success)
                    return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BybitService.PlaceOrderCloseAll|EXCEPTION| {ex.Message}");
            }

            await _teleService.SendMessage(_idUser, $"[ERROR] Không thể đóng lệnh {side}: {symbol}!");
            return false;
        }
    }
}
