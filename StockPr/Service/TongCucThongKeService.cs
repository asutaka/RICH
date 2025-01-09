using MongoDB.Driver;
using OfficeOpenXml;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPr.Service
{
    public interface ITongCucThongKeService
    {
        Task<(int, string)> TongCucThongKe(DateTime dtNow);
    }
    public class TongCucThongKeService : ITongCucThongKeService
    {
        private readonly ILogger<TongCucThongKeService> _logger;
        private readonly IAPIService _apiService;
        private readonly IConfigDataRepo _configRepo;
        private readonly IThongKeRepo _thongkeRepo;
        public TongCucThongKeService(ILogger<TongCucThongKeService> logger,
                                    IAPIService apiService,
                                    IConfigDataRepo configRepo,
                                    IThongKeRepo thongkeRepo)
        {
            _logger = logger;
            _apiService = apiService;
            _configRepo = configRepo;
            _thongkeRepo = thongkeRepo;
        }

        public async Task<(int, string)> TongCucThongKe(DateTime dtNow)
        {
            try
            {
                var url = await _apiService.TongCucThongKeGetUrl();
                var year = dtNow.Year;
                var index = url.IndexOf($".{year}");
                if (index == -1)
                {
                    year = dtNow.Year - 1;
                    index = url.IndexOf($".{year}");
                }
                if (index == -1)
                    return (0, null);
                var monthStr = url.Substring(index - 2, 2).Replace("T", "");
                var month = Math.Abs(int.Parse(monthStr));
                var t = long.Parse($"{year}{month.To2Digit()}");
                var dtLocal = new DateTime(year, month, 28);

                var mode = EConfigDataType.TongCucThongKeThang;
                var builder = Builders<ConfigData>.Filter;
                FilterDefinition<ConfigData> filter = builder.Eq(x => x.ty, (int)mode);
                var lConfig = _configRepo.GetByFilter(filter);
                if (lConfig.Any())
                {
                    if (lConfig.Any(x => x.t == t))
                        return (0, null);
                }

                var res = await TongCucThongKeParsingData(url);
                if (res)
                {
                    //var mes = TongCucThongKeThangPrint(dtLocal);
                    //return (1, mes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"TongCucThongKeService.TongCucThongKe|EXCEPTION| {ex.Message}");
            }

            return (0, null);
        }

        private async Task<bool> TongCucThongKeParsingData(string url)
        {
            try
            {
                var mode = EConfigDataType.TongCucThongKeThang;
                var dtNow = DateTime.Now;
                var year = dtNow.Year;
                var urlCheck = url.Replace("-", ".");
                var index = urlCheck.IndexOf($".{year}");
                if (index == -1)
                {
                    year = dtNow.Year - 1;
                    index = urlCheck.IndexOf($".{year}");
                }
                if (index == -1)
                    return false;
                var isInt = int.TryParse(urlCheck.Substring(index - 2, 2).Replace("T", ""), out var month);
                if (!isInt)
                {
                    for (int i = 1; i <= 12; i++)
                    {
                        if (urlCheck.IndexOf($"{i.To2Digit()}/") > -1)
                        {
                            month = i;
                            break;
                        }
                    }
                }
                var dt = new DateTime(year, month, 28);
                var stream = await _apiService.TongCucThongKeGetFile(url);
                if (stream is null
                   || stream.Length < 1000)
                {
                    return false;
                }

                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                var package = new ExcelPackage(stream);
                var lSheet = package.Workbook.Worksheets;
                bool isIIP = false, isVonDauTu = false, isFDI = false, isBanLe = false, isCPI = false, isVantaiHK = false, isVanTaiHH = false, isSPCN = false, isXK = false, isNK = false;
                foreach (var sheet in lSheet)
                {
                    if (false) { }
                    else if (!isIIP && new List<string> { "IIP", "IIPThang" }.Any(x => sheet.Name.RemoveSpace().RemoveSignVietnamese().ToUpper().EndsWith(x.RemoveSpace().ToUpper())))
                    {
                        isIIP = true;
                        IIP(sheet, dt);
                    }
                    else if (!isVonDauTu && new List<string> { "VDT", "Von Dau Tu", "NSNN", "NSNN Thang", "Von DT" }.Any(x => sheet.Name.RemoveSpace().RemoveSignVietnamese().ToUpper().EndsWith(x.RemoveSpace().ToUpper())))
                    {
                        isVonDauTu = true;
                        VonDauTuNhaNuoc(sheet, dt);
                    }
                    else if (!isFDI && new List<string> { "FDI" }.Any(x => sheet.Name.RemoveSpace().RemoveSignVietnamese().ToUpper().EndsWith(x.RemoveSpace().ToUpper())))
                    {
                        isFDI = true;
                        FDI(sheet, dt);
                    }
                    else if (!isBanLe && new List<string> { "Tongmuc" }.Any(x => sheet.Name.RemoveSpace().RemoveSignVietnamese().ToUpper().EndsWith(x.RemoveSpace().ToUpper())))
                    {
                        isBanLe = true;
                        BanLe(sheet, dt);
                    }
                    else if (!isCPI && new List<string> { "CPI" }.Any(x => sheet.Name.RemoveSpace().RemoveSignVietnamese().ToUpper().EndsWith(x.RemoveSpace().ToUpper())))
                    {
                        isCPI = true;
                        CPI(sheet, dt);
                    }
                    else if (!isXK && new List<string> { "XK", "Xuat Khau", "XK hh", "XK hang hoa", "XK Thang" }.Any(x => sheet.Name.RemoveSpace().RemoveSignVietnamese().ToUpper().EndsWith(x.RemoveSpace().ToUpper())))
                    {
                        isXK = true;
                        XuatKhau(sheet, dt);
                    }
                    else if (!isVantaiHK && new List<string> { "VT HK", "Hanh Khach", "VanTai HK", "Van Tai HK" }.Any(x => sheet.Name.RemoveSpace().RemoveSignVietnamese().ToUpper().EndsWith(x.RemoveSpace().ToUpper())))
                    {
                        isVantaiHK = true;
                        VanTaiHanhKhach(sheet, dt);
                    }
                    else if (!isVanTaiHH && new List<string> { "VT HH", "Hang Hoa", "VanTai HH", "Van Tai HH" }.Any(x => sheet.Name.RemoveSpace().RemoveSignVietnamese().ToUpper().EndsWith(x.RemoveSpace().ToUpper())))
                    {
                        isVanTaiHH = true;
                        VanTaiHangHoa(sheet, dt);
                    }
                }

                var builder = Builders<ConfigData>.Filter;
                FilterDefinition<ConfigData> filter = builder.Eq(x => x.ty, (int)mode);
                var t = long.Parse($"{dt.Year}{dt.Month.To2Digit()}");
                var lConfig = _configRepo.GetByFilter(filter);
                var last = lConfig.LastOrDefault();
                if (last is null)
                {
                    _configRepo.InsertOne(new ConfigData
                    {
                        ty = (int)mode,
                        t = t
                    });
                }
                else
                {
                    last.t = t;
                    _configRepo.Update(last);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.TongCucThongKeParsingData|EXCEPTION| {ex.Message}");
            }
            return false;
        }

        private void IIP(ExcelWorksheet sheet, DateTime dt)
        {
            var cQoQPrev = 2;
            var cQoQoY = 3;
            var cQoQ = 4;

            if (dt.Month == 1)
            {
                cQoQPrev = -1;
                cQoQoY = 2;
                cQoQ = 3;
            }
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.IIP_Dien, dt, sheet, colContent: 1, colVal: -1, colQoQ: cQoQ, colQoQoY: cQoQoY, colUnit: -1, colPrice: -1, colValPrev: -1, colQoQPrev: cQoQPrev, textCompare: "Phan Phoi Dien");
        }

        private void VonDauTuNhaNuoc(ExcelWorksheet sheet, DateTime dt)
        {
            var cValPrev = 3;
            var cVal = 4;
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.DauTuCong, dt, sheet, colContent: 1, colVal: cVal, colQoQ: -1, colQoQoY: -1, colUnit: -1, colPrice: -1, colValPrev: cValPrev, colQoQPrev: -1, "Tong So");
        }

        private void FDI(ExcelWorksheet sheet, DateTime dt)
        {
            InsertThongKeSomeRecord(EKeyTongCucThongKe.FDI, dt, sheet, colContent: 2, colVal: 4, colQoQ: -1, colQoQoY: -1, colUnit: -1, keyStart: "Dia Phuong", colKeyStart: 1, keyEnd: "Lanh Tho", colKeyEnd: 1);
        }

        private void BanLe(ExcelWorksheet sheet, DateTime dt)
        {
            var cContent = 1;
            var cValPrev = 2;
            var cVal = 3;
            var cQoQ = 6;
            if (dt.Month == 1 || dt.Month % 3 == 0)
            {
                cContent = 2;
                cValPrev = 3;
                cVal = 4;
                cQoQ = 6;
            }

            var res = InsertThongKeOnlyRecord(EKeyTongCucThongKe.BanLe, dt, sheet, colContent: cContent, colVal: cVal, colQoQ: cQoQ, colQoQoY: -1, colUnit: -1, colPrice: -1, colValPrev: cValPrev, colQoQPrev: -1, "Ban Le");
        }

        private void CPI(ExcelWorksheet sheet, DateTime dt)
        {
            var cQoQ = 5;
            var cQoQoY = 7;
            if (dt.Month == 1)
            {
                cQoQ = 5;
                cQoQoY = 6;
            }

            InsertThongKeOnlyRecord(EKeyTongCucThongKe.CPI_GiaTieuDung, dt, sheet, colContent: 1, colVal: -1, colQoQ: cQoQ, colQoQoY: cQoQoY, colUnit: -1, colPrice: -1, colValPrev: -1, colQoQPrev: -1, "Chi So Gia Tieu Dung");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.CPI_GiaVang, dt, sheet, colContent: 1, colVal: -1, colQoQ: cQoQ, colQoQoY: cQoQoY, colUnit: -1, colPrice: -1, colValPrev: -1, colQoQPrev: -1, "Chi So Gia Vang");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.CPI_DoLa, dt, sheet, colContent: 1, colVal: -1, colQoQ: cQoQ, colQoQoY: cQoQoY, colUnit: -1, colPrice: -1, colValPrev: -1, colQoQPrev: -1, "Do La");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.CPI_LamPhat, dt, sheet, colContent: 1, colVal: -1, colQoQ: cQoQ, colQoQoY: cQoQoY, colUnit: -1, colPrice: -1, colValPrev: -1, colQoQPrev: -1, "Lam Phat");
        }

        private void VanTaiHanhKhach(ExcelWorksheet sheet, DateTime dt)
        {
            var cContent = 1;
            var cVal = 2;
            var cQoQoY = 4;
            var cQoQ = 5;
            if (dt.Month == 1 || dt.Month % 3 == 0)
            {
                cContent = 2;
                cVal = 3;
                cQoQoY = 4;
                cQoQ = 5;
            }

            var resHangKhong = InsertThongKeOnlyRecord(EKeyTongCucThongKe.HanhKhach_HangKhong, dt, sheet, colContent: cContent, colVal: cVal, colQoQ: cQoQ, colQoQoY: cQoQoY, colUnit: -1, colPrice: -1, colValPrev: -1, colQoQPrev: -1, "Hang Khong");
        }

        private void VanTaiHangHoa(ExcelWorksheet sheet, DateTime dt)
        {
            var cContent = 1;
            var cVal = 2;
            var cQoQoY = 4;
            var cQoQ = 5;
            if (dt.Month == 1 || dt.Month % 3 == 0)
            {
                cContent = 2;
                cVal = 3;
                cQoQoY = 4;
                cQoQ = 5;
            }
            var resDuongBien = InsertThongKeOnlyRecord(EKeyTongCucThongKe.VanTai_DuongBien, dt, sheet, colContent: cContent, colVal: cVal, colQoQ: cQoQ, colQoQoY: cQoQoY, colUnit: -1, colPrice: -1, colValPrev: -1, colQoQPrev: -1, "Duong Bien");
            var resDuongBo = InsertThongKeOnlyRecord(EKeyTongCucThongKe.VanTai_DuongBo, dt, sheet, colContent: cContent, colVal: cVal, colQoQ: cQoQ, colQoQoY: cQoQoY, colUnit: -1, colPrice: -1, colValPrev: -1, colQoQPrev: -1, "Duong Bo");
            var resHangKhong = InsertThongKeOnlyRecord(EKeyTongCucThongKe.VanTai_HangKhong, dt, sheet, colContent: cContent, colVal: cVal, colQoQ: cQoQ, colQoQoY: cQoQoY, colUnit: -1, colPrice: -1, colValPrev: -1, colQoQPrev: -1, "Hang Khong");
        }

        private void XuatKhau(ExcelWorksheet sheet, DateTime dt)
        {
            var cVal = 4;
            var cPrice = 3;
            var cQoQ = 10;
            if (dt.Month == 1)
            {
                cVal = 7;
                cPrice = 6;
                cQoQ = 10;
            }

            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_ThuySan, dt, sheet, colContent: 2, colVal: cVal, colQoQ: cQoQ, colQoQoY: -1, colUnit: -1, colPrice: cPrice, colValPrev: -1, colQoQPrev: -1, "Thuy San");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_CaPhe, dt, sheet, colContent: 2, colVal: cVal, colQoQ: cQoQ, colQoQoY: -1, colUnit: -1, colPrice: cPrice, colValPrev: -1, colQoQPrev: -1, "Ca Phe");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_Gao, dt, sheet, colContent: 2, colVal: cVal, colQoQ: cQoQ, colQoQoY: -1, colUnit: -1, colPrice: cPrice, colValPrev: -1, colQoQPrev: -1, "Gao");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_Ximang, dt, sheet, colContent: 2, colVal: cVal, colQoQ: cQoQ, colQoQoY: -1, colUnit: -1, colPrice: cPrice, colValPrev: -1, colQoQPrev: -1, "Xi mang");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_HoaChat, dt, sheet, colContent: 2, colVal: cVal, colQoQ: cQoQ, colQoQoY: -1, colUnit: -1, colPrice: cPrice, colValPrev: -1, colQoQPrev: -1, "Hoa chat", textIgnore: "san pham");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_SPChatDeo, dt, sheet, colContent: 2, colVal: cVal, colQoQ: cQoQ, colQoQoY: -1, colUnit: -1, colPrice: cPrice, colValPrev: -1, colQoQPrev: -1, "Tu chat deo");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_CaoSu, dt, sheet, colContent: 2, colVal: cVal, colQoQ: cQoQ, colQoQoY: -1, colUnit: -1, colPrice: cPrice, colValPrev: -1, colQoQPrev: -1, "Cao su");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_Go, dt, sheet, colContent: 2, colVal: cVal, colQoQ: cQoQ, colQoQoY: -1, colUnit: -1, colPrice: cPrice, colValPrev: -1, colQoQPrev: -1, "Go");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_DetMay, dt, sheet, colContent: 2, colVal: cVal, colQoQ: cQoQ, colQoQoY: -1, colUnit: -1, colPrice: cPrice, colValPrev: -1, colQoQPrev: -1, "Det may");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_SatThep, dt, sheet, colContent: 2, colVal: cVal, colQoQ: cQoQ, colQoQoY: -1, colUnit: -1, colPrice: cPrice, colValPrev: -1, colQoQPrev: -1, "Sat thep", textIgnore: "san pham");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_DayDien, dt, sheet, colContent: 2, colVal: cVal, colQoQ: cQoQ, colQoQoY: -1, colUnit: -1, colPrice: cPrice, colValPrev: -1, colQoQPrev: -1, "Day dien");
        }

        private bool InsertThongKeOnlyRecord(EKeyTongCucThongKe eThongKe, DateTime dt, ExcelWorksheet sheet, int colContent, int colVal, int colQoQ, int colQoQoY, int colUnit, int colPrice, int colValPrev, int colQoQPrev, string textCompare, string textIgnore = "")
        {
            try
            {
                if (colContent <= 0)
                    return false;

                var unitStr = string.Empty;
                //loop all rows in the sheet
                for (int i = sheet.Dimension.Start.Row; i <= sheet.Dimension.End.Row; i++)
                {
                    var valContent = sheet.Cells[i, colContent].Value?.ToString().Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(valContent))
                        continue;

                    if (!valContent.RemoveSpace().RemoveSignVietnamese().ToUpper().Replace(",", "").Contains(textCompare.RemoveSpace().RemoveSignVietnamese().ToUpper()))
                        continue;

                    if (!string.IsNullOrWhiteSpace(textIgnore) && valContent.RemoveSpace().RemoveSignVietnamese().ToUpper().Replace(",", "").Contains(textIgnore.RemoveSpace().RemoveSignVietnamese().ToUpper()))
                        continue;

                    var model = new ThongKe
                    {
                        d = int.Parse($"{dt.Year}{dt.Month.To2Digit()}"),
                        key = (int)eThongKe
                    };
                    model.content = valContent;

                    if (colVal > 0)
                    {
                        var valStr = sheet.Cells[i, colVal].Value?.ToString().Trim() ?? string.Empty;
                        var isDouble = double.TryParse(valStr.Replace(",", ""), out var val);
                        model.va = isDouble ? Math.Round(val, 1) : 0;
                    }

                    if (colQoQ > 0)
                    {
                        var valStr = sheet.Cells[i, colQoQ].Value?.ToString().Trim() ?? string.Empty;
                        var isDouble = double.TryParse(valStr.Replace(",", ""), out var val);
                        model.qoq = isDouble ? Math.Round(val, 1) : 0;
                    }

                    if (colQoQoY > 0)
                    {
                        var valStr = sheet.Cells[i, colQoQoY].Value?.ToString().Trim() ?? string.Empty;
                        var isDouble = double.TryParse(valStr.Replace(",", ""), out var val);
                        model.qoqoy = isDouble ? Math.Round(val, 1) : 0;
                    }

                    if (colUnit > 0)
                    {
                        var valStr = sheet.Cells[i, colUnit].Value?.ToString().Trim() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(valStr.Replace("'", "").Replace("\"", "")))
                        {
                            valStr = unitStr;
                        }
                        model.unit = valStr;
                        unitStr = valStr;
                    }

                    if (colPrice > 0)
                    {
                        var valStr = sheet.Cells[i, colPrice].Value?.ToString().Trim() ?? string.Empty;
                        var isDouble = double.TryParse(valStr.Replace(",", ""), out var val);
                        if (val > 0)
                        {
                            model.price = Math.Round(model.va * 1000 / val, 1);
                        }
                    }

                    if (colValPrev > 0)
                    {
                        var valStr = sheet.Cells[i, colValPrev].Value?.ToString().Trim() ?? string.Empty;
                        var isDouble = double.TryParse(valStr.Replace(",", ""), out var val);
                        var va = isDouble ? Math.Round(val, 1) : 0;
                        if (va > 0)
                        {
                            var dtPrev = dt.AddMonths(-1);
                            FilterDefinition<ThongKe> filter = null;
                            var builder = Builders<ThongKe>.Filter;
                            var lFilter = new List<FilterDefinition<ThongKe>>()
                            {
                                builder.Eq(x => x.d, int.Parse($"{dtPrev.Year}{dtPrev.Month.To2Digit()}")),
                                builder.Eq(x => x.key, (int)eThongKe)
                            };
                            foreach (var item in lFilter)
                            {
                                if (filter is null)
                                {
                                    filter = item;
                                    continue;
                                }
                                filter &= item;
                            }

                            var entityPrev = _thongkeRepo.GetEntityByFilter(filter);
                            if (entityPrev != null)
                            {
                                entityPrev.va = va;
                                _thongkeRepo.Update(entityPrev);
                            }
                        }
                    }

                    if (colQoQPrev > 0)
                    {
                        var valStr = sheet.Cells[i, colQoQPrev].Value?.ToString().Trim() ?? string.Empty;
                        var isDouble = double.TryParse(valStr.Replace(",", ""), out var val);
                        var va = isDouble ? Math.Round(val, 1) : 0;
                        if (va > 0)
                        {
                            var dtPrev = dt.AddMonths(-1);
                            FilterDefinition<ThongKe> filter = null;
                            var builder = Builders<ThongKe>.Filter;
                            var lFilter = new List<FilterDefinition<ThongKe>>()
                            {
                                builder.Eq(x => x.d, int.Parse($"{dtPrev.Year}{dtPrev.Month.To2Digit()}")),
                                builder.Eq(x => x.key, (int)eThongKe)
                            };
                            foreach (var item in lFilter)
                            {
                                if (filter is null)
                                {
                                    filter = item;
                                    continue;
                                }
                                filter &= item;
                            }

                            var entityPrev = _thongkeRepo.GetEntityByFilter(filter);
                            if (entityPrev != null)
                            {
                                entityPrev.qoq = va;
                                _thongkeRepo.Update(entityPrev);
                            }
                        }
                    }

                    if (model.va <= 0 && model.qoq <= 0 && model.qoqoy <= 0)
                        continue;

                    _thongkeRepo.InsertOne(model);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"TongCucThongKeService.InsertThongKeOnlyRecord|EXCEPTION| {ex.Message}");
            }
            return false;
        }

        private bool InsertThongKeSomeRecord(EKeyTongCucThongKe eThongKe, DateTime dt, ExcelWorksheet sheet, int colContent, int colVal, int colQoQ, int colQoQoY, int colUnit, string keyStart, int colKeyStart, string keyEnd, int colKeyEnd)
        {
            try
            {
                var unitStr = string.Empty;
                var isStart = false;
                //loop all rows in the sheet
                for (int i = sheet.Dimension.Start.Row; i <= sheet.Dimension.End.Row; i++)
                {
                    if (!isStart)
                    {
                        var valKeyStart = sheet.Cells[i, colKeyStart].Value?.ToString().Trim() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(valKeyStart))
                            continue;

                        if (valKeyStart.RemoveSpace().RemoveSignVietnamese().ToUpper().Contains(keyStart.RemoveSpace().ToUpper()))
                        {
                            isStart = true;
                        }
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(keyEnd))
                    {
                        var valKeyEnd = sheet.Cells[i, colKeyEnd].Value?.ToString().Trim() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(valKeyEnd))
                        {
                            if (valKeyEnd.RemoveSpace().RemoveSignVietnamese().ToUpper().Contains(keyEnd.RemoveSpace().ToUpper()))
                            {
                                return true;
                            }
                        }
                    }

                    var model = new ThongKe
                    {
                        d = int.Parse($"{dt.Year}{dt.Month.To2Digit()}"),
                        key = (int)eThongKe
                    };

                    if (colContent > 0)
                    {
                        var valStr = sheet.Cells[i, colContent].Value?.ToString().Trim() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(valStr))
                            continue;

                        model.content = valStr;
                    }

                    if (colVal > 0)
                    {
                        var valStr = sheet.Cells[i, colVal].Value?.ToString().Trim() ?? string.Empty;
                        var isDouble = double.TryParse(valStr.Replace(",", ""), out var val);
                        model.va = isDouble ? Math.Round(val, 1) : 0;
                    }

                    if (colQoQ > 0)
                    {
                        var valStr = sheet.Cells[i, colQoQ].Value?.ToString().Trim() ?? string.Empty;
                        var isDouble = double.TryParse(valStr.Replace(",", ""), out var val);
                        model.qoq = isDouble ? Math.Round(val, 1) : 0;
                    }

                    if (colQoQoY > 0)
                    {
                        var valStr = sheet.Cells[i, colQoQoY].Value?.ToString().Trim() ?? string.Empty;
                        var isDouble = double.TryParse(valStr.Replace(",", ""), out var val);
                        model.qoqoy = isDouble ? Math.Round(val, 1) : 0;
                    }

                    if (colUnit > 0)
                    {
                        var valStr = sheet.Cells[i, colUnit].Value?.ToString().Trim() ?? string.Empty;//
                        if (string.IsNullOrWhiteSpace(valStr.Replace("'", "").Replace("\"", "")))
                        {
                            valStr = unitStr;
                        }
                        model.unit = valStr;
                        unitStr = valStr;
                    }

                    if (model.va <= 0 && model.qoq <= 0 && model.qoqoy <= 0)
                        continue;

                    _thongkeRepo.InsertOne(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.InsertThongKeSomeRecord|EXCEPTION| {ex.Message}");
            }
            return true;
        }
    }
}
