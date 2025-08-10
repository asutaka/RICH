using CoinPr.Service;

namespace CoinPr
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IPrepareService _prepareService;

        public Worker(ILogger<Worker> logger, IPrepareService prepareService)
        {
            _logger = logger;
            _prepareService = prepareService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    _prepareService.CheckWycKoffPrepare();
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    await Task.Delay(1000 * 60 * 60 * 6, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Worker.ExecuteAsync|EXCEPTION| {ex.Message}");
            }
        }
    }
}
