using TestPr.Service;

namespace TestPr
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ITestService _testService;

        public Worker(ILogger<Worker> logger, ITestService testService)
        {
            _logger = logger;
            _testService = testService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _testService.Bybit_SHORT();
            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //    await Task.Delay(1000, stoppingToken);
            //}
        }
    }
}
