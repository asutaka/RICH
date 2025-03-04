﻿using Bybit.Net.Objects.Models.V5;
using MongoDB.Driver;
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


                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BybitService.TradeSignal|EXCEPTION| {ex.Message}");
            }
            return;
        }
    }
}
