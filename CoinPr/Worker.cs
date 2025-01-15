using CoinPr.Service;
using CryptoExchange.Net.CommonObjects;

namespace CoinPr
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IAnalyzeService _analyzeService;
        private readonly ITeleService _teleService;
        private readonly IWebSocketService _socketService;

        public Worker(ILogger<Worker> logger, IAnalyzeService analyzeService, ITeleService teleService, IWebSocketService socketService)
        {
            _logger = logger;
            _analyzeService = analyzeService;
            _teleService = teleService;
            _socketService = socketService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var dt = DateTime.Now;
            try
            {
                //Task.Run(() =>
                //{
                //    _socketService.LiquidWebSocket("wss://ws.coinank.com/wsKline/wsKline");
                //});
                _socketService.BinancePrice();
                //_socketService.BinanceLiquid();
                while (!stoppingToken.IsCancellationRequested)
                {
                    //if (dt.Hour < 12)
                    //{
                    //    await _analyzeService.SyncCoinBinance();
                    //}
                    //await _analyzeService.DetectOrderBlock();
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    //await Task.Delay(1000, stoppingToken);
                    await Task.Delay(1000 * 60 * 60 * 12, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Worker.ExecuteAsync|EXCEPTION| {ex.Message}");
            }
        }
    }
}
