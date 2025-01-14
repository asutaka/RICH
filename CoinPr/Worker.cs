using CoinPr.Service;

namespace CoinPr
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IAnalyzeService _analyzeService;
        private readonly ITeleService _teleService;

        public Worker(ILogger<Worker> logger, IAnalyzeService analyzeService, ITeleService teleService)
        {
            _logger = logger;
            _analyzeService = analyzeService;
            _teleService = teleService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var dt = DateTime.Now;  
            while (!stoppingToken.IsCancellationRequested)
            {
                if (dt.Hour < 12)
                {
                    await _analyzeService.SyncCoinBinance();
                }
                await _analyzeService.DetectOrderBlock();
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000 * 60 * 60 * 6, stoppingToken);
            }
        }
    }
}
