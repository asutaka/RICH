using Binance.Net.Objects.Models.Futures;
using MongoDB.Driver;
using Skender.Stock.Indicators;
using System.Text;
using TradePr.DAL.Entity;
using TradePr.DAL;
using TradePr.Utils;
using Newtonsoft.Json;

namespace TradePr.Service
{
    public interface IBinanceService
    {
        Task<BinanceUsdFuturesAccountBalance> Binance_GetAccountInfo();
        Task Binance_Trade();
    }
    public class BinanceService : IBinanceService
    {
        private readonly ILogger<BinanceService> _logger;
        private readonly ITradingRepo _tradingRepo;
        private readonly ISignalTradeRepo _signalTradeRepo;
        private readonly IThreeSignalTradeRepo _threeSignalTradeRepo;
        private readonly ITokenUnlockTradeRepo _tokenUnlockTradeRepo;
        private readonly ITokenUnlockRepo _tokenUnlockRepo;
        private readonly IErrorPartnerRepo _errRepo;
        private readonly IConfigDataRepo _configRepo;
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private const long _idUser = 1066022551;
        private const decimal _unit = 50;
        private const decimal _margin = 10;
        private readonly int _exchange = (int)EExchange.Binance;
        public BinanceService(ILogger<BinanceService> logger, ITradingRepo tradingRepo, ISignalTradeRepo signalTradeRepo, IErrorPartnerRepo errRepo,
                            IConfigDataRepo configRepo, IThreeSignalTradeRepo threeSignalTradeRepo, ITokenUnlockTradeRepo tokenUnlockTradeRepo, ITokenUnlockRepo tokenUnlockRepo,
                            IAPIService apiService, ITeleService teleService)
        {
            _logger = logger;
            _tradingRepo = tradingRepo;
            _signalTradeRepo = signalTradeRepo;
            _errRepo = errRepo;
            _configRepo = configRepo;
            _threeSignalTradeRepo = threeSignalTradeRepo;
            _tokenUnlockTradeRepo = tokenUnlockTradeRepo;
            _tokenUnlockRepo = tokenUnlockRepo;
            _apiService = apiService;
            _teleService = teleService;
        }
        public async Task<BinanceUsdFuturesAccountBalance> Binance_GetAccountInfo()
        {
            try
            {
                var resAPI = await StaticVal.BinanceInstance().UsdFuturesApi.Account.GetBalancesAsync();
                return resAPI?.Data?.FirstOrDefault(x => x.Asset == "USDT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.Binance_GetAccountInfo|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task Binance_Trade()
        {
            try
            {
                var builder = Builders<ConfigData>.Filter;
                var lSignal = _configRepo.GetByFilter(builder.And(
                    builder.Eq(x => x.ex, _exchange),
                    builder.Eq(x => x.status, 1)
                ));

                if (lSignal.Any(x => x.op == (int)EOption.Signal))
                {
                    await Binance_TradeSignal();
                }
                if (lSignal.Any(x => x.op == (int)EOption.ThreeSignal))
                {
                    await Binance_TradeThreeSignal();
                }
                if (lSignal.Any(x => x.op == (int)EOption.Unlock))
                {
                    await Binance_TradeTokenUnlock();
                }
                await Binance_MarketAction();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.Binance_Trade|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Binance_TradeSignal()
        {
            try
            {
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

                    //Trong 1 tiếng tồn tại ít nhất 2 tín hiệu
                    var lTradeSym = lTrade.Where(x => x.s == item).OrderByDescending(x => x.d);
                    if (lTradeSym.Count() < 2)
                        continue;

                    //2 tín hiệu cùng chiều
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

                    if (res != null)
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
                        var mes = $"[SIGNAL][Mở vị thế {((Binance.Net.Enums.OrderSide)res.Side).ToString().ToUpper()}] {res.s}|Giá mở: {res.priceEntry}|SL: {res.priceStoploss}";
                        await _teleService.SendMessage(_idUser, mes);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.Binance_TradeSignal|EXCEPTION| {ex.Message}");
            }
            return;
        }

        private async Task Binance_TradeThreeSignal()
        {
            try
            {
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
                            var mes = $"[THREE][Mở vị thế {((Binance.Net.Enums.OrderSide)res.Side).ToString().ToUpper()}] {res.s}|Giá mở: {res.priceEntry}|SL: {res.priceStoploss}";
                            await _teleService.SendMessage(_idUser, mes);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"BinanceService.TradeThreeSignal|EXCEPTION|INPUT: {JsonConvert.SerializeObject(item)}| {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.Binance_TradeThreeSignal|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Binance_TradeTokenUnlock()
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
                            var time = (int)new DateTimeOffset(dt.Year, dt.Month, dt.Day, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
                            var builder = Builders<TokenUnlockTrade>.Filter;
                            var first = _tokenUnlockTradeRepo.GetEntityByFilter(builder.And(
                                builder.Eq(x => x.ex, _exchange),
                                builder.Eq(x => x.s, position.Symbol),
                                builder.Eq(x => x.timeUnlock, time)
                            ));

                            if (first is null)
                                continue;

                            var res = await PlaceOrderClose(position.Symbol, Math.Abs(position.PositionAmt), Binance.Net.Enums.OrderSide.Buy);
                            if (res)
                            {
                                first.rate = Math.Round(100 * (-1 + first.priceEntry / (double)position.MarkPrice), 1);
                                first.timeClose = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                                _tokenUnlockTradeRepo.Update(first);
                                var mes = $"[UNLOCK][Đóng vị thế SHORT] {position.Symbol}|Giá đóng: {position.MarkPrice}|Rate: {first.rate}%";
                                sBuilder.AppendLine(mes);
                            }
                        }
                    }
                    #endregion

                    #region Mở vị thế
                    var timeUnlock = (int)new DateTimeOffset(dt.Year, dt.Month, dt.Day, 0, 0, 0, TimeSpan.Zero).AddDays(1).ToUnixTimeSeconds();
                    var tokens = _tokenUnlockRepo.GetByFilter(Builders<TokenUnlock>.Filter.Eq(x => x.time, timeUnlock)).Where(x => !StaticVal._lTokenUnlockBlackList.Contains(x.s));
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

                        var lData15m = await _apiService.GetData($"{item.s}USDT", EInterval.M15);
                        if (!lData15m.Any())
                            continue;

                        var res = await PlaceOrder(new SignalBase
                        {
                            s = $"{item.s}USDT",
                            ex = _exchange,
                            Side = (int)Binance.Net.Enums.OrderSide.Sell,
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

                            var mes = $"[UNLOCK][Mở vị thế SHORT] {res.s}|{((Binance.Net.Enums.OrderSide)res.Side)}|Giá mở: {res.priceEntry}|SL: {res.priceStoploss}";
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
                _logger.LogError(ex, $"BinanceService.Binance_TradeTokenUnlock|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Binance_MarketAction()
        {
            try
            {
                var sBuilder = new StringBuilder();

                var resPosition = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.GetPositionsAsync();
                if (!resPosition.Data.Any())
                    return;

                var lRes = new List<BinancePositionV3>();
                //Force Sell - Khi trong 1 khoảng thời gian ngắn có một loạt các lệnh thanh lý ngược chiều vị thế
                var timeForce = (int)DateTimeOffset.Now.AddMinutes(-15).ToUnixTimeSeconds();
                var lForce = _tradingRepo.GetByFilter(Builders<Trading>.Filter.Gte(x => x.d, timeForce));
                var countForceSell = lForce.Count(x => x.Side == (int)Binance.Net.Enums.OrderSide.Sell);
                var countForceBuy = lForce.Count(x => x.Side == (int)Binance.Net.Enums.OrderSide.Buy);
                if (countForceSell >= 5)
                {
                    var lSell = resPosition.Data.Where(x => x.PositionSide == Binance.Net.Enums.PositionSide.Long);
                    lRes = await ForceMarket(lSell);
                }
                if (countForceBuy >= 5)
                {
                    var lBuy = resPosition.Data.Where(x => x.PositionSide == Binance.Net.Enums.PositionSide.Short);
                    lRes = await ForceMarket(lBuy);
                }

                //Signal
                var builderSignal = Builders<SignalTrade>.Filter;
                var lSignal = _signalTradeRepo.GetByFilter(builderSignal.And(
                                   builderSignal.Eq(x => x.ex, _exchange),
                                   builderSignal.Gte(x => x.timeFlag, (int)DateTimeOffset.Now.AddHours(3).ToUnixTimeSeconds()),
                                   builderSignal.Lte(x => x.timeFlag, (int)DateTimeOffset.Now.AddHours(2).ToUnixTimeSeconds()),
                                   builderSignal.Eq(x => x.status, 0)
                               ));
                //Three
                var builderThree = Builders<ThreeSignalTrade>.Filter;
                var lThree = _threeSignalTradeRepo.GetByFilter(builderThree.And(
                                   builderThree.Eq(x => x.ex, _exchange),
                                   builderThree.Gte(x => x.timeFlag, (int)DateTimeOffset.Now.AddHours(3).ToUnixTimeSeconds()),
                                   builderThree.Lte(x => x.timeFlag, (int)DateTimeOffset.Now.AddHours(2).ToUnixTimeSeconds()),
                                   builderThree.Eq(x => x.status, 0)
                               ));
                //Unlock 
                var dt = DateTime.UtcNow;
                var timeUnlock = (int)new DateTimeOffset(dt.Year, dt.Month, dt.Day, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
                var builderUnlock = Builders<TokenUnlockTrade>.Filter;
                var lUnlock = _tokenUnlockTradeRepo.GetByFilter(builderUnlock.And(
                            builderUnlock.Eq(x => x.ex, _exchange),
                            builderUnlock.Eq(x => x.timeUnlock, timeUnlock)
                        ));
                if (lRes.Any())
                {
                    foreach (var item in lRes)
                    {
                        var priceClose = (double)item.MarkPrice;
                        var signal = lSignal.FirstOrDefault(x => x.s == item.Symbol);
                        if (signal != null)
                        {
                            var rate = Math.Abs(Math.Round(100 * (-1 + priceClose / signal.priceEntry), 1));
                            if (item.PositionSide == Binance.Net.Enums.PositionSide.Long)
                            {
                                if (priceClose < signal.priceEntry)
                                    rate = -rate;
                            }
                            else
                            {
                                if (priceClose > signal.priceEntry)
                                    rate = -rate;
                            }

                            signal.priceClose = priceClose;
                            signal.timeClose = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                            signal.rate = rate;
                            _signalTradeRepo.Update(signal);

                            var mes = $"[Signal][Đóng vị thế {item.PositionSide}] {item.Symbol}|Giá đóng: {item.MarkPrice}|Rate: {rate}";
                            sBuilder.AppendLine(mes);
                            continue;
                        }

                        var three = lThree.FirstOrDefault(x => x.s == item.Symbol);
                        if (three != null)
                        {
                            var rate = Math.Abs(Math.Round(100 * (-1 + priceClose / three.priceEntry), 1));
                            if (item.PositionSide == Binance.Net.Enums.PositionSide.Long)
                            {
                                if (priceClose < three.priceEntry)
                                    rate = -rate;
                            }
                            else
                            {
                                if (priceClose > three.priceEntry)
                                    rate = -rate;
                            }

                            three.priceClose = priceClose;
                            three.timeClose = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                            three.rate = rate;
                            _threeSignalTradeRepo.Update(three);

                            var mes = $"[Three][Đóng vị thế {item.PositionSide}] {item.Symbol}|Giá đóng: {item.MarkPrice}|Rate: {rate}";
                            sBuilder.AppendLine(mes);
                            continue;
                        }

                        var unlock = lUnlock.FirstOrDefault(x => x.s == item.Symbol);
                        if (unlock != null)
                        {
                            var rate = Math.Abs(Math.Round(100 * (-1 + priceClose / unlock.priceEntry), 1));
                            if (item.PositionSide == Binance.Net.Enums.PositionSide.Long)
                            {
                                if (priceClose < unlock.priceEntry)
                                    rate = -rate;
                            }
                            else
                            {
                                if (priceClose > unlock.priceEntry)
                                    rate = -rate;
                            }

                            unlock.priceClose = priceClose;
                            unlock.timeClose = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                            unlock.rate = rate;
                            _tokenUnlockTradeRepo.Update(unlock);

                            var mes = $"[Three][Đóng vị thế {item.PositionSide}] {item.Symbol}|Giá đóng: {item.MarkPrice}|Rate: {rate}";
                            sBuilder.AppendLine(mes);
                            continue;
                        }

                        var mesOther = $"[Three][Đóng vị thế {item.PositionSide}] {item.Symbol}|Giá đóng: {item.MarkPrice}";
                        sBuilder.AppendLine(mesOther);
                    }

                    if (sBuilder.Length > 0)
                    {
                        await _teleService.SendMessage(_idUser, sBuilder.ToString());
                    }
                    return;
                }

                //Signal
                if (lSignal?.Any() ?? false)
                {
                    lRes = await ForceMarket(resPosition.Data.Where(x => lSignal.Any(y => y.s == x.Symbol)));
                    foreach (var item in lRes)
                    {
                        var priceClose = (double)item.MarkPrice;
                        var first = lSignal.First(x => x.s == item.Symbol);
                        var rate = Math.Abs(Math.Round(100 * (-1 + priceClose / first.priceEntry), 1));
                        if (item.PositionSide == Binance.Net.Enums.PositionSide.Long)
                        {
                            if (priceClose < first.priceEntry)
                                rate = -rate;
                        }
                        else
                        {
                            if (priceClose > first.priceEntry)
                                rate = -rate;
                        }

                        first.priceClose = priceClose;
                        first.timeClose = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                        first.rate = rate;
                        _signalTradeRepo.Update(first);

                        var mes = $"[Signal][Đóng vị thế {item.PositionSide}] {item.Symbol}|Giá đóng: {item.MarkPrice}|Rate: {rate}";
                        sBuilder.AppendLine(mes);
                    }
                }
                //Three
                if (lThree?.Any() ?? false)
                {
                    lRes = await ForceMarket(resPosition.Data.Where(x => lThree.Any(y => y.s == x.Symbol)));
                    foreach (var item in lRes)
                    {
                        var priceClose = (double)item.MarkPrice;
                        var first = lThree.First(x => x.s == item.Symbol);
                        var rate = Math.Abs(Math.Round(100 * (-1 + priceClose / first.priceEntry), 1));
                        if (item.PositionSide == Binance.Net.Enums.PositionSide.Long)
                        {
                            if (priceClose < first.priceEntry)
                                rate = -rate;
                        }
                        else
                        {
                            if (priceClose > first.priceEntry)
                                rate = -rate;
                        }

                        first.priceClose = priceClose;
                        first.timeClose = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                        first.rate = rate;
                        _threeSignalTradeRepo.Update(first);

                        var mes = $"[Three][Đóng vị thế {item.PositionSide}] {item.Symbol}|Giá đóng: {item.MarkPrice}|Rate: {rate}";
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
                _logger.LogError(ex, $"BinanceService.Binance_MarketAction|EXCEPTION| {ex.Message}");
            }
        }

        private async Task<List<BinancePositionV3>> ForceMarket(IEnumerable<BinancePositionV3> lData)
        {
            var lRes = new List<BinancePositionV3>();
            try
            {
                foreach (var item in lData)
                {
                    var side = (item.PositionSide == Binance.Net.Enums.PositionSide.Long) ? Binance.Net.Enums.OrderSide.Buy : Binance.Net.Enums.OrderSide.Sell;
                    var res = await PlaceOrderClose(item.Symbol, Math.Abs(item.PositionAmt), side);
                    if (!res)
                        continue;

                    lRes.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.ForceMarket|EXCEPTION| {ex.Message}");
            }

            return lRes;
        }

        private async Task<SignalBase> PlaceOrder(SignalBase entity, Quote quote)
        {
            try
            {
                var SL_RATE = 0.017;
                var curTime = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                var account = await Binance_GetAccountInfo();
                if (account == null)
                {
                    await _teleService.SendMessage(_idUser, "[ERROR] Không lấy được thông tin tài khoản");
                    return null;
                }

                if (account.WalletBalance * _margin <= _unit)
                    return null;


                var side = (Binance.Net.Enums.OrderSide)entity.Side;
                var SL_side = side == Binance.Net.Enums.OrderSide.Buy ? Binance.Net.Enums.OrderSide.Sell : Binance.Net.Enums.OrderSide.Buy;

                var near = 2;
                if (quote.Close < 5)
                {
                    near = 0;
                }
                var soluong = Math.Round(_unit / quote.Close, near);
                var res = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.PlaceOrderAsync(entity.s,
                                                                                                    side: side,
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
                        s = entity.s,
                        time = curTime,
                        ty = (int)ETypeBot.TokenUnlock,
                        action = (int)EAction.Short,
                        des = $"side: {side}, type: {Binance.Net.Enums.FuturesOrderType.Market}, quantity: {soluong}"
                    });
                    return null;
                }

                var resPosition = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.GetPositionsAsync(entity.s);
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

                if (resPosition.Data.Any())
                {
                    var first = resPosition.Data.First();
                    entity.priceEntry = (double)first.MarkPrice;

                    if (quote.Close < 5)
                    {
                        var price = quote.Close.ToString().Split('.').Last();
                        price = price.ReverseString();
                        near = long.Parse(price).ToString().Length;
                    }
                    var checkLenght = quote.Close.ToString().Split('.').Last();
                    decimal sl = 0;
                    if (side == Binance.Net.Enums.OrderSide.Buy)
                    {
                        sl = Math.Round(first.MarkPrice * (decimal)(1 - SL_RATE), near);
                    }
                    else
                    {
                        sl = Math.Round(first.MarkPrice * (decimal)(1 + SL_RATE), near);
                    }

                    res = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.PlaceOrderAsync(first.Symbol,
                                                                                                    side: SL_side,
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
                        _errRepo.InsertOne(new ErrorPartner
                        {
                            s = entity.s,
                            time = curTime,
                            ty = (int)ETypeBot.TokenUnlock,
                            action = (int)EAction.Short_SL,
                            des = $"side: {SL_side}, type: {Binance.Net.Enums.FuturesOrderType.Market}, quantity: {soluong}, stopPrice: {sl}"
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
                _logger.LogError(ex, $"BinanceService.PlaceOrder|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private async Task<bool> PlaceOrderClose(string symbol, decimal quan, Binance.Net.Enums.OrderSide side)
        {
            try
            {
                var res = await StaticVal.BinanceInstance().UsdFuturesApi.Trading.PlaceOrderAsync(symbol,
                                                                                                    side: (Binance.Net.Enums.OrderSide)((int)side),
                                                                                                    type: Binance.Net.Enums.FuturesOrderType.Market,
                                                                                                    positionSide: Binance.Net.Enums.PositionSide.Both,
                                                                                                    reduceOnly: false,
                                                                                                    quantity: quan);
                if (res.Success)
                    return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BinanceService.PlaceOrderClose|EXCEPTION| {ex.Message}");
            }

            await _teleService.SendMessage(_idUser, $"[ERROR] Không thể đóng lệnh {side}: {symbol}!");
            return false;
        }
    }
}
