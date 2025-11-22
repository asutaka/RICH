using TestPr.Service;

namespace TestPr
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ITestService _testService;
        private readonly ILastService _lastService;

        public Worker(ILogger<Worker> logger, ITestService testService, ILastService lastService)
        {
            _logger = logger;
            _testService = testService;
            _lastService = lastService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _lastService.fake();
            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //    await Task.Delay(1000, stoppingToken);
            //}

            
        }
    }
   
}
