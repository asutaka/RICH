using Bybit.Net.Objects.Models.V5;
using MongoDB.Driver;
using Skender.Stock.Indicators;
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
        Task Bybit_MarketAction();
    }
    public class BybitService : IBybitService
    {
        private readonly ILogger<BybitService> _logger;
        private readonly ITradingRepo _tradingRepo;
        private readonly ISignalTradeRepo _signalTradeRepo;
        private readonly IErrorPartnerRepo _errRepo;
        private readonly IConfigDataRepo _configRepo;
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private const long _idUser = 1066022551;
        private const decimal _unit = 50;
        private const decimal _margin = 10;
        public BybitService(ILogger<BybitService> logger, ITradingRepo tradingRepo, IAPIService apiService,
                            ISignalTradeRepo signalTradeRepo, ITeleService teleService, IErrorPartnerRepo errRepo, IConfigDataRepo configRepo)
        {
            _logger = logger;
            _tradingRepo = tradingRepo;
            _apiService = apiService;
            _teleService = teleService;
            _signalTradeRepo = signalTradeRepo;
            _errRepo = errRepo;
            _configRepo = configRepo;
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
                _logger.LogError(ex, $"BybitService.GetAccountInfo|EXCEPTION| {ex.Message}");
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

                    //Trade
                    var res = await PlaceOrder(entity, lData15m.Last());
                    if(res != null)
                    {
                        _signalTradeRepo.InsertOne(res);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BybitService.TradeSignal|EXCEPTION| {ex.Message}");
            }
            return;
        }

        public async Task Bybit_MarketAction()
        {
            try
            {
                var config = _configRepo.GetAll();
                if (config.FirstOrDefault(x => x.ex == (int)EExchange.Bybit 
                                        && (x.op == (int)EOption.Signal || x.op == (int)EOption.ThreeSignal) 
                                        && x.status > 0) is null)
                    return;

                var sBuilder = new StringBuilder();
                var time = (int)DateTimeOffset.Now.AddHours(3).ToUnixTimeSeconds();
                var timeLeft = (int)DateTimeOffset.Now.AddHours(2).ToUnixTimeSeconds();
                var lTrade = _signalTradeRepo.GetByFilter(Builders<SignalTrade>.Filter.Gte(x => x.timeFlag, time));
                lTrade = lTrade.Where(x => x.timeFlag >= timeLeft && x.status == 0).ToList();
                if (!(lTrade?.Any() ?? false))
                    return;

                var resPosition = await StaticVal.ByBitInstance().V5Api.Trading.GetPositionsAsync(Bybit.Net.Enums.Category.Linear);
                if (!resPosition.Data.List.Any())
                    return;

                var mes = await MarketActionStr(resPosition.Data.List, lTrade);
                if(!string.IsNullOrWhiteSpace(mes))
                {
                    sBuilder.AppendLine(mes);
                }

                //Force Sell - Khi trong 1 khoảng thời gian ngắn có một loạt các lệnh thanh lý ngược chiều vị thế
                var timeForce = (int)DateTimeOffset.Now.AddMinutes(-15).ToUnixTimeSeconds();
                var lForce = _tradingRepo.GetByFilter(Builders<Trading>.Filter.Gte(x => x.d, timeForce));
                var countForceSell = lForce.Count(x => x.Side == (int)Bybit.Net.Enums.PositionSide.Sell);
                var countForceBuy = lForce.Count(x => x.Side == (int)Bybit.Net.Enums.PositionSide.Buy);
                if(countForceSell >= 5)
                {
                    var lSell = resPosition.Data.List.Where(x => x.Side == Bybit.Net.Enums.PositionSide.Buy);
                    var mesSell = await MarketActionStr(lSell, lTrade);
                    if (!string.IsNullOrWhiteSpace(mesSell))
                    {
                        sBuilder.AppendLine(mesSell);
                    }
                }
                if(countForceBuy >= 5)
                {
                    var lBuy = resPosition.Data.List.Where(x => x.Side == Bybit.Net.Enums.PositionSide.Sell);
                    var mesBuy = await MarketActionStr(lBuy, lTrade);
                    if (!string.IsNullOrWhiteSpace(mesBuy))
                    {
                        sBuilder.AppendLine(mesBuy);
                    }
                }

                if (sBuilder.Length > 0)
                {
                    await _teleService.SendMessage(_idUser, sBuilder.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BybitService.MarketAction|EXCEPTION| {ex.Message}");
            }
        }

        private async Task<string> MarketActionStr(IEnumerable<BybitPosition> lData, List<SignalTrade> lTrade)
        {
            var sBuilder = new StringBuilder();
            try
            {
                foreach (var item in lData)
                {
                    var first = lTrade.FirstOrDefault(x => x.s == item.Symbol);
                    if (first is null && (DateTime.Now - item.CreateTime.Value).TotalHours < 24)
                        continue;

                    var side = (item.Side == Bybit.Net.Enums.PositionSide.Buy) ? Bybit.Net.Enums.PositionSide.Sell : Bybit.Net.Enums.PositionSide.Buy;
                    var res = await PlaceOrderClose(item.Symbol, item.Quantity, side);
                    if (!res)
                        continue;

                    if (first is null)
                        continue;

                    first.priceClose = (double)item.MarkPrice.Value;
                    first.timeClose = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                    first.rate = Math.Round(100 * (-1 + first.priceClose / first.priceEntry), 1);
                    first.status = 1;
                    _signalTradeRepo.Update(first);

                    var mes = $"[Đóng vị thế {item.Side}] {first.s}|Giá đóng: {first.priceClose}|Rate: {first.rate}%";
                    sBuilder.AppendLine(mes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BybitService.MarketActionStr|EXCEPTION| {ex.Message}");
            }

            return sBuilder.ToString();
        }

        private async Task<SignalTrade> PlaceOrder(SignalTrade entity, Quote quote)
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

                var near = 2; if (quote.Close < 1)
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
