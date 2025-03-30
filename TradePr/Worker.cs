using TradePr.Service;
using TradePr.Utils;

namespace TradePr
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IBinanceService _binnanceService;
        private readonly IBybitService _bybitService;
        private readonly IWebSocketService _socketService;

        public Worker(ILogger<Worker> logger, IBinanceService binanceService, IBybitService bybitService, IWebSocketService socketService)
        {
            _logger = logger;
            _binnanceService = binanceService;
            _bybitService = bybitService;
            _socketService = socketService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //await _bybitService.Bybit_GetAccountInfo();
            //await _binnanceService.Binance_GetAccountInfo();

            //await _bybitService.Bybit_Trade();
            //return;

            _socketService.BinanceAction();
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
