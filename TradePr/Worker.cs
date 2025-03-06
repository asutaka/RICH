using TradePr.Service;

namespace TradePr
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IBinanceService _binnanceService;
        private readonly IBybitService _bybitService;

        public Worker(ILogger<Worker> logger, IBinanceService binanceService, IBybitService bybitService)
        {
            _logger = logger;
            _binnanceService = binanceService;
            _bybitService = bybitService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //await _bybitService.Bybit_GetAccountInfo();
            //await _binnanceService.GetAccountInfo();
            while (!stoppingToken.IsCancellationRequested)
            {
                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await _binnanceService.Binance_Trade();
                await _bybitService.Bybit_Trade();
                await Task.Delay(1000 * 60, stoppingToken);
            }
        }
    }
}
