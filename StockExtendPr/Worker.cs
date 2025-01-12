using StockExtendPr.Service;

namespace StockExtendPr
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IGiaNganhHangService _giaService;

        public Worker(ILogger<Worker> logger, IGiaNganhHangService giaService)
        {
            _logger = logger;
            _giaService = giaService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _giaService.TraceGia(true);
                await Task.Delay(1000 * 60 * 60, stoppingToken);
            }
        }
    }
}
