using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.Text;
using TradePr.DAL;
using TradePr.DAL.Entity;
using TradePr.Utils;

namespace TradePr.Service
{
    public interface IBybitService
    {
        Task<BybitAssetBalance> Bybit_GetAccountInfo();
        Task Bybit_Trade();
    }
    public class BybitService : IBybitService
    {
        private readonly ILogger<BybitService> _logger;
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
        private readonly int _exchange = (int)EExchange.Bybit;
        private readonly int _forceSell = 4;
        public BybitService(ILogger<BybitService> logger, ITradingRepo tradingRepo, ISignalTradeRepo signalTradeRepo, IErrorPartnerRepo errRepo,
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
        public async Task<BybitAssetBalance> Bybit_GetAccountInfo()
        {
            try
            {
                var resAPI = await StaticVal.ByBitInstance().V5Api.Account.GetBalancesAsync( AccountType.Unified);
                return resAPI?.Data?.List?.FirstOrDefault().Assets.FirstOrDefault(x => x.Asset == "USDT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_GetAccountInfo|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public async Task Bybit_Trade()
        {
            try
            {
                var builder = Builders<ConfigData>.Filter;
                var lSignal = _configRepo.GetByFilter(builder.And(
                    builder.Eq(x => x.ex, _exchange),
                    builder.Eq(x => x.status, 1)
                ));

                if(lSignal.Any(x => x.op == (int)EOption.Signal))
                {
                    await Bybit_TradeSignal();
                }
                if (lSignal.Any(x => x.op == (int)EOption.ThreeSignal))
                {
                    await Bybit_TradeThreeSignal();
                }
                if (lSignal.Any(x => x.op == (int)EOption.Unlock))
                {
                    await Bybit_TradeTokenUnlock();
                }
                await Bybit_MarketAction();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_Trade|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Bybit_TradeSignal()
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
                    var lData15m = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetMarkPriceKlinesAsync(Category.Linear, $"{item}USDT", KlineInterval.ThirtyMinutes);
                    if (lData15m.Data is null
                        || !lData15m.Data.List.Any())
                        continue;

                    var last = lData15m.Data.List.Last();
                    var itemCheck = lData15m.Data.List.SkipLast(1).Last();
                    if (itemCheck.OpenPrice >= itemCheck.ClosePrice)
                        continue;

                    //Trade
                    var res = await PlaceOrder(new SignalBase
                    {
                        s = item,
                        ex = _exchange,
                        Side = first.Side,
                        timeFlag = (int)DateTimeOffset.Now.ToUnixTimeSeconds()
                    }, last);

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
                        var mes = $"[SIGNAL][Mở vị thế {((OrderSide)res.Side).ToString().ToUpper()}] {res.s}|Giá mở: {res.priceEntry}|SL: {res.priceStoploss}";
                        await _teleService.SendMessage(_idUser, mes);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TradeSignal|EXCEPTION| {ex.Message}");
            }
            return;
        }

        private async Task Bybit_TradeThreeSignal()
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

                        var lData15m = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetMarkPriceKlinesAsync(Category.Linear, $"{item.s}USDT", KlineInterval.ThirtyMinutes);
                        if (lData15m.Data is null
                            || !lData15m.Data.List.Any())
                            continue;

                        var last = lData15m.Data.List.Last();
                        var res = await PlaceOrder(new SignalBase
                        {
                            s = item.s,
                            ex = _exchange,
                            Side = item.Side,
                            timeFlag = (int)DateTimeOffset.Now.ToUnixTimeSeconds()
                        }, last);
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
                            var mes = $"[THREE][Mở vị thế {((OrderSide)res.Side).ToString().ToUpper()}] {res.s}|Giá mở: {res.priceEntry}|SL: {res.priceStoploss}";
                            await _teleService.SendMessage(_idUser, mes);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.TradeThreeSignal|EXCEPTION|INPUT: {JsonConvert.SerializeObject(item)}| {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TradeThreeSignal|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Bybit_TradeTokenUnlock()
        {
            try
            {
                var dt = DateTime.UtcNow;
                //if (true)
                if (dt.Hour == 23 && dt.Minute == 58)
                {
                    var sBuilder = new StringBuilder();
                    #region Đóng vị thế
                    var resPosition = await StaticVal.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, settleAsset: "USDT");
                    if (resPosition?.Data?.List?.Any()?? false)
                    {
                        //close all
                        foreach (var position in resPosition.Data.List)
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

                            var res = await PlaceOrderClose(position.Symbol, Math.Abs(position.Quantity), PositionSide.Buy);
                            if (res)
                            {
                                first.rate = Math.Round(100 * (-1 + first.priceEntry / (double)position.MarkPrice.Value), 1);
                                first.timeClose = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                                _tokenUnlockTradeRepo.Update(first);
                                var mes = $"[UNLOCK][Đóng vị thế SELL] {position.Symbol}|Giá đóng: {position.MarkPrice}|Rate: {first.rate}%";
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

                        var lData15m = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetMarkPriceKlinesAsync(Category.Linear, $"{item.s}USDT", KlineInterval.ThirtyMinutes);
                        if (lData15m.Data is null
                            || !lData15m.Data.List.Any())
                            continue;

                        var last = lData15m.Data.List.Last();
                        var res = await PlaceOrder(new SignalBase
                        {
                            s = $"{item.s}USDT",
                            ex = _exchange,
                            Side = (int)PositionSide.Sell,
                            timeFlag = (int)DateTimeOffset.Now.ToUnixTimeSeconds()
                        }, last);
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

                            var mes = $"[UNLOCK][Mở vị thế SHORT] {res.s}|{((OrderSide)res.Side)}|Giá mở: {res.priceEntry}|SL: {res.priceStoploss}";
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
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TradeTokenUnlock|EXCEPTION| {ex.Message}");
            }
        }

        private async Task Bybit_MarketAction()
        {
            try
            {
                var sBuilder = new StringBuilder();

                var resPosition = await StaticVal.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, settleAsset: "USDT");
                if (!resPosition.Data.List.Any())
                    return;

                var lRes = new List<BybitPosition>();
                //Force Sell - Khi trong 1 khoảng thời gian ngắn có một loạt các lệnh thanh lý ngược chiều vị thế
                var timeForce = (int)DateTimeOffset.Now.AddMinutes(-15).ToUnixTimeSeconds();
                var lForce = _tradingRepo.GetByFilter(Builders<Trading>.Filter.Gte(x => x.d, timeForce));
                var countForceSell = lForce.Count(x => x.Side == (int)OrderSide.Sell);
                var countForceBuy = lForce.Count(x => x.Side == (int)OrderSide.Buy);
                if (countForceSell >= _forceSell)
                {
                    var lSell = resPosition.Data.List.Where(x => x.Side == PositionSide.Buy);
                    lRes = await ForceMarket(lSell);
                }
                if (countForceBuy >= _forceSell)
                {
                    var lBuy = resPosition.Data.List.Where(x => x.Side == PositionSide.Sell);
                    lRes = await ForceMarket(lBuy);
                }

                //Signal
                var builderSignal = Builders<SignalTrade>.Filter;
                var lSignal = _signalTradeRepo.GetByFilter(builderSignal.And(
                                   builderSignal.Eq(x => x.ex, _exchange),
                                   //builderSignal.Gte(x => x.timeFlag, (int)DateTimeOffset.Now.AddHours(-3).ToUnixTimeSeconds()),
                                   builderSignal.Lte(x => x.timeFlag, (int)DateTimeOffset.Now.AddHours(-2).ToUnixTimeSeconds()),
                                   builderSignal.Eq(x => x.status, 0)
                               ));
                //Three
                var builderThree = Builders<ThreeSignalTrade>.Filter;
                var lThree = _threeSignalTradeRepo.GetByFilter(builderThree.And(
                                   builderThree.Eq(x => x.ex, _exchange),
                                   //builderThree.Gte(x => x.timeFlag, (int)DateTimeOffset.Now.AddHours(-3).ToUnixTimeSeconds()),
                                   builderThree.Lte(x => x.timeFlag, (int)DateTimeOffset.Now.AddHours(-2).ToUnixTimeSeconds()),
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
                        var priceClose = (double)item.MarkPrice.Value;
                        var signal = lSignal.FirstOrDefault(x => x.s == item.Symbol);
                        if(signal != null)
                        {
                            var rate = Math.Abs(Math.Round(100 * (-1 + priceClose / signal.priceEntry), 1));
                            if (item.Side == PositionSide.Buy)
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

                            var mes = $"[Signal][Đóng vị thế {item.Side}] {item.Symbol}|Giá đóng: {item.MarkPrice}|Rate: {rate}";
                            sBuilder.AppendLine(mes);
                            continue;
                        }

                        var three = lThree.FirstOrDefault(x => x.s == item.Symbol);
                        if (three != null)
                        {
                            var rate = Math.Abs(Math.Round(100 * (-1 + priceClose / three.priceEntry), 1));
                            if (item.Side == PositionSide.Buy)
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

                            var mes = $"[Three][Đóng vị thế {item.Side}] {item.Symbol}|Giá đóng: {item.MarkPrice}|Rate: {rate}";
                            sBuilder.AppendLine(mes);
                            continue;
                        }

                        var unlock = lUnlock.FirstOrDefault(x => x.s == item.Symbol);
                        if (unlock != null)
                        {
                            var rate = Math.Abs(Math.Round(100 * (-1 + priceClose / unlock.priceEntry), 1));
                            if (item.Side == PositionSide.Buy)
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

                            var mes = $"[Three][Đóng vị thế {item.Side}] {item.Symbol}|Giá đóng: {item.MarkPrice}|Rate: {rate}";
                            sBuilder.AppendLine(mes);
                            continue;
                        }

                        var mesOther = $"[Three][Đóng vị thế {item.Side}] {item.Symbol}|Giá đóng: {item.MarkPrice}";
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
                    lRes = await ForceMarket(resPosition.Data.List.Where(x => lSignal.Any(y => y.s == x.Symbol)));
                    foreach (var item in lRes)
                    {
                        var priceClose = (double)item.MarkPrice.Value;
                        var first = lSignal.First(x => x.s == item.Symbol);
                        var rate = Math.Abs(Math.Round(100 * (-1 + priceClose / first.priceEntry), 1));
                        if(item.Side == PositionSide.Buy)
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

                        var mes = $"[Signal][Đóng vị thế {item.Side}] {item.Symbol}|Giá đóng: {item.MarkPrice}|Rate: {rate}";
                        sBuilder.AppendLine(mes);
                    }
                }
                //Three
                if (lThree?.Any() ?? false)
                {
                    lRes = await ForceMarket(resPosition.Data.List.Where(x => lThree.Any(y => y.s == x.Symbol)));
                    foreach (var item in lRes)
                    {
                        var priceClose = (double)item.MarkPrice.Value;
                        var first = lThree.First(x => x.s == item.Symbol);
                        var rate = Math.Abs(Math.Round(100 * (-1 + priceClose / first.priceEntry), 1));
                        if (item.Side == PositionSide.Buy)
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

                        var mes = $"[Three][Đóng vị thế {item.Side}] {item.Symbol}|Giá đóng: {item.MarkPrice}|Rate: {rate}";
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
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_MarketAction|EXCEPTION| {ex.Message}");
            }
        }

        private async Task<List<BybitPosition>> ForceMarket(IEnumerable<BybitPosition> lData)
        {
            var lRes = new List<BybitPosition>();
            try
            {
                foreach (var item in lData)
                {
                    var side = (item.Side == PositionSide.Buy) ? PositionSide.Sell : PositionSide.Buy;
                    var res = await PlaceOrderClose(item.Symbol, item.Quantity, side);
                    if (!res)
                        continue;

                    lRes.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.ForceMarket|EXCEPTION| {ex.Message}");
            }

            return lRes;
        }

        private async Task<SignalBase> PlaceOrder(SignalBase entity, BybitBasicKline quote)
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


                var side = (OrderSide)entity.Side;
                var SL_side = side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy;
                var direction = side == OrderSide.Buy ? TriggerDirection.Fall : TriggerDirection.Rise;

                var near = 2; 
                if (quote.ClosePrice < 5)
                {
                    near = 0;
                }
                var soluong = Math.Round(_unit / quote.ClosePrice, near);
                var res = await StaticVal.ByBitInstance().V5Api.Trading.PlaceOrderAsync(Category.Linear,
                                                                                        entity.s,
                                                                                        side: side,
                                                                                        type: NewOrderType.Market,
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
                        des = $"side: {side}, type: {NewOrderType.Market}, quantity: {soluong}"
                    });
                    return null;
                }

                var resPosition = await StaticVal.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, entity.s);
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
                    await _teleService.SendMessage(_idUser, $"[ERROR] |{entity.s}|{res.Error.Message}");
                    return entity;
                }

                if (resPosition.Data.List.Any())
                {
                    var first = resPosition.Data.List.First();
                    entity.priceEntry = (double)first.MarkPrice;

                    if (quote.ClosePrice < 5)
                    {
                        var price = quote.ClosePrice.ToString().Split('.').Last();
                        price = price.ReverseString();
                        near = long.Parse(price).ToString().Length;
                    }
                    var checkLenght = quote.ClosePrice.ToString().Split('.').Last();
                    decimal sl = 0;
                    if (side == OrderSide.Buy)
                    {
                        sl = Math.Round(first.MarkPrice.Value * (decimal)(1 - SL_RATE), near);
                    }
                    else
                    {
                        sl = Math.Round(first.MarkPrice.Value * (decimal)(1 + SL_RATE), near);
                    }
                    res = await StaticVal.ByBitInstance().V5Api.Trading.PlaceOrderAsync(Category.Linear,
                                                                                            first.Symbol,
                                                                                            side: SL_side,
                                                                                            type: NewOrderType.Market,
                                                                                            triggerPrice: sl,
                                                                                            triggerDirection: direction,
                                                                                            triggerBy: TriggerType.LastPrice,
                                                                                            quantity: soluong,
                                                                                            timeInForce: TimeInForce.GoodTillCanceled,
                                                                                            reduceOnly: true,
                                                                                            stopLossOrderType: OrderType.Limit,
                                                                                            stopLossTakeProfitMode: StopLossTakeProfitMode.Partial,
                                                                                            stopLossTriggerBy: TriggerType.LastPrice,
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
                            des = $"side: {SL_side}, type: {NewOrderType.Market}, quantity: {soluong}, stopPrice: {sl}"
                        });
                        await _teleService.SendMessage(_idUser, $"[ERROR] |{entity.s}|{res.Error.Message}");
                        return null;
                    }

                    entity.timeStoploss = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                    entity.priceStoploss = (double)sl;
                }
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.PlaceOrder|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private async Task<bool> PlaceOrderClose(string symbol, decimal quan, PositionSide side)
        {
            try
            {
                var res = await StaticVal.ByBitInstance().V5Api.Trading.PlaceOrderAsync(Category.Linear,
                                                                                        symbol,
                                                                                        side: (OrderSide)((int)side),
                                                                                        type: NewOrderType.Market,
                                                                                        quantity: quan);
                if (res.Success)
                {
                    var resCancel = await StaticVal.ByBitInstance().V5Api.Trading.CancelAllOrderAsync(Category.Linear, symbol);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.PlaceOrderClose|EXCEPTION| {ex.Message}");
            }

            await _teleService.SendMessage(_idUser, $"[ERROR] Không thể đóng lệnh {side}: {symbol}!");
            return false;
        }
    }
}
