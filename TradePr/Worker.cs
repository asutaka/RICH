using CoinUtilsPr;
using TradePr.Service;

namespace TradePr
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IBinanceService _binnanceService;
        private readonly IBybitService _bybitService;
        private readonly ISyncDataService _syncService;

        public Worker(ILogger<Worker> logger, IBinanceService binanceService, IBybitService bybitService, ISyncDataService syncService)
        {
            _logger = logger;
            _binnanceService = binanceService;
            _bybitService = bybitService;
            _syncService = syncService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //await _bybitService.Bybit_GetAccountInfo();
            //await _binnanceService.Binance_GetAccountInfo();

            //await _bybitService.Bybit_Trade();
            //await _syncService.Bybit_LONG();
            //return;

            while (!stoppingToken.IsCancellationRequested)
            {
                var dt = DateTime.Now;
                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                //var binance = _binnanceService.Binance_Trade();
                await _bybitService.Bybit_Trade();
                //Task.WaitAll(binance, bybit);

                if(dt.DayOfWeek == DayOfWeek.Monday)
                {
                    if(dt.Hour == 9)
                    {
                        _syncService.ClearData();
                        if (false)
                        {

                        }
                        else if (dt.Minute == 0)
                        {
                            _syncService.Bybit_LONG(EOrderSideOption.OP_0);
                        }
                        else if(dt.Minute == 15)
                        {
                            _syncService.Bybit_LONG(EOrderSideOption.OP_1);
                        }
                        else if (dt.Minute == 30)
                        {
                            _syncService.Bybit_LONG(EOrderSideOption.OP_2);
                        }   
                    }   
                    if(dt.Hour == 10)
                    {
                        if (false)
                        {

                        }
                        else if (dt.Minute == 0)
                        {
                            _syncService.Bybit_SHORT(EOrderSideOption.OP_0);
                            //_syncService.Binance_LONG();
                        }
                        else if (dt.Minute == 15)
                        {
                            _syncService.Bybit_SHORT(EOrderSideOption.OP_1);
                        }
                        else if (dt.Minute == 30)
                        {
                            //_syncService.Binance_SHORT();
                            _syncService.Bybit_SHORT(EOrderSideOption.OP_2);
                        }
                    }
                }

                //if(dt.Hour == 7 && dt.Minute == 0)
                //{
                //    await _syncService.UnitSetting();
                //}

                await Task.Delay(1000 * 60, stoppingToken);
            }
        }
    }
}
