using CoinPr.Service;

namespace CoinPr
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IWebSocketService _socketService;

        public Worker(ILogger<Worker> logger, IWebSocketService socketService)
        {
            _logger = logger;
            _socketService = socketService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _socketService.BinanceLiquid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Worker.ExecuteAsync|EXCEPTION| {ex.Message}");
            }
        }
    }
}
