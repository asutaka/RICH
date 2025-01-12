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

        private const long _idGroup = -4237476810;
        private const long _idChannel = -1002247826353;
        private const long _idUser = 1066022551;

        public Worker(ILogger<Worker> logger, 
                    ITeleService teleService, IBaoCaoPhanTichService bcptService, IGiaNganhHangService giaService, ITongCucThongKeService tongcucService, IAnalyzeService analyzeService,
                    ITuDoanhService tudoanhService, IStockRepo stockRepo)
        {
            _logger = logger;
            _bcptService = bcptService;
            _teleService = teleService;
            _giaService = giaService;
            _tongcucService = tongcucService;
            _analyzeService = analyzeService;
            _tudoanhService = tudoanhService;

            _stockRepo = stockRepo;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tmp = await _giaService.TraceGia(false);
            return;
            StockInstance();

            while (!stoppingToken.IsCancellationRequested)
            {
                var dt = DateTime.Now;

                var isDayOfWork = dt.DayOfWeek >= DayOfWeek.Monday && dt.DayOfWeek <= DayOfWeek.Friday;//Từ thứ 2 đến thứ 6
                var isPreTrade = dt.Hour < 9;
                var isTimePrint = dt.Minute >= 15 && dt.Minute < 30;//từ phút thứ 15 đến phút thứ 30
                var isRealTime = (dt.Hour >= 9 && dt.Hour < 12) || (dt.Hour >= 13 && dt.Hour < 15);//từ 9h đến 3h
                var bcpt = await _bcptService.BaoCaoPhanTich();
                if(!string.IsNullOrWhiteSpace(bcpt))
                {
                    await _teleService.SendMessage(_idGroup, bcpt);
                }
                //Tổng cục thống kê
                if (dt.Day == 6)
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
                            await _teleService.SendMessage(_idGroup, res.Item2);
                        }
                    }
                    else if (dt.Hour == 17)
                    {
                        var res = await _giaService.TraceGia(true);
                        if (res.Item1 > 0)
                        {
                            await _teleService.SendMessage(_idGroup, res.Item2);
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
                                await _teleService.SendMessage(_idChannel, mes);
                            }
                        }

                        if (dt.Hour == 14 && dt.Minute >= 45)
                        {
                            var mes = await _analyzeService.ThongKeGDNN_NhomNganh();
                            if (!string.IsNullOrWhiteSpace(mes))
                            {
                                await _teleService.SendMessage(_idChannel, mes);
                            }
                            var res = await _analyzeService.ChiBaoKyThuat(dt);
                            if (res.Item1 > 0)
                            {
                                await _teleService.SendMessage(_idChannel, res.Item2);
                            }
                        }

                        if(dt.Hour >= 15)
                        {
                            var res = await _tudoanhService.TuDoanhHSX();
                            if (res.Item1 > 0)
                            {
                                await _teleService.SendMessage(_idChannel, res.Item2);
                            }
                        }
                    }
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
