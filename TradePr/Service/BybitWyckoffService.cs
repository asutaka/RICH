using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;
using CoinUtilsPr;
using CoinUtilsPr.DAL;
using CoinUtilsPr.DAL.Entity;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradePr.Utils;

namespace TradePr.Service
{
    public interface IBybitWyckoffService
    {
        Task<BybitAssetBalance> Bybit_GetAccountInfo();
        Task Bybit_Trade();
    }
    public class BybitWyckoffService : IBybitWyckoffService
    {
        private readonly ILogger<BybitWyckoffService> _logger;
        private readonly IAPIService _apiService;
        private readonly ITeleService _teleService;
        private readonly ISymbolRepo _symRepo;
        private readonly IConfigDataRepo _configRepo;
        private readonly ITradingRepo _tradeRepo;
        private readonly int _exchange = (int)EExchange.Bybit;
        private const long _idUser = 1066022551;
        private readonly int _HOUR = 90;
        public BybitWyckoffService(ILogger<BybitWyckoffService> logger,
                            IAPIService apiService, ITeleService teleService, ISymbolRepo symRepo,
                            IConfigDataRepo configRepo, ITradingRepo tradeRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _teleService = teleService;
            _symRepo = symRepo;
            _configRepo = configRepo;
            _tradeRepo = tradeRepo;
        }

        public async Task<BybitAssetBalance> Bybit_GetAccountInfo()
        {
            try
            {
                var resAPI = await StaticTrade.ByBitInstance().V5Api.Account.GetBalancesAsync(AccountType.Unified);
                return resAPI?.Data?.List?.FirstOrDefault().Assets.FirstOrDefault(x => x.Asset == "USDT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitWyckoffService.Bybit_GetAccountInfo|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        public Task Bybit_Trade()
        {
            throw new NotImplementedException();
        }

        private async Task Bybit_TakeProfit()
        {
            try
            {
                var pos = await StaticTrade.ByBitInstance().V5Api.Trading.GetPositionsAsync(Category.Linear, settleAsset: "USDT");
                if (!pos.Data.List.Any())
                    return;

                var dt = DateTime.UtcNow;

                #region TP
                var dic = new Dictionary<BybitPosition, decimal>();
                foreach (var item in pos.Data.List)
                {
                    var side = item.Side == PositionSide.Sell ? OrderSide.Sell : OrderSide.Buy;

                    var curTime = (dt - item.UpdateTime.Value).TotalHours;

                    var builder = Builders<Trading>.Filter;
                    var place = _tradeRepo.GetEntityByFilter(builder.And(
                        builder.Eq(x => x.ex, _exchange),
                        builder.Eq(x => x.s, item.Symbol),
                        builder.Eq(x => x.Status, 0)
                    //builder.Gte(x => x.Date, DateTime.Now.AddHours(-8)),
                    //builder.Lte(x => x.Date, DateTime.Now.AddHours(-7))
                    ));
                    //Lỗi gì đó ko lưu lại đc log
                    if (place is null)
                    {
                        Console.WriteLine($"Place null");
                        await PlaceOrderClose(item);
                        continue;
                    }
                    //Hết thời gian
                    var dTime = (dt - place.Date).TotalHours;
                    if (curTime >= _HOUR || dTime >= _HOUR)
                    {
                        Console.WriteLine($"Het thoi gian: curTime: {curTime}, dTime: {dTime}");
                        await PlaceOrderClose(item);
                        continue;
                    }
                    //Đạt Target
                    var rate = Math.Round((-1 + item.MarkPrice.Value / item.AveragePrice.Value), 1);
                    if ((place.Side == (int)OrderSide.Buy && rate >= (decimal)place.RateTP)
                        || (place.Side == (int)OrderSide.Sell && rate <= -(decimal)place.RateTP))
                    {
                        Console.WriteLine($"Dat Target: rate: {rate}, RateTP:{place.RateTP}");
                        await PlaceOrderClose(item);
                        continue;
                    }

                    var l15m = await GetData(item.Symbol);
                    if (l15m is null || !l15m.Any())
                        continue;

                    var lbb = l15m.GetBollingerBands();
                    var last = l15m.Last();

                    var cur = l15m.SkipLast(1).Last();
                    var bb_Cur = lbb.First(x => x.Date == cur.Date);

                    var lChotNon = l15m.Where(x => x.Date > item.UpdateTime.Value && x.Date < cur.Date);
                    var flag = false;
                    var isChotNon = false;
                    if (side == OrderSide.Buy)
                    {
                        foreach (var chot in lChotNon)
                        {
                            var bbCheck = lbb.First(x => x.Date == chot.Date);
                            if (chot.High >= (decimal)bbCheck.Sma.Value)
                            {
                                isChotNon = true;
                            }
                        }

                        if (last.High > (decimal)bb_Cur.UpperBand.Value
                            || cur.High > (decimal)bb_Cur.UpperBand.Value)
                        {
                            flag = true;
                        }
                        else if (isChotNon
                                && cur.Close < (decimal)bb_Cur.Sma.Value
                                && cur.Close <= cur.Open
                                && last.Close >= item.AveragePrice.Value)
                        {
                            flag = true;
                        }
                    }
                    else
                    {
                        foreach (var chot in lChotNon)
                        {
                            var bbCheck = lbb.First(x => x.Date == chot.Date);
                            if (chot.Low <= (decimal)bbCheck.Sma.Value)
                            {
                                isChotNon = true;
                            }
                        }

                        if (last.Low < (decimal)bb_Cur.LowerBand.Value
                            || cur.Low < (decimal)bb_Cur.LowerBand.Value)
                        {
                            flag = true;
                        }
                        else if (isChotNon
                            && cur.Close > (decimal)bb_Cur.Sma.Value
                            && cur.Close >= cur.Open
                            && last.Close <= item.AveragePrice.Value)
                        {
                            flag = true;
                        }
                    }
                    if (flag)
                    {
                        Console.WriteLine($"Flag");
                        await PlaceOrderClose(item);
                        continue;
                    }

                    if ((cur.Close >= item.AveragePrice.Value && side == OrderSide.Buy)
                            || (cur.Close <= item.AveragePrice.Value && side == OrderSide.Sell))
                    {
                        dic.Add(item, rate);
                    }
                    else
                    {
                        dic.Add(item, -rate);
                    }
                }
                #endregion

                #region Clean DB
                var lAll = _tradeRepo.GetAll().Where(x => x.Status == 0);
                foreach (var item in lAll)
                {
                    var exist = pos.Data.List.FirstOrDefault(x => x.Symbol == item.s);
                    if (exist is null)
                    {
                        item.Status = 1;
                        _tradeRepo.Update(item);
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.Bybit_TakeProfit|EXCEPTION| {ex.Message}");
            }
        }

        private async Task<bool> PlaceOrderClose(BybitPosition pos)
        {
            var side = pos.Side == PositionSide.Sell ? OrderSide.Sell : OrderSide.Buy;
            var CLOSE_side = pos.Side == PositionSide.Sell ? OrderSide.Buy : OrderSide.Sell;
            try
            {
                var res = await StaticTrade.ByBitInstance().V5Api.Trading.PlaceOrderAsync(Category.Linear,
                                                                                        pos.Symbol,
                                                                                        side: CLOSE_side,
                                                                                        type: NewOrderType.Market,
                                                                                        quantity: Math.Abs(pos.Quantity));
                if (res.Success)
                {
                    var resCancel = await StaticTrade.ByBitInstance().V5Api.Trading.CancelAllOrderAsync(Category.Linear, pos.Symbol);

                    var rate = Math.Round(100 * (-1 + pos.MarkPrice.Value / pos.AveragePrice.Value), 1);
                    var winloss = "LOSS";
                    if (side == OrderSide.Buy && rate > 0)
                    {
                        winloss = "WIN";
                        rate = Math.Abs(rate);
                    }
                    else if (side == OrderSide.Sell && rate < 0)
                    {
                        winloss = "WIN";
                        rate = Math.Abs(rate);
                    }
                    else
                    {
                        rate = -Math.Abs(rate);
                    }

                    var builder = Builders<Trading>.Filter;
                    var filter = builder.And(
                        builder.Eq(x => x.ex, _exchange),
                        builder.Eq(x => x.s, pos.Symbol),
                        builder.Eq(x => x.Status, 0),
                        builder.Gte(x => x.d, (int)DateTimeOffset.Now.AddHours(-8).ToUnixTimeSeconds())
                    );
                    var exists = _tradeRepo.GetEntityByFilter(filter);
                    if (exists != null)
                    {
                        exists.RateClose = (double)rate;
                        exists.Status = 1;
                        _tradeRepo.Update(exists);
                    }

                    var balance = string.Empty;
                    var account = await Bybit_GetAccountInfo();
                    if (account != null)
                    {
                        balance = $"|Balance: {Math.Round(account.WalletBalance.Value, 1)}$";
                    }

                    await _teleService.SendMessage(_idUser, $"[CLOSE - {side.ToString().ToUpper()}({winloss}: {rate}%)|Bybit] {pos.Symbol}|TP: {pos.MarkPrice}|Entry: {pos.AveragePrice}{balance}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm")}|BybitService.PlaceOrderClose|EXCEPTION| {ex.Message}");
            }
            await _teleService.SendMessage(_idUser, $"[Bybit] Không thể đóng lệnh {side}: {pos.Symbol}!");
            return false;
        }

        private List<long> _lIgnoreCode = new List<long>
        {
            110007
        };
    }
}
