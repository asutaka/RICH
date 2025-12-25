using Bybit.Net.Enums;
using CoinUtilsPr;
using TestPr.Model;
using TestPr.Service;

namespace TestPr
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITestService _testService;
        private readonly ILastService _lastService;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, ITestService testService, ILastService lastService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _testService = testService;
            _lastService = lastService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("\n╔════════════════════════════════════════╗");
            Console.WriteLine("║   BACKTEST TAKERSERVICE STRATEGY      ║");
            Console.WriteLine("╚════════════════════════════════════════╝\n");

            Console.WriteLine("Chọn chức năng:");
            Console.WriteLine("1. Chạy backtest mặc định (SOL, 90 ngày gần nhất)");
            Console.WriteLine("2. Chạy backtest tùy chỉnh (nhiều coin)");
            Console.WriteLine("3. Chạy LastService.fake3() (chức năng cũ)");
            Console.Write("\nNhập lựa chọn (1-3): ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await RunDefaultBacktest();
                    break;
                case "2":
                    await RunCustomBacktest();
                    break;
                case "3":
                    await _lastService.fake3();
                    break;
                default:
                    Console.WriteLine("Lựa chọn không hợp lệ!");
                    break;
            }

            Console.WriteLine("\nHoàn thành! Chương trình sẽ tự động thoát sau 3 giây...");
            await Task.Delay(3000);
        }

        private async Task RunDefaultBacktest()
        {
            using var scope = _serviceProvider.CreateScope();
            var backtestService = scope.ServiceProvider.GetRequiredService<IBacktestService>();

            var config = new BacktestConfig
            {
                Symbols = new List<string> { "SOLUSDT" },
                StartDate = DateTime.UtcNow.AddDays(-90), // Tăng từ 30 lên 90 ngày
                EndDate = DateTime.UtcNow,
                MaxCandlesHold = 24,
                StopLossRate = 1.5m
            };

            await backtestService.RunBacktest(config);
        }

        private async Task RunCustomBacktest()
        {
            using var scope = _serviceProvider.CreateScope();
            var backtestService = scope.ServiceProvider.GetRequiredService<IBacktestService>();

            Console.WriteLine("\n--- BACKTEST TÙY CHỈNH ---");
            //var lAll = await StaticVal.ByBitInstance().V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, limit: 1000);
            //var lUsdt = lAll.Data.List.Where(x => x.QuoteAsset == "USDT" && !x.Name.StartsWith("1000")).Select(x => x.Name);
            //var symbols = lUsdt.Skip(500).Take(100).ToList();

            // Nhập danh sách coin
            //Console.WriteLine("\nNhập danh sách coin (cách nhau bởi dấu phẩy):");
            //Console.WriteLine("Ví dụ: SOLUSDT,BTCUSDT,ETHUSDT");
            //Console.Write("Danh sách coin: ");
            //var symbolsInput = Console.ReadLine();
            var symbols = new List<string> { "YGGUSDT", "VINEUSDT", "ZKCUSDT" };

            //// Nhập số ngày backtest
            //Console.Write("\nNhập số ngày backtest (mặc định 30): ");
            //var daysInput = Console.ReadLine();
            //int days = int.TryParse(daysInput, out var d) ? d : 30;

            //// Nhập Stop Loss Rate
            //Console.Write("\nNhập Stop Loss % (mặc định 1.5): ");
            //var slInput = Console.ReadLine();
            //decimal slRate = decimal.TryParse(slInput, out var sl) ? sl : 1.5m;

            //// Nhập Max Candles Hold
            //Console.Write("\nNhập số nến giữ tối đa (mặc định 24): ");
            //var maxCandlesInput = Console.ReadLine();
            //int maxCandles = int.TryParse(maxCandlesInput, out var mc) ? mc : 24;

            var config = new BacktestConfig
            {
                Symbols = symbols,
                StartDate = DateTime.UtcNow.AddDays(-90), // Tăng từ 30 lên 90 ngày
                EndDate = DateTime.UtcNow,
                MaxCandlesHold = 24,
                StopLossRate = 1.5m
            };

            await backtestService.RunBacktest(config);
        }
    }
   
}

