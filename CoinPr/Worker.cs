using CoinPr.Service;

namespace CoinPr
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IAnalyzeService _analyzeService;

        public Worker(ILogger<Worker> logger, IAnalyzeService analyzeService)
        {
            _logger = logger;
            _analyzeService = analyzeService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                //await _analyzeService.DetectOrderBlock();
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
