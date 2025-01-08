using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Service;
using StockPr.Utils;

namespace StockPr
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IStockRepo _stockRepo;

        public Worker(ILogger<Worker> logger, ITeleService teleService, IStockRepo stockRepo)
        {
            _logger = logger;
            _stockRepo = stockRepo;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            StockInstance();
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

        private List<Stock> StockInstance()
        {
            if (StaticVal._lStock != null && StaticVal._lStock.Any())
                return StaticVal._lStock;
            StaticVal._lStock = _stockRepo.GetAll();

            return StaticVal._lStock;
        }
    }
}
