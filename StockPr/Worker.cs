﻿using StockPr.DAL;
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
        private readonly IGiaNganhHangService _giaService;
        private readonly ITeleService _teleService;
        private readonly ITongCucThongKeService _tongcucService;
        private readonly IAnalyzeService _analyzeService;
        private readonly ITuDoanhService _tudoanhService;
        private readonly IBaoCaoTaiChinhService _bctcService;
        private readonly IPortfolioService _portfolioService;
        private readonly IEPSRankService _epsService;
        private readonly IF319Service _f319Service;
        private readonly ITestService _testService;
        private readonly IChartService _chartService;
        private readonly INewsService _newsService;
        private readonly ICommonService _commonService;

        private const long _idGroup = -4237476810;
        private const long _idGroupF319 = -4739186506;
        private const long _idChannel = -1002247826353;
        private const long _idChannelNews = -1002709849640;
        private const long _idUser = 1066022551;

        public Worker(ILogger<Worker> logger, 
                    ITeleService teleService, IBaoCaoPhanTichService bcptService, IGiaNganhHangService giaService, ITongCucThongKeService tongcucService, 
                    IAnalyzeService analyzeService, ITuDoanhService tudoanhService, IBaoCaoTaiChinhService bctcService, IStockRepo stockRepo, 
                    IPortfolioService portfolioService, IEPSRankService epsService, ITestService testService, IF319Service f319Service, 
                    ICommonService commonService, INewsService newsService, IChartService chartService)
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
            _portfolioService = portfolioService;
            _epsService = epsService;
            _testService = testService;
            _f319Service = f319Service;
            _commonService = commonService;
            _newsService = newsService;
            _chartService = chartService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            StockInstance();
            //////for Test
            //await _testService.CheckWycKoff();
            //return;
            while (!stoppingToken.IsCancellationRequested)
            {
                var dt = DateTime.Now;

                var isDayOfWork = dt.DayOfWeek >= DayOfWeek.Monday && dt.DayOfWeek <= DayOfWeek.Friday;//Từ thứ 2 đến thứ 6
                var isPreTrade = dt.Hour < 9;
                var isTimePrint = dt.Minute >= 15 && dt.Minute < 30;//từ phút thứ 15 đến phút thứ 30
                var isRealTime = (dt.Hour >= 9 && dt.Hour < 12) || (dt.Hour >= 13 && dt.Hour < 15);//từ 9h đến 3h
                //Báo cáo phân tích
                var lBCPT = await _bcptService.BaoCaoPhanTich();
                if (lBCPT.Item1?.Any() ?? false)
                {
                    foreach (var item in lBCPT.Item1)
                    {
                        await _teleService.SendMessage(_idGroup, item.content, item.link);
                        Thread.Sleep(200);
                    }
                }
                if (lBCPT.Item2?.Any() ?? false)
                {
                    var mes = string.Join("\n", lBCPT.Item2);
                    await _teleService.SendMessage(_idUser, mes);
                    Thread.Sleep(200);
                }

                var f319 = await _f319Service.F319KOL();
                if(f319.Any())
                {
                    foreach (var item in f319)
                    {
                        await _teleService.SendMessage(_idGroupF319, item.Message, true);
                        Thread.Sleep(200);
                    }
                }

                var news = await _newsService.GetNews();
                if (news.Any())
                {
                    foreach (var item in news)
                    {
                        await _teleService.SendMessage(_idChannelNews, item, true);
                        Thread.Sleep(200);
                    }
                }
                //if(dt.Day == 1)
                //{
                //    var eps = await _epsService.RankEPS(DateTime.Now);
                //    if (eps.Item1 > 0)
                //    {
                //        await _teleService.SendMessage(_idGroup, eps.Item2, true);
                //    }
                //}

                //Quỹ đầu tư
                var portfolio = await _portfolioService.Portfolio();
                if (!string.IsNullOrWhiteSpace(portfolio.Item1))
                {
                    await _teleService.SendMessage(_idChannel, portfolio.Item1, portfolio.Item2);
                }
                //Tổng cục thống kê
                if (dt.Day == 6 && dt.Hour >= 8 && dt.Hour < 11)
                {
                    var res = await _tongcucService.TongCucThongKe(dt);
                    if (res.Item1 > 0)
                    {
                        await _teleService.SendMessage(_idChannel, res.Item2);
                    }
                }
                //Giá Ngành Hàng
                if (dt.Minute < 15)
                {
                    if (dt.Hour == 9 || dt.Hour == 13)
                    {
                        var res = await _giaService.TraceGia(false);
                        if(res.Item1 > 0)
                        {
                            await _teleService.SendMessage(_idGroup, res.Item2, true);
                        }
                    }
                    else if (dt.Hour == 17)
                    {
                        var res = await _giaService.TraceGia(true);
                        if (res.Item1 > 0)
                        {
                            await _teleService.SendMessage(_idGroup, res.Item2, true);
                        }
                    }
                }

                if (isDayOfWork 
                    && !StaticVal._lNghiLe.Any(x => x.Month == dt.Month && x.Day == dt.Day))
                {
                    if (!isPreTrade)
                    {
                        if (isRealTime && isTimePrint)
                        {
                            var mes = await _analyzeService.Realtime();
                            if (!string.IsNullOrWhiteSpace(mes))
                            {
                                await _teleService.SendMessage(_idChannel, mes, true);
                            }
                        }

                        if ((dt.Hour == 14 && dt.Minute >= 45)
                            || (dt.Hour == 15 && dt.Minute <= 30))
                        {
                            var mes = await _analyzeService.ThongKeGDNN_NhomNganh();
                            if (!string.IsNullOrWhiteSpace(mes))
                            {
                                await _teleService.SendMessage(_idChannel, mes, true);
                                //chart
                                var stream = await _chartService.Chart_ThongKeKhopLenh();
                                if (stream != null)
                                {
                                    await _teleService.SendPhoto(_idChannel, stream);
                                }
                            }

                            var res = await _analyzeService.ChiBaoKyThuat(dt, true);
                            if (res.Item1 > 0)
                            {
                                await _teleService.SendMessage(_idChannel, res.Item2, true);
                            }
                            if (!string.IsNullOrWhiteSpace(res.Item3))
                            {
                                await _teleService.SendMessage(_idUser, res.Item3);
                            }
                            if (!string.IsNullOrWhiteSpace(res.Item4))
                            {
                                await _teleService.SendMessage(_idUser, res.Item4);
                            }
                        }

                        if(dt.Hour == 19)
                        {
                            var mes = await _analyzeService.ThongKeTuDoanh();
                            if (!string.IsNullOrWhiteSpace(mes))
                            {
                                await _teleService.SendMessage(_idChannel, mes, true);
                            }
                        }
                    }
                    if(dt.Hour == 11 && dt.Minute >= 30 && dt.Minute < 45)
                    {
                        var gdnn = await _analyzeService.ThongkeForeign_PhienSang(dt);
                        if (gdnn.Item1 > 0)
                        {
                            await _teleService.SendMessage(_idChannel, gdnn.Item2, true);
                        }
                    }  
                    if(dt.Hour == 14 && dt.Minute >= 0 && dt.Minute < 15)
                    {
                        var res = await _analyzeService.ChiBaoKyThuat(dt, false);
                        if (!string.IsNullOrWhiteSpace(res.Item3))
                        {
                            await _teleService.SendMessage(_idUser, res.Item3.Replace("ngày", "cuối phiên ngày"));
                        }
                    }
                }

                //if (dt.Hour == 23
                //    && ((dt.Month % 3 == 1 && dt.Day >= 15) || dt.Month % 3 == 2 && dt.Day == 10))  
                //{
                //    var isValid = await _bctcService.CheckVietStockToken();
                //    if (isValid)
                //    {
                //        await _bctcService.SyncBCTC();
                //    }
                //    else
                //    {
                //        await _teleService.SendMessage(_idUser, $"[VietStock] Token is Expired");
                //    }
                //}
                await Task.Delay(1000 * 60 * 15, stoppingToken);
            }
        }

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
