using Microsoft.Extensions.Options;
using MongoDB.Driver;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Research;
using StockPr.Service;
using StockPr.Settings;
using StockPr.Utils;

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
        private readonly IBacktestService _backtestService; // ✨ For research/testing

        private readonly long _idGroup;
        private readonly long _idGroupF319;
        private readonly long _idChannel;
        private readonly long _idChannelNews;
        private readonly long _idUser;

        public Worker(ILogger<Worker> logger, 
                    ITeleService teleService, IBaoCaoPhanTichService bcptService, IGiaNganhHangService giaService, ITongCucThongKeService tongcucService, 
                    IAnalyzeService analyzeService, ITuDoanhService tudoanhService, IBaoCaoTaiChinhService bctcService, IStockRepo stockRepo, IAccountRepo accountRepo,
                    IPortfolioService portfolioService, IEPSRankService epsService, IF319Service f319Service, 
                    ICommonService commonService, INewsService newsService, IChartService chartService,
                    IBacktestService backtestService, // ✨ Inject backtest service
                    IOptions<TelegramSettings> telegramSettings)
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
            _backtestService = backtestService; // ✨ Store backtest service
            
            // Load Telegram IDs from configuration
            _idGroup = telegramSettings.Value.GroupId;
            _idGroupF319 = telegramSettings.Value.GroupF319Id;
            _idChannel = telegramSettings.Value.ChannelId;
            _idChannelNews = telegramSettings.Value.ChannelNewsId;
            _idUser = telegramSettings.Value.UserId;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            StockInstance();

            //// ==================== BACKTEST RUNNER ====================
            //// ⚠️ UNCOMMENT PHẦN NÀY ĐỂ CHẠY BACKTEST
            //// Nhớ COMMENT LẠI sau khi test xong!

            //try
            //{
            //    _logger.LogInformation("=== STARTING BACKTEST ===");

            //    // Chọn backtest muốn chạy (uncomment 1 trong các dòng dưới):

            //    await _backtestService.BackTest22122025();    
            //    //await _backtestService.BacktestOptimalStrategy();        // ✨ Phân tích chỉ báo (BB, RSI, MA9, WMA45)
            //                                                       // await _backtestService.BatDayCK();                    // Bắt đáy CK
            //                                                       // await _backtestService.CheckAllDay_OnlyVolume();   // Check volume patterns
            //                                                       // await _backtestService.CheckGDNN();                // Check GDNN
            //                                                       // await _backtestService.CheckCungCau();             // Check cung cầu
            //                                                       // await _backtestService.CheckCrossMa50_BB();        // Check MA50 & BB
            //                                                       // await _backtestService.CheckWycKoff();             // Check Wyckoff

            //    _logger.LogInformation("=== BACKTEST COMPLETED ===");
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Backtest failed");
            //    throw;
            //}
            //return; // Dừng Worker sau khi backtest xong

            // ==================== END BACKTEST RUNNER ====================

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var dt = DateTime.Now;
                    var isDayOfWork = dt.DayOfWeek >= DayOfWeek.Monday && dt.DayOfWeek <= DayOfWeek.Friday;
                    var isPreTrade = dt.Hour < 9;
                    var isTimePrint = dt.Minute >= 15 && dt.Minute < 30;
                    var isRealTime = (dt.Hour >= 9 && dt.Hour < 12) || (dt.Hour >= 13 && dt.Hour < 15);

                    // ⚡ PARALLEL EXECUTION: Tạo list tasks để chạy song song
                    var tasks = new List<Task>();

                    // Always run tasks (không phụ thuộc thời gian)
                    tasks.Add(ProcessBaoCaoPhanTich());
                    tasks.Add(ProcessF319KOL());
                    tasks.Add(ProcessNews());
                    tasks.Add(ProcessPortfolio());

                    // Conditional tasks (phụ thuộc thời gian)
                    if (dt.Day == 6 && dt.Hour >= 9 && dt.Hour < 11)
                    {
                        tasks.Add(ProcessTongCucThongKe(dt));
                    }

                    if (dt.Minute < 15)
                    {
                        if (dt.Hour == 9 || dt.Hour == 13)
                        {
                            tasks.Add(ProcessTraceGia(false));
                        }
                        else if (dt.Hour == 17)
                        {
                            tasks.Add(ProcessTraceGia(true));
                        }
                    }

                    if (isDayOfWork && !StaticVal._lNghiLe.Any(x => x.Month == dt.Month && x.Day == dt.Day))
                    {
                        if (!isPreTrade)
                        {
                            if (isRealTime && isTimePrint)
                            {
                                tasks.Add(ProcessRealtime());
                            }

                            if ((dt.Hour == 14 && dt.Minute >= 45) || (dt.Hour == 15 && dt.Minute <= 30))
                            {
                                tasks.Add(ProcessThongKeGDNN());
                                tasks.Add(ProcessChiBaoKyThuat(dt));
                            }

                            if (dt.Hour >= 19 && dt.Hour <= 23)
                            {
                                tasks.Add(ProcessThongKeTuDoanh());
                                tasks.Add(ProcessChartThongKeKhopLenh());
                                Process4U();
                            }

                            if(dt.Hour >= 19 && dt.Minute >= 30)
                            {
                                tasks.Add(ProcessDetectEntry());
                            }
                        }

                        if (dt.Hour == 11 && dt.Minute >= 30 && dt.Minute < 45)
                        {
                            tasks.Add(ProcessThongKeForeignPhienSang(dt));
                        }
                    }

                    if (dt.Hour == 23 && dt.Minute <= 15)
                    {
                        tasks.Add(Task.Run(() => ProcessEPSRank()));
                    }

                    // ⚡ CHẠY TẤT CẢ TASKS SONG SONG
                    await Task.WhenAll(tasks);

                    _logger.LogInformation($"Completed {tasks.Count} tasks in parallel at {DateTime.Now}");

                    await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker execution failed");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }


        #region Helper Methods for Parallel Execution

        private readonly SemaphoreSlim _telegramSemaphore = new SemaphoreSlim(1, 1);

        private async Task SendMessagesWithRateLimit(long chatId, IEnumerable<string> messages, bool isMarkdown = false)
        {
            await _telegramSemaphore.WaitAsync();
            try
            {
                foreach (var message in messages)
                {
                    await _teleService.SendMessage(chatId, message, isMarkdown);
                    await Task.Delay(200); // Rate limit: 5 msg/second
                }
            }
            finally
            {
                _telegramSemaphore.Release();
            }
        }

        private async Task ProcessBaoCaoPhanTich()
        {
            try
            {
                var lBCPT = await _bcptService.BaoCaoPhanTich();
                if (lBCPT.Item1?.Any() ?? false)
                {
                    foreach (var item in lBCPT.Item1)
                    {
                        await _teleService.SendMessage(_idGroup, item.content, item.link);
                        await Task.Delay(200);
                    }
                }
                if (lBCPT.Item2?.Any() ?? false)
                {
                    var mes = string.Join("\n", lBCPT.Item2);
                    await _teleService.SendMessage(_idUser, mes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessBaoCaoPhanTich failed");
            }
        }

        private async Task ProcessF319KOL()
        {
            try
            {
                var f319 = await _f319Service.F319KOL();
                if (f319.Any())
                {
                    await SendMessagesWithRateLimit(_idGroupF319, f319.Select(x => x.Message), true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessF319KOL failed");
            }
        }

        private async Task ProcessNews()
        {
            try
            {
                var news = await _newsService.GetNews();
                if (news.Any())
                {
                    await SendMessagesWithRateLimit(_idChannelNews, news, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessNews failed");
            }
        }

        private async Task ProcessPortfolio()
        {
            try
            {
                var portfolio = await _portfolioService.Portfolio();
                if (!string.IsNullOrWhiteSpace(portfolio.Item1))
                {
                    await _teleService.SendMessage(_idChannel, portfolio.Item1, portfolio.Item2);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessPortfolio failed");
            }
        }

        private async Task ProcessTongCucThongKe(DateTime dt)
        {
            try
            {
                var res = await _tongcucService.TongCucThongKe(dt);
                if (res.Item1 > 0)
                {
                    await _teleService.SendMessage(_idChannel, res.Item2);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessTongCucThongKe failed");
            }
        }

        private async Task ProcessTraceGia(bool isEndOfDay)
        {
            try
            {
                var res = await _giaService.TraceGia(isEndOfDay);
                if (res.Item1 > 0)
                {
                    await _teleService.SendMessage(_idGroup, res.Item2, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessTraceGia failed");
            }
        }

        private async Task ProcessRealtime()
        {
            try
            {
                var mes = await _analyzeService.Realtime();
                if (!string.IsNullOrWhiteSpace(mes))
                {
                    await _teleService.SendMessage(_idChannel, mes, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessRealtime failed");
            }
        }

        private async Task ProcessThongKeGDNN()
        {
            try
            {
                var mes = await _analyzeService.ThongKeGDNN_NhomNganh();
                if (!string.IsNullOrWhiteSpace(mes))
                {
                    await _teleService.SendMessage(_idChannel, mes, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessThongKeGDNN failed");
            }
        }

        private async Task ProcessChiBaoKyThuat(DateTime dt)
        {
            try
            {
                var res = await _analyzeService.ChiBaoKyThuat(dt, true);
                if (res.Item1 > 0)
                {
                    await _teleService.SendMessage(_idChannel, res.Item2, true);
                }
                if (!string.IsNullOrWhiteSpace(res.Item4))
                {
                    await _teleService.SendMessage(_idUser, res.Item4);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessChiBaoKyThuat failed");
            }
        }

        private async Task ProcessThongKeTuDoanh()
        {
            try
            {
                var mes = await _analyzeService.ThongKeTuDoanh();
                if (!string.IsNullOrWhiteSpace(mes))
                {
                    await _teleService.SendMessage(_idChannel, mes, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessThongKeTuDoanh failed");
            }
        }

        private async Task ProcessChartThongKeKhopLenh()
        {
            try
            {
                var stream = await _analyzeService.Chart_ThongKeKhopLenh();
                if (stream != null)
                {
                    await _teleService.SendPhoto(_idChannel, stream);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessChartThongKeKhopLenh failed");
            }
        }

        private async Task ProcessThongKeForeignPhienSang(DateTime dt)
        {
            try
            {
                var gdnn = await _analyzeService.ThongkeForeign_PhienSang(dt);
                if (gdnn.Item1 > 0)
                {
                    await _teleService.SendMessage(_idChannel, gdnn.Item2, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessThongKeForeignPhienSang failed");
            }
        }

        private void ProcessEPSRank()
        {
            try
            {
                _epsService.RankMaCKSync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessEPSRank failed");
            }
        }

        private async Task ProcessDetectEntry()
        {
            var mes = await _analyzeService.DetectEntry();
            if (!string.IsNullOrWhiteSpace(mes))
            {
                await _teleService.SendMessage(_idUser, mes, true);
            }
        }

        private async Task Process4U()
        {
            try
            {
                var res = _analyzeService.Chart_4U();
                if (!res)
                    return;

                var dic = new Dictionary<string, Stream>();
                var lAccount = _accountRepo.GetByFilter(Builders<Account>.Filter.Gt(x => x.status, 0));
                foreach (var item in lAccount)
                {
                    if (item.list is null)
                        item.list = new List<string>();

                    var lStream = new List<Stream>();
                    foreach (var itemMaCK in item.list)
                    {
                        try
                        {
                            if (dic.ContainsKey(itemMaCK))
                            {
                                lStream.Add(dic[itemMaCK]);
                                continue;
                            }
                            var stream = await _chartService.Chart_ThongKeKhopLenh(itemMaCK);
                            if (stream != null)
                            {
                                lStream.Add(stream);
                                dic.Add(itemMaCK, stream);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"ChartService.Chart_4U|ACCOUNT| {item.u}|EXCEPTION| {ex.Message}");
                        }
                    }

                    if (lStream.Any())
                    {
                        foreach (var itemStream in lStream)
                        {
                            await _teleService.SendPhoto(item.u, itemStream);
                            Thread.Sleep(500);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ChartService.Chart_4U|EXCEPTION| {ex.Message}");
            }
        }

        #endregion

        private List<Stock> StockInstance()
        {
            if (StaticVal._lStock != null && StaticVal._lStock.Any())
                return StaticVal._lStock;
            StaticVal._lStock = _stockRepo.GetAll();
            _commonService.GetCurrentTime();

            return StaticVal._lStock;
        }
    }
}
