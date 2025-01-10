﻿using MongoDB.Driver;
using Newtonsoft.Json;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Model;
using StockPr.Utils;
using Telegram.Bot.Types;

namespace StockPr.Service
{
    public interface IChartService
    {
        Task<List<InputFileStream>> Chart_MaCK(string input);
    }
    public class ChartService : IChartService
    {
        private readonly ILogger<ChartService> _logger;
        private readonly IFinancialRepo _financialRepo;
        private readonly ICommonService _commonService;
        private readonly IAPIService _apiService;
        private readonly IThongKeRepo _thongkeRepo;
        private readonly IThongKeQuyRepo _thongkequyRepo;
        public ChartService(ILogger<ChartService> logger, 
            ICommonService commonService, IAPIService apiService,  
            IFinancialRepo financialRepo, IThongKeRepo thongKeRepo, IThongKeQuyRepo thongKeQuyRepo) 
        {
            _logger = logger;
            _commonService = commonService;
            _apiService = apiService;
            _financialRepo = financialRepo;
            _thongkeRepo = thongKeRepo;
            _thongkequyRepo = thongKeQuyRepo;
        }

        public async Task<List<InputFileStream>> Chart_MaCK(string input)
        {
            var lRes = new List<InputFileStream>();
            try
            {
                var stock = StaticVal._lStock.FirstOrDefault(x => x.s == input);
                if (stock == null)
                    return null;

                var lFinancial = _financialRepo.GetByFilter(Builders<Financial>.Filter.Eq(x => x.s, input));
                if (!lFinancial.Any())
                    return null;

                var lStream = new List<Stream>();
                var streamDoanhThu = await Chart_DoanhThu_LoiNhuan(lFinancial.Select(x => new BaseFinancialDTO { d = x.d, rv = x.rv, pf = x.pf }).ToList(), input);
                lStream.Add(streamDoanhThu);

                if (stock.IsTonKho())
                {
                    var stream = await Chart_TonKho(lFinancial, input);
                    lStream.Add(stream);
                }

                if (stock.IsNguoiMua())
                {
                    var stream = await Chart_NguoiMua(lFinancial, input);
                    lStream.Add(stream);
                }

                if (stock.IsFDI())
                {
                    var stream = await Chart_FDI();
                    lStream.Add(stream);
                }

                if (stock.IsXNK())
                {
                    var stream = await Chart_XNK(stock);
                    lStream.Add(stream);
                }

                if (stock.IsBanLe())
                {
                    var stream = await Chart_ThongKe_BanLe();
                    lStream.Add(stream);
                }

                if (stock.IsHangKhong())
                {
                    var stream = await Chart_ThongKeQuy_HangKhong();
                    lStream.Add(stream);
                }

                if (stock.IsCaoSu())
                {
                    var stream = await Chart_ThongKeQuy_HangKhong();
                    lStream.Add(stream);
                }

                if (stock.IsNganHang())
                {
                    lStream.AddRange(await Chart_NganHang(input));
                }

                if (stock.IsChungKhoan())
                {
                    lStream.AddRange(await Chart_ChungKhoan(input));
                }

                lRes = lStream.Select(x => InputFile.FromStream(x)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ChartService.Chart_MaCK|EXCEPTION| {ex.Message}");
            }
            return lRes;
        }

        public async Task<Stream> Chart_DoanhThu_LoiNhuan(List<BaseFinancialDTO> lFinancial, string code)
        {
            try
            {
                var time = _commonService.GetCurrentTime();
                var lTangTruong = new List<double>();
                foreach (var item in lFinancial)
                {
                    double tangTruong = 0;
                    var prevQuarter = item.d.GetYoyQuarter();
                    var prev = lFinancial.FirstOrDefault(x => x.d == prevQuarter);
                    if (prev is not null && prev.pf != 0)
                    {
                        tangTruong = Math.Round(100 * (-1 + item.pf / prev.pf), 1);
                        if (item.pf > prev.pf)
                        {
                            tangTruong = Math.Abs(tangTruong);
                        }
                        if (tangTruong >= StaticVal._MaxRate)
                        {
                            tangTruong = StaticVal._MaxRate;
                        }
                        if (tangTruong <= -StaticVal._MaxRate)
                        {
                            tangTruong = -StaticVal._MaxRate;
                        }
                    }

                    lTangTruong.Add(tangTruong);
                }
                var lTake = lFinancial.TakeLast(StaticVal._TAKE);

                var lSeries = new List<HighChartSeries_BasicColumn>
                {
                    new HighChartSeries_BasicColumn
                    {
                        data = lTake.Select(x => x.rv),
                        name = "Doanh thu",
                        type = "column",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}" },
                        color = "#012060"
                    },
                     new HighChartSeries_BasicColumn
                    {
                        data = lTake.Select(x => x.pf),
                        name = "Lợi nhuận",
                        type = "column",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}" },
                        color = "#C00000"
                    },
                    new HighChartSeries_BasicColumn
                    {
                        data = lTangTruong.TakeLast(StaticVal._TAKE),
                        name = "Tăng trưởng lợi nhuận",
                        type = "spline",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}%" },
                        color = "#C00000",
                        yAxis = 1,
                    }
                };

                return await Chart_BasicBase($"{code} - Doanh thu, Lợi nhuận Quý {time.Item3}/{time.Item2} (QoQ)", lTake.Select(x => x.d.GetNameQuarter()).ToList(), lSeries);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ChartService.Chart_BDS_DoanhThu_LoiNhuan|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private async Task<Stream> Chart_TonKho(List<Financial> lFinancial, string code)
        {
            try
            {
                var time = _commonService.GetCurrentTime();
                var lTangTruong = new List<double>();
                foreach (var item in lFinancial)
                {
                    double tangTruong = 0;
                    var prevQuarter = item.d.GetPrevQuarter();
                    var prev = lFinancial.FirstOrDefault(x => x.d == prevQuarter);
                    if (prev is not null && prev.inv > 0)
                    {
                        tangTruong = Math.Round(100 * (-1 + item.inv / prev.inv), 1);
                    }

                    lTangTruong.Add(tangTruong);
                }
                var lTake = lFinancial.TakeLast(StaticVal._TAKE);

                var lSeries = new List<HighChartSeries_BasicColumn>
                {
                    new HighChartSeries_BasicColumn
                    {
                        data = lTake.Select(x => x.inv),
                        name = "Tồn kho",
                        type = "column",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}" },
                        color = "#012060"
                    },
                    new HighChartSeries_BasicColumn
                    {
                        data = lTangTruong.TakeLast(StaticVal._TAKE),
                        name = "Tăng trưởng tồn kho",
                        type = "spline",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}%" },
                        color = "#C00000",
                        yAxis = 1,
                    }
                };

                return await Chart_BasicBase($"{code} - Tồn kho Quý {time.Item3}/{time.Item2} (QoQoY)", lTake.Select(x => x.d.GetNameQuarter()).ToList(), lSeries);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ChartService.Chart_TonKho|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private async Task<Stream> Chart_NguoiMua(List<Financial> lFinancial, string code)
        {
            try
            {
                var time = _commonService.GetCurrentTime();
                var lTangTruong = new List<double>();
                foreach (var item in lFinancial)
                {
                    double tangTruong = 0;
                    var prevQuarter = item.d.GetPrevQuarter();
                    var prev = lFinancial.FirstOrDefault(x => x.d == prevQuarter);
                    if (prev is not null && prev.bp > 0)
                    {
                        tangTruong = Math.Round(100 * (-1 + item.bp / prev.bp), 1);
                    }

                    lTangTruong.Add(tangTruong);
                }
                var lTake = lFinancial.TakeLast(StaticVal._TAKE);

                var lSeries = new List<HighChartSeries_BasicColumn>
                {
                    new HighChartSeries_BasicColumn
                    {
                        data = lTake.Select(x => x.bp),
                        name = "Người mua trả tiền trước",
                        type = "column",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}" },
                        color = "#012060"
                    },
                    new HighChartSeries_BasicColumn
                    {
                        data = lTangTruong.TakeLast(StaticVal._TAKE),
                        name = "Tăng trưởng người mua",
                        type = "spline",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}%" },
                        color = "#C00000",
                        yAxis = 1,
                    }
                };

                return await Chart_BasicBase($"{code} - Người mua trả tiền trước Quý {time.Item3}/{time.Item2} (QoQoY)", lTake.Select(x => x.d.GetNameQuarter()).ToList(), lSeries);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ChartService.Chart_NguoiMua|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private async Task<Stream> Chart_XNK(Stock stock)
        {
            var eThongKe = EKeyTongCucThongKe.None;
            var strTitle = string.Empty;
            if (stock.cat.Any(x => x.ty == (int)EStockType.Thep))
            {
                eThongKe = EKeyTongCucThongKe.XK_SatThep;
                strTitle = "sắt thép";
            }
            if (stock.cat.Any(x => x.ty == (int)EStockType.Than))
            {
                strTitle = "than";
            }
            if (stock.cat.Any(x => x.ty == (int)EStockType.PhanBon))
            {
                strTitle = "phân bón";
            }
            if (stock.cat.Any(x => x.ty == (int)EStockType.HoaChat))
            {
                eThongKe = EKeyTongCucThongKe.XK_HoaChat;
                strTitle = "hóa chất";
            }
            if (stock.cat.Any(x => x.ty == (int)EStockType.Go))
            {
                eThongKe = EKeyTongCucThongKe.XK_Go;
                strTitle = "gỗ";
            }
            if (stock.cat.Any(x => x.ty == (int)EStockType.Gao))
            {
                eThongKe = EKeyTongCucThongKe.XK_Gao;
                strTitle = "gạo";
            }
            if (stock.cat.Any(x => x.ty == (int)EStockType.XiMang))
            {
                eThongKe = EKeyTongCucThongKe.XK_Ximang;
                strTitle = "xi măng";
            }
            if (stock.cat.Any(x => x.ty == (int)EStockType.CaPhe))
            {
                eThongKe = EKeyTongCucThongKe.XK_CaPhe;
                strTitle = "cà phê";
            }
            if (stock.cat.Any(x => x.ty == (int)EStockType.CaoSu))
            {
                eThongKe = EKeyTongCucThongKe.XK_CaoSu;
                strTitle = "cao su";
            }

            var lThongKe = _thongkeRepo.GetByFilter(Builders<ThongKe>.Filter.Eq(x => x.key, (int)eThongKe)).OrderBy(x => x.d);
            var lThongKeQuy = _thongkequyRepo.GetByFilter(Builders<ThongKe>.Filter.Eq(x => x.key, (int)eThongKe)).OrderBy(x => x.d);

            var lastThongKe = lThongKe?.LastOrDefault() ?? new ThongKe();//202301
            var lastThongKeQuy = lThongKeQuy?.LastOrDefault() ?? new ThongKe();//20231

            var yearThongKe = lastThongKe.d / 100;
            var yearThongKeQuy = lastThongKeQuy.d / 10;

            var monthThongKe = lastThongKe.d - yearThongKe * 100;
            var monthThongKeQuy = (lastThongKeQuy.d - yearThongKeQuy * 10) * 3;

            var dayThongKe = 27;
            var dayThongKeQuy = 28;

            var dThongKe = new DateTime(yearThongKe < 1 ? 1 : yearThongKe, monthThongKe < 1 ? 1 : monthThongKe, dayThongKe < 1 ? 1 : dayThongKe);
            var dThongKeQuy = new DateTime(yearThongKeQuy < 1 ? 1 : yearThongKeQuy, monthThongKeQuy < 1 ? 1 : monthThongKeQuy, dayThongKeQuy < 1 ? 1 : dayThongKeQuy);

            var isNK = eThongKe.ToString().Contains("NK");
            var lData = new List<(double, double, string)>();

            if (dThongKe > dThongKeQuy)
            {
                lData = lThongKe.Select(x => (x.va, x.price, x.d.GetNameMonth())).ToList();
            }
            else
            {
                lData = lThongKeQuy.Select(x => (x.va, x.price, x.d.GetNameQuarter())).ToList();
            }
            return await Chart_XNK(lData, !isNK, strTitle, string.Empty, string.Empty);
        }

        private async Task<Stream> Chart_XNK(IEnumerable<(double, double, string)> lVal, bool isXK, string title, string unit1, string unit2)
        {
            try
            {
                var strMode = isXK ? "xuất khẩu" : "nhập khẩu";
                var lSeries = new List<HighChartSeries_BasicColumn>
                {
                    new HighChartSeries_BasicColumn
                    {
                        data = lVal.TakeLast(25).Select(x => x.Item1),
                        name = $"Giá trị {strMode} {title}",
                        type = "column",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}" },
                        color = "#012060"
                    }
                };

                if (lVal.Sum(x => x.Item2) > 0)
                {
                    lSeries.Add(new HighChartSeries_BasicColumn
                    {
                        data = lVal.TakeLast(25).Select(x => x.Item1),
                        name = $"Giá {title}",
                        type = "spline",
                        dataLabels = new HighChartDataLabel { enabled = true, format = "{point.y:.1f}" },
                        color = "#C00000",
                        yAxis = 1
                    });
                }

                return await Chart_BasicBase($"{strMode} {title}", lVal.TakeLast(25).Select(x => x.Item3).ToList(), lSeries, $"giá trị: {unit1}", $"giá trị: {unit2}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"ChartService.Chart_XNK|EXCEPTION| {ex.Message}");
            }

            return null;
        }

        private async Task<Stream> Chart_ThongKe_BanLe()
        {
            try
            {
                var lBanLe = _thongkeRepo.GetByFilter(Builders<ThongKe>.Filter.Eq(x => x.key, (int)EKeyTongCucThongKe.BanLe)).OrderBy(x => x.d);
                var lSeries = new List<HighChartSeries_BasicColumn>
                {
                    new HighChartSeries_BasicColumn
                    {
                        data = lBanLe.TakeLast(StaticVal._TAKE).Select(x => Math.Round(x.va/1000, 1)),
                        name = "Tổng mức bán lẻ",
                        type = "column",
                        dataLabels = new HighChartDataLabel { enabled = true, format = "{point.y:.1f}" },
                        color = "#012060"
                    },
                    new HighChartSeries_BasicColumn
                    {
                        data = lBanLe.TakeLast(StaticVal._TAKE).Select(x => x.y - 100),
                        name = "So với cùng kỳ",
                        type = "spline",
                        dataLabels = new HighChartDataLabel { enabled = true, format = "{point.y:.1f}" },
                        color = "#C00000",
                        yAxis = 1
                    }
                };

                return await Chart_BasicBase($"Tổng mức bán lẻ so với cùng kỳ năm ngoái(QoQ)", lBanLe.TakeLast(StaticVal._TAKE).Select(x => x.d.GetNameMonth()).ToList(), lSeries, "giá trị: nghìn tỷ", "giá trị: %");
            }
            catch (Exception ex)
            {
                _logger.LogError($"ChartService.Chart_BanLe|EXCEPTION| {ex.Message}");
            }

            return null;
        }

        private async Task<Stream> Chart_ThongKeQuy_HangKhong()
        {
            try
            {
                var lThongKe = _thongkequyRepo.GetByFilter(Builders<ThongKe>.Filter.Eq(x => x.key, (int)EKeyTongCucThongKe.QUY_GiaVT_HangKhong)).OrderBy(x => x.d);
                var lSeries = new List<HighChartSeries_BasicColumn>
                {
                    new HighChartSeries_BasicColumn
                    {
                        data = lThongKe.TakeLast(StaticVal._TAKE).Select(x => x.y - 100),
                        name = "So với cùng kỳ",
                        type = "spline",
                        dataLabels = new HighChartDataLabel { enabled = true, format = "{point.y:.1f}" },
                        color = "#C00000",
                        yAxis = 1
                    }
                };

                return await Chart_BasicBase($"Giá vận tải hàng không quý so với cùng kỳ năm ngoái(QoQ)", lThongKe.TakeLast(StaticVal._TAKE).Select(x => x.d.GetNameQuarter()).ToList(), lSeries, "Đơn vị: %", "Đơn vị: %");
            }
            catch (Exception ex)
            {
                _logger.LogError($"ChartService.Chart_ThongKeQuy_HangKhong|EXCEPTION| {ex.Message}");
            }

            return null;
        }

        private async Task<Stream> Chart_FDI()
        {
            try
            {
                var lFDI = _thongkeRepo.GetByFilter(Builders<ThongKe>.Filter.Eq(x => x.key, (int)EKeyTongCucThongKe.FDI));
                var maxD = lFDI.MaxBy(x => x.d).d;
                lFDI = lFDI.Where(x => x.d == maxD).OrderByDescending(x => x.va).Take(StaticVal._TAKE).ToList();

                var lSeries = new List<HighChartSeries_BasicColumn>
                {
                    new HighChartSeries_BasicColumn
                    {
                        data = lFDI.Select(x => Math.Round(x.va/1000, 1)),
                        name = "Vốn đăng ký",
                        type = "column",
                        dataLabels = new HighChartDataLabel { enabled = true, format = "{point.y:.1f}" },
                        color = "#012060"
                    }
                };

                return await Chart_BasicBase($"Vốn FDI đăng ký", lFDI.Select(x => x.content).ToList(), lSeries, "giá trị: tỷ USD", "giá trị: %");
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.Chart_FDI|EXCEPTION| {ex.Message}");
            }

            return null;
        }

        private async Task<Stream> Chart_BasicBase(string title, List<string> lCat, List<HighChartSeries_BasicColumn> lSerie, string titleX = null, string titleY = null)
        {
            try
            {
                var basicColumn = new HighchartBasicColumn(title, lCat, lSerie);
                var strX = string.IsNullOrWhiteSpace(titleX) ? "(Đơn vị: tỷ)" : titleX;
                var strY = string.IsNullOrWhiteSpace(titleY) ? "(Tỉ lệ: %)" : titleY;

                basicColumn.yAxis = new List<HighChartYAxis> { new HighChartYAxis { title = new HighChartTitle { text = strX }, labels = new HighChartLabel{ format = "{value}" } },
                                                                 new HighChartYAxis { title = new HighChartTitle { text = strY }, labels = new HighChartLabel{ format = "{value}" }, opposite = true }};

                var chart = new HighChartModel(JsonConvert.SerializeObject(basicColumn));
                var body = JsonConvert.SerializeObject(chart);
                return await _apiService.GetChartImage(body);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ChartService.Chart_BasicBase|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        #region Ngân hàng
        private async Task<List<Stream>> Chart_NganHang(string code)
        {
            var lFinancial = _financialRepo.GetByFilter(Builders<Financial>.Filter.Eq(x => x.s, code));
            if (!lFinancial.Any())
                return null;

            var lOutput = new List<Stream>();

            lFinancial = lFinancial.OrderBy(x => x.d).ToList();
            var streamTangTruongTinDung = await Chart_NganHang_TangTruongTinDung(lFinancial, code);
            var streamNoXau = await Chart_NganHang_NoXau(lFinancial, code);
            var streamNim = await Chart_NganHang_NimCasaChiPhiVon(lFinancial, code);
            var streamDoanhThu = await Chart_DoanhThu_LoiNhuan(lFinancial.Select(x => new BaseFinancialDTO { d = x.d, rv = x.rv, pf = x.pf }).ToList(), code);
            lOutput.Add(streamTangTruongTinDung);
            lOutput.Add(streamNoXau);
            lOutput.Add(streamNim);
            lOutput.Add(streamDoanhThu);
            return lOutput;
        }
        private async Task<Stream> Chart_NganHang_TangTruongTinDung(List<Financial> lFinancial, string code)
        {
            try
            {
                var time = _commonService.GetCurrentTime();
                var lTake = lFinancial.TakeLast(StaticVal._TAKE);

                var basicColumn = new HighchartTangTruongTinDung($"{code} - Tăng trưởng tín dụng Quý {time.Item3}/{time.Item2} (YoY)", lTake.Select(x => x.d.GetNameQuarter()).ToList(), new List<HighChartSeries_TangTruongTinDung>
                {
                    new HighChartSeries_TangTruongTinDung
                    {
                        name="Room tín dụng",
                        type = "column",
                        data = lTake.Select(x => x.room ?? 0).ToList(),
                        color = "rgba(158, 159, 163, 0.5)",
                        pointPlacement = -0.2,
                        dataLabels = new HighChartDataLabel()
                    },
                    new HighChartSeries_TangTruongTinDung
                    {
                        name="Tăng trưởng tín dụng",
                        type = "column",
                        data = lTake.Select(x => x.credit_r ?? 0).ToList(),
                        color = "#012060",
                        dataLabels = new HighChartDataLabel()
                    }
                });
                var strTitleYAxis = "(Đơn vị: %)";
                basicColumn.yAxis = new List<HighChartYAxis> { new HighChartYAxis { title = new HighChartTitle { text = strTitleYAxis }, labels = new HighChartLabel{ format = "{value}" } },
                                                                 new HighChartYAxis { title = new HighChartTitle { text = string.Empty }, labels = new HighChartLabel{ format = "{value} %" }, opposite = true }};

                var chart = new HighChartModel(JsonConvert.SerializeObject(basicColumn));
                var body = JsonConvert.SerializeObject(chart);
                return await _apiService.GetChartImage(body);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ChartService.Chart_NganHang_TangTruongTinDung|EXCEPTION| {ex.Message}");
            }
            return null;
        }
        private async Task<Stream> Chart_NganHang_NoXau(List<Financial> lFinancial, string code)
        {
            try
            {
                var time = _commonService.GetCurrentTime();

                var yearPrev = time.Item2;
                var quarterPrev = time.Item3;
                if (time.Item3 > 1)
                {
                    quarterPrev--;
                }
                else
                {
                    quarterPrev = 4;
                    yearPrev--;
                }

                var lBaoPhu = new List<double>();
                var lTiLeNo = new List<double>();
                var lTangTruongNoNhom2 = new List<double>();
                var lTangTruongTrichLap = new List<double>();
                foreach (var item in lFinancial)
                {
                    double baophu = 0, tileNoxau = 0, tangTruongTrichLap = 0, tangTruongNoNhom2 = 0;
                    var noxau = item.debt3 + item.debt4 + item.debt5;
                    if (noxau > 0)
                    {
                        baophu = Math.Round(100 * (item.risk ?? 0) / noxau, 1);
                    }
                    //if (item.debt > 0)
                    //{
                    //    tileNoxau = Math.Round((float)(100 * noxau) / item.debt, 1);
                    //}

                    var prev = lFinancial.FirstOrDefault(x => x.d == item.d.GetPrevQuarter());
                    if (prev != null)
                    {
                        //if (prev.risk > 0)
                        //{
                        //    tangTruongTrichLap = Math.Round(100 * (-1 + (item.risk ?? 0) / (prev.risk ?? 1)), 1);
                        //}
                        if (prev.debt2 > 0)
                        {
                            tangTruongNoNhom2 = Math.Round(100 * (-1 + (float)item.debt2 / prev.debt2), 1);
                        }
                    }

                    //tang truong tin dung, room tin dung
                    lBaoPhu.Add(baophu);
                    //lTiLeNo.Add(tileNoxau);
                    lTangTruongNoNhom2.Add(tangTruongNoNhom2);
                    //lTangTruongTrichLap.Add(tangTruongTrichLap);
                }
                var lSeries = new List<HighChartSeries_BasicColumn>
                {
                     new() {
                        data = lBaoPhu.TakeLast(StaticVal._TAKE),
                        name = "Bao phủ nợ xấu",
                        type = "column",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}%" },
                        color = "#012060"
                    },
                    //new()
                    //{
                    //    data = lTiLeNo.TakeLast(StaticVal._TAKE),
                    //    name = "Tỉ lệ nợ xấu",
                    //    type = "spline",
                    //    dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}%" },
                    //    color = "#C00000",
                    //    yAxis = 1,
                    //},
                    new()
                    {
                        data = lTangTruongNoNhom2.TakeLast(StaticVal._TAKE),
                        name = "Tăng trưởng nợ nhóm 2",
                        type = "spline",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}%" },
                        color = "#ffbf00",
                        yAxis = 1
                    },
                    //new()
                    //{
                    //    data = lTangTruongTrichLap.TakeLast(StaticVal._TAKE),
                    //    name = "Tăng trưởng trích lập",
                    //    type = "spline",
                    //    dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}%" },
                    //    color = "rgba(158, 159, 163, 0.5)",
                    //    yAxis = 1
                    //}
                };
                return await Chart_BasicBase($"{code} - Nợ xấu Quý {time.Item3}/{time.Item2} (QoQoY)", lFinancial.TakeLast(StaticVal._TAKE).Select(x => x.d.GetNameQuarter()).ToList(), lSeries, "(Bao phủ nợ xấu: %)", "(Tăng trưởng: %)");
            }
            catch (Exception ex)
            {
                _logger.LogError($"ChartService.Chart_NganHang_NoXau|EXCEPTION| {ex.Message}");
            }
            return null;
        }
        private async Task<Stream> Chart_NganHang_NimCasaChiPhiVon(List<Financial> lFinancial, string code)
        {
            try
            {
                var time = _commonService.GetCurrentTime();

                var lNim = new List<double>();
                var lCasa = new List<double>();
                var lCir = new List<double>();
                var lChiPhiVon = new List<double>();
                foreach (var item in lFinancial)
                {
                    //tang truong tin dung, room tin dung
                    lNim.Add(item.nim_r ?? 0);
                    lCasa.Add(item.casa_r ?? 0);
                    lCir.Add(item.cir_r ?? 0);
                    lChiPhiVon.Add(item.cost_r ?? 0);
                }
                var lSeries = new List<HighChartSeries_BasicColumn>
                {
                    new HighChartSeries_BasicColumn
                    {
                        data = lNim.TakeLast(StaticVal._TAKE),
                        name = "NIM",
                        type = "column",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}%" },
                        color = "#012060"
                    },
                    new HighChartSeries_BasicColumn
                    {
                        data = lCasa.TakeLast(StaticVal._TAKE),
                        name = "CASA",
                        type = "spline",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}%" },
                        color = "#C00000",
                        yAxis = 1,
                    },
                    new HighChartSeries_BasicColumn
                    {
                        data = lCir.TakeLast(StaticVal._TAKE),
                        name = "CIR",
                        type = "spline",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}%" },
                        color = "#ffbf00",
                        yAxis = 1,
                    },
                    new HighChartSeries_BasicColumn
                    {
                        data = lChiPhiVon.TakeLast(StaticVal._TAKE),
                        name = "Tăng trưởng chi phí vốn",
                        type = "spline",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}%" },
                        color = "rgba(158, 159, 163, 0.5)",
                        yAxis = 1
                    }
                };
                return await Chart_BasicBase($"{code} - NIM, CASA, CIR, Chi phí vốn Quý {time.Item3}/{time.Item2}", lFinancial.TakeLast(StaticVal._TAKE).Select(x => x.d.GetNameQuarter()).ToList(), lSeries, "(NIM: %)");
            }
            catch (Exception ex)
            {
                _logger.LogError($"ChartService.Chart_NganHang_NimCasaChiPhiVon|EXCEPTION| {ex.Message}");
            }
            return null;
        }
        #endregion

        #region Chứng khoán
        private async Task<List<Stream>> Chart_ChungKhoan(string code)
        {
            var lFinancial = _financialRepo.GetByFilter(Builders<Financial>.Filter.Eq(x => x.s, code));
            if (!lFinancial.Any())
                return null;

            var lOutput = new List<Stream>();

            lFinancial = lFinancial.OrderBy(x => x.d).ToList();
            var streamDoanhThu = await Chart_DoanhThu_LoiNhuan(lFinancial.Select(x => new BaseFinancialDTO { d = x.d, rv = x.rv, pf = x.pf }).ToList(), code);
            lOutput.Add(streamDoanhThu);

            var streamTangTruongTinDung = await Chart_CK_TangTruongTinDung_RoomTinDung(lFinancial, code);
            lOutput.Add(streamTangTruongTinDung);

            var streamTuDoanh = await Chart_CK_TuDoanh(lFinancial, code);
            lOutput.Add(streamTuDoanh);

            var streamMoiGioi = await Chart_CK_MoiGioi(lFinancial, code);
            lOutput.Add(streamMoiGioi);

            return lOutput;
        }
        private async Task<Stream> Chart_CK_TangTruongTinDung_RoomTinDung(List<Financial> lFinancial, string code)
        {
            try
            {
                var lMargin = new List<double>();
                var lVonChu = new List<double>();
                var lMarginTrenVonChu = new List<double>();
                var lTangTruongMargin = new List<double>();
                var time = _commonService.GetCurrentTime();
                foreach (var item in lFinancial)
                {
                    //tang truong tin dung, room tin dung
                    lMargin.Add(item.debt);
                    lVonChu.Add(item.eq);
                    double marginTrenVonChu = 0;
                    if (item.eq > 0)
                    {
                        marginTrenVonChu = Math.Round(item.debt * 100 / item.eq, 1);
                    }
                    lMarginTrenVonChu.Add(marginTrenVonChu);
                    //
                    var prev = lFinancial.FirstOrDefault(x => x.d == item.d.GetPrevQuarter());
                    double tangTruongMargin = 0;
                    if (prev is not null && prev.idebt != 0)
                    {
                        tangTruongMargin = Math.Round(100 * (-1 + item.idebt / prev.idebt), 1);
                    }
                    lTangTruongMargin.Add(tangTruongMargin);
                }

                var basicColumn = new HighchartTangTruongTinDung($"{code} - Tăng trưởng Margin Quý {time.Item3}/{time.Item2} (QoQoY)", lFinancial.TakeLast(StaticVal._TAKE).Select(x => x.d.GetNameQuarter()).ToList(), new List<HighChartSeries_TangTruongTinDung>
                {
                    new HighChartSeries_TangTruongTinDung
                    {
                        name="Vốn chủ sở hữu",
                        type = "column",
                        data = lVonChu.TakeLast(StaticVal._TAKE).ToList(),
                        color = "rgba(158, 159, 163, 0.5)",
                        pointPlacement = -0.2,
                        dataLabels = new HighChartDataLabel()
                    },
                    new HighChartSeries_TangTruongTinDung
                    {
                        name="Dư nợ Margin",
                        type = "column",
                        data = lMargin.TakeLast(StaticVal._TAKE).ToList(),
                        color = "#012060",
                        dataLabels = new HighChartDataLabel()
                    },
                    new HighChartSeries_TangTruongTinDung
                    {
                        name="Tăng trưởng lãi Margin",
                        type = "spline",
                        data = lTangTruongMargin.TakeLast(StaticVal._TAKE).ToList(),
                        color = "#C00000",
                        dataLabels = new HighChartDataLabel(),
                        yAxis = 1
                    },
                    new HighChartSeries_TangTruongTinDung
                    {
                        name="Margin trên vốn chủ",
                        type = "spline",
                        data = lMarginTrenVonChu.TakeLast(StaticVal._TAKE).ToList(),
                        color = "#ffbf00",
                        dataLabels = new HighChartDataLabel(),
                        yAxis = 1
                    }
                });
                basicColumn.yAxis = new List<HighChartYAxis> { new HighChartYAxis { title = new HighChartTitle { text = "(Đơn vị: tỷ)" }, labels = new HighChartLabel{ format = "{value}" } },
                                                                 new HighChartYAxis { title = new HighChartTitle { text = "(Đơn vị: %)" }, labels = new HighChartLabel{ format = "{value} %" }, opposite = true }};

                var chart = new HighChartModel(JsonConvert.SerializeObject(basicColumn));
                var body = JsonConvert.SerializeObject(chart);
                return await _apiService.GetChartImage(body);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ChartService.Chart_CK_TangTruongTinDung_RoomTinDung|EXCEPTION| {ex.Message}");
            }
            return null;
        }
        private async Task<Stream> Chart_CK_TuDoanh(List<Financial> lFinancial, string code)
        {
            try
            {
                var time = _commonService.GetCurrentTime();

                var lTaiSanTuDoanh = new List<double>();
                var lLoiNhuanTuDoanh = new List<double>();
                var lLoiNhuanTrenTaiSanTuDoanh = new List<double>();
                foreach (var item in lFinancial)
                {
                    double loiNhuanTrenTaiSan = 0;
                    if (item.itrade != 0)
                    {
                        loiNhuanTrenTaiSan = Math.Round(100 * item.trade / item.itrade, 1);
                    }

                    //tang truong tin dung, room tin dung
                    lTaiSanTuDoanh.Add(item.itrade);
                    lLoiNhuanTuDoanh.Add(item.trade);
                    lLoiNhuanTrenTaiSanTuDoanh.Add(loiNhuanTrenTaiSan);
                }

                var lSeries = new List<HighChartSeries_BasicColumn>
                {
                    new HighChartSeries_BasicColumn
                    {
                        data = lTaiSanTuDoanh.TakeLast(StaticVal._TAKE).ToList(),
                        name = "Tài sản tự doanh",
                        type = "column",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}" },
                        color = "#012060"
                    },
                    new HighChartSeries_BasicColumn
                    {
                        data = lLoiNhuanTuDoanh.TakeLast(StaticVal._TAKE).ToList(),
                        name = "Lợi nhuận tự doanh",
                        type = "column",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}" },
                        color = "#C00000"
                    },
                    new HighChartSeries_BasicColumn
                    {
                        data = lLoiNhuanTrenTaiSanTuDoanh.TakeLast(StaticVal._TAKE).ToList(),
                        name = "Biên lợi nhuận trên tài sản",
                        type = "spline",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}%" },
                        color = "#ffbf00",
                        yAxis = 1,
                    }
                };

                return await Chart_BasicBase($"{code} - Thống kê tự doanh {time.Item3}/{time.Item2}", lFinancial.TakeLast(StaticVal._TAKE).Select(x => x.d.GetNameQuarter()).ToList(), lSeries);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ChartService.Chart_CK_TuDoanh|EXCEPTION| {ex.Message}");
            }
            return null;
        }
        private async Task<Stream> Chart_CK_MoiGioi(List<Financial> lFinancial, string code)
        {
            try
            {
                var time = _commonService.GetCurrentTime();

                var lDoanhThuMoiGioi = new List<double>();
                var lLoiNhuanMoiGioi = new List<double>();
                var lBienLoiNhuanMoiGioi = new List<double>();
                foreach (var item in lFinancial)
                {
                    double loiNhuanMoiGioi = item.broker - item.bcost;
                    double bienLoiNhuan = 0;
                    if (item.broker != 0)
                    {
                        bienLoiNhuan = Math.Round(100 * loiNhuanMoiGioi / item.broker, 1);
                    }

                    //tang truong tin dung, room tin dung
                    lDoanhThuMoiGioi.Add(item.broker);
                    lLoiNhuanMoiGioi.Add(loiNhuanMoiGioi);
                    lBienLoiNhuanMoiGioi.Add(bienLoiNhuan);
                }

                var lSeries = new List<HighChartSeries_BasicColumn>
                {
                    new HighChartSeries_BasicColumn
                    {
                        data = lDoanhThuMoiGioi.TakeLast(StaticVal._TAKE).ToList(),
                        name = "Doanh thu môi giới",
                        type = "column",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}" },
                        color = "#012060"
                    },
                    new HighChartSeries_BasicColumn
                    {
                        data = lLoiNhuanMoiGioi.TakeLast(StaticVal._TAKE).ToList(),
                        name = "Lợi nhuận môi giới",
                        type = "column",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}" },
                        color = "#C00000"
                    },
                    new HighChartSeries_BasicColumn
                    {
                        data = lBienLoiNhuanMoiGioi.TakeLast(StaticVal._TAKE).ToList(),
                        name = "Biên lợi nhuận",
                        type = "spline",
                        dataLabels = new HighChartDataLabel{ enabled = true, format = "{point.y:.1f}%" },
                        color = "#ffbf00",
                        yAxis = 1,
                    }
                };

                return await Chart_BasicBase($"{code} - Thống kê môi giới {time.Item3}/{time.Item2}", lFinancial.TakeLast(StaticVal._TAKE).Select(x => x.d.GetNameQuarter()).ToList(), lSeries);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ChartService.Chart_CK_MoiGioi|EXCEPTION| {ex.Message}");
            }
            return null;
        }
        #endregion
    }
}
