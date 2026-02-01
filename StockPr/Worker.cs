using Microsoft.Extensions.Options;
using MongoDB.Driver;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Research;
using StockPr.Service;
using StockPr.Settings;
using StockPr.Utils;
using System.Threading.Tasks;

namespace StockPr
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IStockRepo _stockRepo;
        private readonly IAccountRepo _accountRepo;
        private readonly IBaoCaoPhanTichService _bcptService;
        private readonly IGiaNganhHangService _giaService;
        private readonly ITeleService _teleService;
        private readonly ITongCucThongKeService _tongcucService;
        private readonly IAnalyzeService _analyzeService;
        private readonly ITuDoanhService _tudoanhService;
        private readonly IBaoCaoTaiChinhService _bctcService;
        private readonly IPortfolioService _portfolioService;
        private readonly IEPSRankService _epsService;
        private readonly IF319Service _f319Service;
        private readonly IChartService _chartService;
        private readonly INewsService _newsService;
        private readonly ICommonService _commonService;
        private readonly IVietstockAuthService _authService;
        private readonly IBacktestService _backtestService; // ✨ For research/testing
        private readonly IAPIService _APIService;

        private readonly long _idGroup;
        private readonly long _idGroupF319;
        private readonly long _idChannel;
        private readonly long _idChannelNews;
        private readonly long _idUser;

        public Worker(ILogger<Worker> logger, 
                    ITeleService teleService, IBaoCaoPhanTichService bcptService, IGiaNganhHangService giaService, ITongCucThongKeService tongcucService, 
                    IAnalyzeService analyzeService, ITuDoanhService tudoanhService, IBaoCaoTaiChinhService bctcService, IStockRepo stockRepo, IAccountRepo accountRepo,
                    IPortfolioService portfolioService, IEPSRankService epsService, IF319Service f319Service, 
                    ICommonService commonService, INewsService newsService, IChartService chartService, IVietstockAuthService authService,
                    IBacktestService backtestService, // ✨ Inject backtest service
                    IOptions<TelegramSettings> telegramSettings,
                    IAPIService apiService)
        {
            _logger = logger;
            _bcptService = bcptService;
            _teleService = teleService;
            _giaService = giaService;
            _tongcucService = tongcucService;
            _analyzeService = analyzeService;
            _tudoanhService = tudoanhService;
            _bctcService = bctcService;

            _stockRepo = stockRepo;
            _accountRepo = accountRepo;
            _portfolioService = portfolioService;
            _epsService = epsService;
            _f319Service = f319Service;
            _commonService = commonService;
            _newsService = newsService;
            _chartService = chartService;
            _authService = authService;
            _backtestService = backtestService; // ✨ Store backtest service
            
            // Load Telegram IDs from configuration
            _idGroup = telegramSettings.Value.GroupId;
            _idGroupF319 = telegramSettings.Value.GroupF319Id;
            _idChannel = telegramSettings.Value.ChannelId;
            _idChannelNews = telegramSettings.Value.ChannelNewsId;
            _idUser = telegramSettings.Value.UserId;
            _APIService = apiService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("StockPr Worker starting at: {time}", DateTimeOffset.Now);

            // Initialize data
            StockInstance();

            // ==================== BACKTEST RUNNER ====================
            // ⚠️ UNCOMMENT PHẦN NÀY ĐỂ CHẠY BACKTEST
            // Nhớ COMMENT LẠI sau khi test xong!
            /*
            try
            {
                _logger.LogInformation("=== STARTING BACKTEST ===");
                await _backtestService.BackTest22122025();    
                _logger.LogInformation("=== BACKTEST COMPLETED ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backtest failed");
                throw;
            }
            return; // Dừng Worker sau khi backtest xong
            */
            // ==================== END BACKTEST RUNNER ====================

            _logger.LogInformation("StockPr Worker is running. Jobs are managed by Quartz.NET.");

            // Keep the worker alive while the host is running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private List<Stock> StockInstance()
        {
            if (StaticVal._lStock != null && StaticVal._lStock.Any())
                return StaticVal._lStock;
            
            _logger.LogInformation("Initializing Stock list from database...");
            StaticVal._lStock = _stockRepo.GetAll();
            _commonService.GetCurrentTime();

            return StaticVal._lStock;
        }
    }
}
