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
        private readonly IBaoCaoPhanTichService _bcptService;
        private readonly ITeleService _teleService;
        private const long _idGroup = -4237476810;
        //private const long _idChannel = -1002247826353;
        //private const long _idUser = 1066022551;

        public Worker(ILogger<Worker> logger, 
                    ITeleService teleService, IBaoCaoPhanTichService bcptService,
                    IStockRepo stockRepo)
        {
            _logger = logger;
            _bcptService = bcptService;
            _teleService = teleService;
            _stockRepo = stockRepo;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            StockInstance();
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                var bcpt = await _bcptService.BaoCaoPhanTich();
                if(!string.IsNullOrWhiteSpace(bcpt))
                {
                    await _teleService.SendMessage(_idGroup, bcpt);
                }
                await Task.Delay(1000 * 60 * 15, stoppingToken);
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
