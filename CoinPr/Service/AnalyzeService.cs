﻿using CoinPr.DAL;
using CoinPr.DAL.Entity;
using CoinPr.Utils;
using MongoDB.Driver;

namespace CoinPr.Service
{
    public interface IAnalyzeService
    {
        Task SyncCoinBinance();
        Task DetectOrderBlock();
    }

    public class AnalyzeService : IAnalyzeService
    {
        private readonly ILogger<AnalyzeService> _logger;
        private readonly IAPIService _apiService;
        private readonly IOrderBlockRepo _orderBlockRepo;
        private readonly ICoinRepo _coinRepo;
        public AnalyzeService(ILogger<AnalyzeService> logger,
                                    IAPIService apiService,
                                    IOrderBlockRepo orderBlockRepo,
                                    ICoinRepo coinRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _orderBlockRepo = orderBlockRepo;
            _coinRepo = coinRepo;
        }

        public async Task SyncCoinBinance()
        {
            try
            {
                var lCoin = _coinRepo.GetAll();
                var lBinance = await _apiService.GetBinanceSymbol();
                foreach ( var item in lBinance)
                {
                    var first = lCoin.FirstOrDefault(x => x.FromAsset == item.FromAsset);
                    if(first is null)
                    {
                        _coinRepo.InsertOne(new Coin
                        {
                            FromAsset = item.FromAsset,
                            ToAsset = item.ToAsset,
                            ContractKey = $"{item.FromAsset}{item.ToAsset}"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.SyncCoinBinance|EXCEPTION| {ex.Message}");
            }
        }

        public async Task DetectOrderBlock()
        {
            try
            {
                var lCoin = _coinRepo.GetAll();
                #region 1W
                _orderBlockRepo.DeleteMany(Builders<OrderBlock>.Filter.Eq(x => x.interval, (int)EInterval.W1));
                foreach (var binance in lCoin)
                {
                    await CheckOrderBlock($"{binance.FromAsset}{binance.ToAsset}", exchange: EExchange.Binance, EInterval.W1, 20);
                } 
                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.DetectOrderBlock|EXCEPTION| {ex.Message}");
            }
        }

        private async Task CheckOrderBlock(string symbol, EExchange exchange, EInterval interval, int minrate = 5)
        {
            try
            {
                var lData = await _apiService.GetData(symbol, exchange, interval);
                Thread.Sleep(200);
                var lOrderBlock = lData.GetOrderBlock(minrate);
                if (lOrderBlock.Any())
                {
                    foreach (var item in lOrderBlock)
                    {
                        item.s = symbol;
                        item.ex = (int)exchange;
                        item.interval = (int)interval;
                        _orderBlockRepo.InsertOne(item);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.CheckOrderBlock|EXCEPTION|INPUT: {symbol}| {ex.Message}");
            }
        }
    }
}
