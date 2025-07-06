using MongoDB.Driver;
using OfficeOpenXml;
using StockPr.DAL;
using StockPr.DAL.Entity;
using StockPr.Utils;
using System.Text;

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
                    index = url.IndexOf($"-{year}");
                    if(index == -1)
                    {
                        year = dtNow.Year - 1;
                        index = url.IndexOf($".{year}");
                        if(index == -1)
                        {
                            index = url.IndexOf($"-{year}");
                        }    
                    }
                }
                if (index == -1)
                    return (0, null);
                var monthStr = url.Substring(index - 2, 2).Replace("T", "");
                var isInt = int.TryParse(monthStr, out var month);
                if (!isInt)
                {
                    var strSplit = url.Split('/');
                    foreach (var item in strSplit)
                    {
                        isInt = int.TryParse(item, out var val);
                        if(isInt && val <= 12)
                        {
                            month = val - 1;
                            break;
                        }    
                    }
                }

                if(month <= 0)
                    return (0, null);

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

                var res = await TongCucThongKeParsingData(url, dtNow);
                if (res)
                {
                    var mes = TongCucThongKeThangPrint(dtLocal);
                    return (1, mes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"TongCucThongKeService.TongCucThongKe|EXCEPTION| {ex.Message}");
            }

            return (0, null);
        }

        private async Task<bool> TongCucThongKeParsingData(string url, DateTime dtNow)
        {
            try
            {
                var mode = EConfigDataType.TongCucThongKeThang;
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
                bool isIIP = false, isVonDauTu = false, isFDI = false, isBanLe = false, isCPI = false, isVantaiHK = false, isVanTaiHH = false, isXK = false;
                foreach (var sheet in lSheet)
                {
                    if (false) { }
                    else if (!isBanLe && new List<string> { "Tongmuc", "Tongmuc OK" }.Any(x => sheet.Name.RemoveSpace().RemoveNumber().RemoveSignVietnamese().ToUpper().EndsWith(x.RemoveSpace().RemoveNumber().ToUpper())))
                    {
                        isBanLe = true;
                        BanLe(sheet, dt);
                    }
                    else if (!isXK && new List<string> { "XK", "Xuat Khau", "XK hh", "XK hang hoa", "XK Thang" }.Any(x => sheet.Name.RemoveSpace().RemoveNumber().RemoveSignVietnamese().ToUpper().EndsWith(x.RemoveSpace().RemoveNumber().ToUpper())))
                    {
                        isXK = true;
                        XuatKhau(sheet, dt);
                    }
                }
                foreach (var sheet in lSheet.OrderByDescending(x => x.Name))
                {
                    if (false) { }
                    else if (!isIIP && new List<string> { "IIP", "IIPThang" }.Any(x => sheet.Name.RemoveSpace().RemoveNumber().RemoveSignVietnamese().ToUpper().EndsWith(x.RemoveSpace().RemoveNumber().ToUpper())))
                    {
                        isIIP = true;
                        IIP(sheet, dt);
                    }
                    else if (!isVonDauTu && new List<string> { "VDT Thuc hien", "VDT", "Von Dau Tu", "NSNN", "NSNN Thang", "Von DT" }.Any(x => sheet.Name.RemoveSpace().RemoveNumber().RemoveSignVietnamese().ToUpper().EndsWith(x.RemoveSpace().RemoveNumber().ToUpper())))
                    {
                        isVonDauTu = true;
                        VonDauTuNhaNuoc(sheet, dt);
                    }
                    else if (!isFDI && new List<string> { "FDI", "DTNN" }.Any(x => sheet.Name.RemoveSpace().RemoveNumber().RemoveSignVietnamese().ToUpper().EndsWith(x.RemoveSpace().RemoveNumber().ToUpper())))
                    {
                        isFDI = true;
                        FDI(sheet, dt);
                    }
                    else if (!isCPI && new List<string> { "CPI" }.Any(x => sheet.Name.RemoveSpace().RemoveNumber().RemoveSignVietnamese().ToUpper().EndsWith(x.RemoveSpace().RemoveNumber().ToUpper())))
                    {
                        isCPI = true;
                        CPI(sheet, dt);
                    }
                    else if (!isVantaiHK && new List<string> { "VT HK", "Hanh Khach", "VanTai HK", "Van Tai HK", "Van Tai Thang" }.Any(x => sheet.Name.RemoveSpace().RemoveNumber().RemoveSignVietnamese().ToUpper().EndsWith(x.RemoveSpace().RemoveNumber().ToUpper())))
                    {
                        isVantaiHK = true;
                        VanTaiHanhKhach(sheet, dt);
                    }
                    else if (!isVanTaiHH && new List<string> { "VT HH", "Hang Hoa", "VanTai HH", "Van Tai HH", "VTHangHoaThang" }.Any(x => sheet.Name.RemoveSpace().RemoveNumber().RemoveSignVietnamese().ToUpper().EndsWith(x.RemoveSpace().RemoveNumber().ToUpper())))
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

        private string TongCucThongKeThangPrint(DateTime dt)
        {
            var filter = Builders<ThongKe>.Filter.Eq(x => x.d, int.Parse($"{dt.Year}{dt.Month.To2Digit()}"));
            var lData = _thongkeRepo.GetByFilter(filter);
            if (!(lData?.Any() ?? false))
                return string.Empty;

            var strBuilder = new StringBuilder();
            strBuilder.AppendLine($"[Thông báo] Tình hình kinh tế - xã hội tháng {dt.Month}");
            strBuilder.AppendLine();

            strBuilder.AppendLine(CPIStr(dt, lData));
            strBuilder.AppendLine(NhomNganhStr(dt, lData));
            strBuilder.AppendLine(CangbienStr(dt, lData));
            strBuilder.AppendLine(KCNStr(dt, lData));
            strBuilder.AppendLine(XNKStr(dt, lData));
            return strBuilder.ToString();
        }

        #region ParsingData
        private void IIP(ExcelWorksheet sheet, DateTime dt)
        {
            var cYearPrev = 2;
            var cMonth = 3;
            var cYear = 4;

            if (dt.Month == 1)
            {
                cYearPrev = -1;
                cMonth = 2;
                cYear = 3;
            }
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.IIP_Dien, dt, sheet, colContent: 1, colVal: -1, colYear: cYear, colMonth: cMonth, colUnit: -1, colPrice: -1, colValPrev: -1, colYearPrev: cYearPrev, textCompare: "Phan Phoi Dien");
        }

        private void VonDauTuNhaNuoc(ExcelWorksheet sheet, DateTime dt)
        {
            var cValPrev = 3;
            var cVal = 4;
            if (dt.Month == 1)
            {
                cValPrev = -1;    
            }
            
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.DauTuCong, dt, sheet, colContent: 1, colVal: cVal, colYear: -1, colMonth: -1, colUnit: -1, colPrice: -1, colValPrev: cValPrev, colYearPrev: -1, "Tong So");
        }

        private void FDI(ExcelWorksheet sheet, DateTime dt)
        {
            InsertThongKeSomeRecord(EKeyTongCucThongKe.FDI, dt, sheet, colContent: 2, colVal: 4, colVal2: 5, keyStart: "Dia Phuong", colKeyStart: 1, keyEnd: "Lanh Tho", colKeyEnd: 1);
        }

        private void BanLe(ExcelWorksheet sheet, DateTime dt)
        {
            var cContent = 1;
            var cValPrev = 2;
            var cVal = 3;
            var cYear = 6;
            if (dt.Month == 1)
            {
                cContent = 2;
                cValPrev = 3;
                cVal = 4;
                cYear = 6;
            }  

            var res = InsertThongKeOnlyRecord(EKeyTongCucThongKe.BanLe, dt, sheet, colContent: cContent, colVal: cVal, colYear: cYear, colMonth: -1, colUnit: -1, colPrice: -1, colValPrev: cValPrev, colYearPrev: -1, "Ban Le");
            if(res is null)
            {
                cContent++;
                cValPrev++;
                cVal++;
                cYear++;
                InsertThongKeOnlyRecord(EKeyTongCucThongKe.BanLe, dt, sheet, colContent: cContent, colVal: cVal, colYear: cYear, colMonth: -1, colUnit: -1, colPrice: -1, colValPrev: cValPrev, colYearPrev: -1, "Ban Le");
            }
        }

        private void CPI(ExcelWorksheet sheet, DateTime dt)
        {
            var cYear = 5;
            var cMonth = 7;
            if (dt.Month == 12)
            {
                cYear = 4;
                cMonth = 5;
            }
            else if(dt.Month == 1)
            {
                cMonth = 6;
            }

            var lamphat = InsertThongKeOnlyRecord(EKeyTongCucThongKe.CPI_LamPhat, dt, sheet, colContent: 1, colVal: -1, colYear: cYear, colMonth: cMonth, colUnit: -1, colPrice: -1, colValPrev: -1, colYearPrev: -1, "Lam Phat");
            if(lamphat.y == 0)
            {
                cYear--;
                cMonth--;
                InsertThongKeOnlyRecord(EKeyTongCucThongKe.CPI_LamPhat, dt, sheet, colContent: 1, colVal: -1, colYear: cYear, colMonth: cMonth, colUnit: -1, colPrice: -1, colValPrev: -1, colYearPrev: -1, "Lam Phat");
            }

            InsertThongKeOnlyRecord(EKeyTongCucThongKe.CPI_GiaTieuDung, dt, sheet, colContent: 1, colVal: -1, colYear: cYear, colMonth: cMonth, colUnit: -1, colPrice: -1, colValPrev: -1, colYearPrev: -1, "Chi So Gia Tieu Dung");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.CPI_GiaVang, dt, sheet, colContent: 1, colVal: -1, colYear: cYear, colMonth: cMonth, colUnit: -1, colPrice: -1, colValPrev: -1, colYearPrev: -1, "Chi So Gia Vang");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.CPI_DoLa, dt, sheet, colContent: 1, colVal: -1, colYear: cYear, colMonth: cMonth, colUnit: -1, colPrice: -1, colValPrev: -1, colYearPrev: -1, "Do La");
            
        }

        private void VanTaiHanhKhach(ExcelWorksheet sheet, DateTime dt)
        {
            var cContent = 1;
            var cVal = 2;
            var cMonth = 4;
            var cYear = 5;
            if (dt.Month == 1)
            {
                cContent = 2;
                cVal = 3;
            }

            var res = InsertThongKeOnlyRecord(EKeyTongCucThongKe.HanhKhach_HangKhong, dt, sheet, colContent: cContent, colVal: cVal, colYear: cYear, colMonth: cMonth, colUnit: -1, colPrice: -1, colValPrev: -1, colYearPrev: -1, "Hang Khong");
            if (res is null)
            {
                cContent++;
                cMonth++;
                cVal++;
                cYear++;
                InsertThongKeOnlyRecord(EKeyTongCucThongKe.HanhKhach_HangKhong, dt, sheet, colContent: cContent, colVal: cVal, colYear: cYear, colMonth: cMonth, colUnit: -1, colPrice: -1, colValPrev: -1, colYearPrev: -1, "Hang Khong");
            }
        }

        private void VanTaiHangHoa(ExcelWorksheet sheet, DateTime dt)
        {
            var cContent = 1;
            var cVal = 2;
            var cMonth = 4;
            var cYear = 5;
            if (dt.Month == 1)
            {
                cContent = 2;
                cVal = 3;
                cMonth = 4;
                cYear = 5;
            }
            var res =  InsertThongKeOnlyRecord(EKeyTongCucThongKe.VanTai_DuongBien, dt, sheet, colContent: cContent, colVal: cVal, colYear: cYear, colMonth: cMonth, colUnit: -1, colPrice: -1, colValPrev: -1, colYearPrev: -1, "Duong Bien");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.VanTai_DuongBo, dt, sheet, colContent: cContent, colVal: cVal, colYear: cYear, colMonth: cMonth, colUnit: -1, colPrice: -1, colValPrev: -1, colYearPrev: -1, "Duong Bo");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.VanTai_HangKhong, dt, sheet, colContent: cContent, colVal: cVal, colYear: cYear, colMonth: cMonth, colUnit: -1, colPrice: -1, colValPrev: -1, colYearPrev: -1, "Hang Khong");
            if (res is null)
            {
                cContent++;
                cMonth++;
                cVal++;
                cYear++;
                InsertThongKeOnlyRecord(EKeyTongCucThongKe.VanTai_DuongBien, dt, sheet, colContent: cContent, colVal: cVal, colYear: cYear, colMonth: cMonth, colUnit: -1, colPrice: -1, colValPrev: -1, colYearPrev: -1, "Duong Bien");
                InsertThongKeOnlyRecord(EKeyTongCucThongKe.VanTai_DuongBo, dt, sheet, colContent: cContent, colVal: cVal, colYear: cYear, colMonth: cMonth, colUnit: -1, colPrice: -1, colValPrev: -1, colYearPrev: -1, "Duong Bo");
                InsertThongKeOnlyRecord(EKeyTongCucThongKe.VanTai_HangKhong, dt, sheet, colContent: cContent, colVal: cVal, colYear: cYear, colMonth: cMonth, colUnit: -1, colPrice: -1, colValPrev: -1, colYearPrev: -1, "Hang Khong");
            }
        }

        private void XuatKhau(ExcelWorksheet sheet, DateTime dt)
        {
            var cPrice = -1;
            var cValPrev = -1;
            var cVal = 4;
            var cYear = 10;
            if (dt.Month == 1)
            {
                cVal = 7;
                cYear = 10;
            }

            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_ThuySan, dt, sheet, colContent: 2, colVal: cVal, colYear: cYear, colMonth: -1, colUnit: -1, colPrice: cPrice, colValPrev: cValPrev, colYearPrev: -1, "Thuy San");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_CaPhe, dt, sheet, colContent: 2, colVal: cVal, colYear: cYear, colMonth: -1, colUnit: -1, colPrice: cPrice, colValPrev: cValPrev, colYearPrev: -1, "Ca Phe");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_Gao, dt, sheet, colContent: 2, colVal: cVal, colYear: cYear, colMonth: -1, colUnit: -1, colPrice: cPrice, colValPrev: cValPrev, colYearPrev: -1, "Gao");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_Ximang, dt, sheet, colContent: 2, colVal: cVal, colYear: cYear, colMonth: -1, colUnit: -1, colPrice: cPrice, colValPrev: cValPrev, colYearPrev: -1, "Xi mang");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_HoaChat, dt, sheet, colContent: 2, colVal: cVal, colYear: cYear, colMonth: -1, colUnit: -1, colPrice: cPrice, colValPrev: cValPrev, colYearPrev: -1, "Hoa chat", textIgnore: "san pham");
            var res = InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_SPChatDeo, dt, sheet, colContent: 2, colVal: cVal, colYear: cYear, colMonth: -1, colUnit: -1, colPrice: cPrice, colValPrev: cValPrev, colYearPrev: -1, "Tu chat deo");
            if(res is null)
            {
                res = InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_SPChatDeo, dt, sheet, colContent: 2, colVal: cVal, colYear: cYear, colMonth: -1, colUnit: -1, colPrice: cPrice, colValPrev: cValPrev, colYearPrev: -1, "san pham chat deo");
                if(res is null)
                {
                    InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_SPChatDeo, dt, sheet, colContent: 2, colVal: cVal, colYear: cYear, colMonth: -1, colUnit: -1, colPrice: cPrice, colValPrev: cValPrev, colYearPrev: -1, "sp chat deo");
                }
            }
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_CaoSu, dt, sheet, colContent: 2, colVal: cVal, colYear: cYear, colMonth: -1, colUnit: -1, colPrice: cPrice, colValPrev: cValPrev, colYearPrev: -1, "Cao su");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_Go, dt, sheet, colContent: 2, colVal: cVal, colYear: cYear, colMonth: -1, colUnit: -1, colPrice: cPrice, colValPrev: cValPrev, colYearPrev: -1, "Go");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_DetMay, dt, sheet, colContent: 2, colVal: cVal, colYear: cYear, colMonth: -1, colUnit: -1, colPrice: cPrice, colValPrev: cValPrev, colYearPrev: -1, "Det may");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_SatThep, dt, sheet, colContent: 2, colVal: cVal, colYear: cYear, colMonth: -1, colUnit: -1, colPrice: cPrice, colValPrev: cValPrev, colYearPrev: -1, "Sat thep", textIgnore: "san pham");
            InsertThongKeOnlyRecord(EKeyTongCucThongKe.XK_DayDien, dt, sheet, colContent: 2, colVal: cVal, colYear: cYear, colMonth: -1, colUnit: -1, colPrice: cPrice, colValPrev: cValPrev, colYearPrev: -1, "Day dien");
        }

        private ThongKe InsertThongKeOnlyRecord(EKeyTongCucThongKe eThongKe, DateTime dt, ExcelWorksheet sheet, int colContent, int colVal, int colYear, int colMonth, int colUnit, int colPrice, int colValPrev, int colYearPrev, string textCompare, string textIgnore = "")
        {
            try
            {
                if (colContent <= 0)
                    return null;

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

                    if (colYear > 0)
                    {
                        var valStr = sheet.Cells[i, colYear].Value?.ToString().Trim() ?? string.Empty;
                        var isDouble = double.TryParse(valStr.Replace(",", ""), out var val);
                        model.y = isDouble ? Math.Round(val, 1) : 0;
                    }

                    if (colMonth > 0)
                    {
                        var valStr = sheet.Cells[i, colMonth].Value?.ToString().Trim() ?? string.Empty;
                        var isDouble = double.TryParse(valStr.Replace(",", ""), out var val);
                        model.m = isDouble ? Math.Round(val, 1) : 0;
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

                    if (colYearPrev > 0)
                    {
                        var valStr = sheet.Cells[i, colYearPrev].Value?.ToString().Trim() ?? string.Empty;
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
                                entityPrev.y = va;
                                _thongkeRepo.Update(entityPrev);
                            }
                        }
                    }

                    if (model.va <= 0 && model.y <= 0 && model.m <= 0)
                        continue;

                    FilterDefinition<ThongKe> filterCheckModel = null;
                    var builderCheckModel = Builders<ThongKe>.Filter;
                    var lFilterCheckModel = new List<FilterDefinition<ThongKe>>()
                            {
                                builderCheckModel.Eq(x => x.d, model.d),
                                builderCheckModel.Eq(x => x.key, model.key)
                            };
                    foreach (var item in lFilterCheckModel)
                    {
                        if (filterCheckModel is null)
                        {
                            filterCheckModel = item;
                            continue;
                        }
                        filterCheckModel &= item;
                    }
                    var checkModel = _thongkeRepo.GetEntityByFilter(filterCheckModel);
                    if (checkModel is null)
                    {
                        _thongkeRepo.InsertOne(model);
                    }
                    else
                    {
                        checkModel.va = model.va;
                        checkModel.y = model.y;
                        checkModel.m = model.m;
                        checkModel.content = model.content;
                        checkModel.price = model.price;
                        checkModel.unit = model.unit;
                        _thongkeRepo.Update(checkModel);
                    }
                    return model;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"TongCucThongKeService.InsertThongKeOnlyRecord|EXCEPTION| {ex.Message}");
            }
            return null;
        }

        private bool InsertThongKeSomeRecord(EKeyTongCucThongKe eThongKe, DateTime dt, ExcelWorksheet sheet, int colContent, int colVal, int colVal2, string keyStart, int colKeyStart, string keyEnd, int colKeyEnd)
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

                    if(colVal2 > 0)
                    {
                        var valStr = sheet.Cells[i, colVal2].Value?.ToString().Trim() ?? string.Empty;
                        var isDouble = double.TryParse(valStr.Replace(",", ""), out var val);
                        model.va += isDouble ? Math.Round(val, 1) : 0;
                    }    

                    if (model.va <= 0 && model.y <= 0 && model.m <= 0)
                        continue;


                    FilterDefinition<ThongKe> filterCheckModel = null;
                    var builderCheckModel = Builders<ThongKe>.Filter;
                    var lFilterCheckModel = new List<FilterDefinition<ThongKe>>()
                            {
                                builderCheckModel.Eq(x => x.d, model.d),
                                builderCheckModel.Eq(x => x.key, model.key),
                                builderCheckModel.Eq(x => x.content, model.content)
                            };
                    foreach (var item in lFilterCheckModel)
                    {
                        if (filterCheckModel is null)
                        {
                            filterCheckModel = item;
                            continue;
                        }
                        filterCheckModel &= item;
                    }
                    var checkModel = _thongkeRepo.GetEntityByFilter(filterCheckModel);
                    if (checkModel is null)
                    {
                        _thongkeRepo.InsertOne(model);
                    }
                    else
                    {
                        checkModel.va = model.va;
                        checkModel.y = model.y;
                        checkModel.m = model.m;
                        checkModel.content = model.content;
                        checkModel.price = model.price;
                        checkModel.unit = model.unit;
                        _thongkeRepo.Update(checkModel);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"AnalyzeService.InsertThongKeSomeRecord|EXCEPTION| {ex.Message}");
            }
            return true;
        }
        #endregion

        #region Print
        private string CPIStr(DateTime dt, List<ThongKe> lData)
        {
            var strBuilder = new StringBuilder();
            var GiaTieuDung = lData.FirstOrDefault(x => x.key == (int)EKeyTongCucThongKe.CPI_GiaTieuDung);
            var GiaVang = lData.FirstOrDefault(x => x.key == (int)EKeyTongCucThongKe.CPI_GiaVang);
            var GiaUSD = lData.FirstOrDefault(x => x.key == (int)EKeyTongCucThongKe.CPI_DoLa);
            var LamPhat = lData.FirstOrDefault(x => x.key == (int)EKeyTongCucThongKe.CPI_LamPhat);

            strBuilder.AppendLine($"*CPI tháng {dt.Month}:");
            strBuilder.AppendLine($"1. Giá tiêu dùng: M({Math.Round((GiaTieuDung?.m ?? 0) - 100, 1).ToString("#,##0.##")}%)| Y({Math.Round((GiaTieuDung?.y ?? 0) - 100, 1)}%)");
            strBuilder.AppendLine($"2. Giá Vàng:          M({Math.Round((GiaVang?.m ?? 0) - 100, 1).ToString("#,##0.##")}%)| Y({Math.Round((GiaVang?.y ?? 0) - 100, 1)}%)");
            strBuilder.AppendLine($"3. Đô la Mỹ:          M({Math.Round((GiaUSD?.m ?? 0) - 100, 1).ToString("#,##0.##")}%)| Y({Math.Round((GiaUSD?.y ?? 0) - 100, 1)}%)");
            strBuilder.AppendLine($"4. Lạm phát:        M({Math.Round((LamPhat?.m ?? 0), 1).ToString("#,##0.##")}%)| Y({Math.Round((LamPhat?.y ?? 0), 1)}%)");
            return strBuilder.ToString();
        }

        private string NhomNganhStr(DateTime dt, List<ThongKe> lData)
        {
            var strBuilder = new StringBuilder();
            strBuilder.AppendLine($"*Nhóm ngành:");
            strBuilder.AppendLine(DienStr(dt, lData));
            strBuilder.AppendLine(BanleStr(dt, lData));
            strBuilder.AppendLine(DautucongStr(dt, lData));
            return strBuilder.ToString();
        }
        
        private string DienStr(DateTime dt, List<ThongKe> lData)
        {
            FilterDefinition<ThongKe> filterIIP = null;
            var builderIIP = Builders<ThongKe>.Filter;
            var lFilterIIP = new List<FilterDefinition<ThongKe>>()
            {
                builderIIP.Eq(x => x.d, int.Parse($"{dt.Year}{dt.Month.To2Digit()}")),
                builderIIP.Eq(x => x.key, (int)EKeyTongCucThongKe.IIP_Dien),
            };
            foreach (var item in lFilterIIP)
            {
                if (filterIIP is null)
                {
                    filterIIP = item;
                    continue;
                }
                filterIIP &= item;
            }
            var lDataIIP = _thongkeRepo.GetByFilter(filterIIP);
            var Dien = lDataIIP.FirstOrDefault(x => x.content.RemoveSpace().RemoveSignVietnamese().ToUpper().Contains("Phan Phoi Dien".RemoveSpace().ToUpper()));
            return $"1. Điện: M({Math.Round((Dien?.m ?? 0) - 100, 1).ToString("#,##0.##")}%)|Y({Math.Round((Dien?.y ?? 0) - 100, 1).ToString("#,##0.##")}%)";
        }

        private string BanleStr(DateTime dt, List<ThongKe> lData)
        {
            var BanLe = GetDataWithRate(lData, dt, EKeyTongCucThongKe.BanLe);
            return $"2. Bán lẻ: {Math.Round(BanLe.Item1 / 1000, 1).ToString("#,##0.##")} nghìn tỷ|M({BanLe.Item2}%)|Y({BanLe.Item3}%)";
        }

        private string DautucongStr(DateTime dt, List<ThongKe> lData)
        {
            var DauTuCong = GetDataWithRate(lData, dt, EKeyTongCucThongKe.DauTuCong);
            return $"3. Đầu tư công: {Math.Round(DauTuCong.Item1 / 1000, 1).ToString("#,##0.##")} nghìn tỉ|M({DauTuCong.Item2}%)|Y({DauTuCong.Item3}%)";
        }

        private string CangbienStr(DateTime dt, List<ThongKe> lData)
        {
            var strBuilder = new StringBuilder();
            var DuongBien = lData.FirstOrDefault(x => x.key == (int)EKeyTongCucThongKe.VanTai_DuongBien);
            var DuongBo = lData.FirstOrDefault(x => x.key == (int)EKeyTongCucThongKe.VanTai_DuongBo);
            var DuongHangKhong = lData.FirstOrDefault(x => x.key == (int)EKeyTongCucThongKe.VanTai_HangKhong);
            var KhachHangKhong = lData.FirstOrDefault(x => x.key == (int)EKeyTongCucThongKe.HanhKhach_HangKhong);

            strBuilder.AppendLine($"*Nhóm ngành cảng biển, Logistic:");
            strBuilder.AppendLine($"1. Vận tải Đường Biển: M({Math.Round((DuongBien?.m ?? 0) - 100, 1).ToString("#,##0.##")}%)| Y({Math.Round((DuongBien?.y ?? 0) - 100, 1)}%)");
            strBuilder.AppendLine($"2. Vận tải Đường Bộ:     M({Math.Round((DuongBo?.m ?? 0) - 100, 1).ToString("#,##0.##")}%)| Y({Math.Round((DuongBo?.y ?? 0) - 100, 1)}%)");
            strBuilder.AppendLine($"3. Vận tải Hàng Không: M({Math.Round((DuongHangKhong?.m ?? 0) - 100, 1).ToString("#,##0.##")}%)| Y({Math.Round((DuongHangKhong?.y ?? 0) - 100, 1)}%)");
            strBuilder.AppendLine($"4. Tổng lượt khách HK: M({Math.Round((KhachHangKhong?.m ?? 0) - 100, 1).ToString("#,##0.##")}%)| Y({Math.Round((KhachHangKhong?.y ?? 0) - 100, 1)}%)");
            return strBuilder.ToString();
        }

        private string KCNStr(DateTime dt, List<ThongKe> lData)
        {
            var strBuilder = new StringBuilder();
            var filterFDI = Builders<ThongKe>.Filter.Eq(x => x.key, (int)EKeyTongCucThongKe.FDI);
            var lDataFDI = _thongkeRepo.GetByFilter(filterFDI);
            var lDataCur = lDataFDI.Where(x => x.d == int.Parse($"{dt.Year}{dt.Month.To2Digit()}")).OrderByDescending(x => x.va).Take(5);
            strBuilder.AppendLine($"*Nhóm ngành KCN:");
            var iFDI = 1;
            foreach (var item in lDataCur)
            {
                var qoqoy = lDataFDI.FirstOrDefault(x => x.d == int.Parse($"{dt.AddMonths(-1).Year}{(dt.AddMonths(-1).Month).To2Digit()}") && x.content.Replace(" ", "").Equals(item.content.Replace(" ", "")));
                var qoq = lDataFDI.FirstOrDefault(x => x.d == int.Parse($"{dt.AddYears(-1).Year}{dt.Month.To2Digit()}") && x.content.Replace(" ", "").Equals(item.content.Replace(" ", "")));
                var rateM = (qoqoy is null || qoqoy.va <= 0) ? 0 : Math.Round(100 * (-1 + item.va / qoqoy.va));
                var rateY = (qoq is null || qoq.va <= 0) ? 0 : Math.Round(100 * (-1 + item.va / qoq.va));

                var unit = "triệu USD";
                if (item.va >= 1000)
                {
                    unit = "tỷ USD";
                    item.va = Math.Round(item.va / 1000, 1);
                }

                strBuilder.AppendLine($"{iFDI++}. {item.content}({Math.Round(item.va, 1)} {unit})|M({rateM}%)|Y({rateY}%)");
            }
            return strBuilder.ToString();
        }

        private string XNKStr(DateTime dt, List<ThongKe> lData)
        {
            var strBuilder = new StringBuilder();
            var ThuySan = GetDataWithRate(lData, dt, EKeyTongCucThongKe.XK_ThuySan);
            var CaPhe = GetDataWithRate(lData, dt, EKeyTongCucThongKe.XK_CaPhe);
            var Gao = GetDataWithRate(lData, dt, EKeyTongCucThongKe.XK_Gao);
            var HoaChat = GetDataWithRate(lData, dt, EKeyTongCucThongKe.XK_HoaChat);
            var SPChatDeo = GetDataWithRate(lData, dt, EKeyTongCucThongKe.XK_SPChatDeo);
            var CaoSu = GetDataWithRate(lData, dt, EKeyTongCucThongKe.XK_CaoSu);
            var Go = GetDataWithRate(lData, dt, EKeyTongCucThongKe.XK_Go);
            var DetMay = GetDataWithRate(lData, dt, EKeyTongCucThongKe.XK_DetMay);
            var SatThep = GetDataWithRate(lData, dt, EKeyTongCucThongKe.XK_SatThep);
            var DayDien = GetDataWithRate(lData, dt, EKeyTongCucThongKe.XK_DayDien);
            strBuilder.AppendLine($"*Xuất khẩu:");
            strBuilder.AppendLine($"1. Thủy sản:        {Math.Round(ThuySan.Item1, 1).ToString("#,##0.##").ShowLimit()} triệu USD|M({ThuySan.Item2}%)|Y({ThuySan.Item3}%)");
            strBuilder.AppendLine($"2. Cà phê:            {Math.Round(CaPhe.Item1, 1).ToString("#,##0.##").ShowLimit()} triệu USD|M({CaPhe.Item2}%)|Y({CaPhe.Item3}%)");
            strBuilder.AppendLine($"3. Gạo:                 {Math.Round(Gao.Item1, 1).ToString("#,##0.##").ShowLimit()} triệu USD|M({Gao.Item2}%)|Y({Gao.Item3}%)");
            strBuilder.AppendLine($"4. Hóa chất:        {Math.Round(HoaChat.Item1, 1).ToString("#,##0.##").ShowLimit()} triệu USD|M({HoaChat.Item2}%)|Y({HoaChat.Item3}%)");
            strBuilder.AppendLine($"5. SP chất dẻo:  {Math.Round(SPChatDeo.Item1, 1).ToString("#,##0.##").ShowLimit()} triệu USD|M({SPChatDeo.Item2}%)|Y({SPChatDeo.Item3}%)");
            strBuilder.AppendLine($"6. Cao su:            {Math.Round(CaoSu.Item1, 1).ToString("#,##0.##").ShowLimit()} triệu USD|M({CaoSu.Item2}%)|Y({CaoSu.Item3}%)");
            strBuilder.AppendLine($"7. Gỗ:                    {Math.Round(Go.Item1, 1).ToString("#,##0.##").ShowLimit()} triệu USD|M({Go.Item2}%)|Y({Go.Item3}%)");
            strBuilder.AppendLine($"8. Dệt may:          {Math.Round(DetMay.Item1, 1).ToString("#,##0.##").ShowLimit()} triệu USD|M({DetMay.Item2}%)|Y({DetMay.Item3}%)");
            strBuilder.AppendLine($"9. Sắt thép:         {Math.Round(SatThep.Item1, 1).ToString("#,##0.##").ShowLimit()} triệu USD|M({SatThep.Item2}%)|Y({SatThep.Item3}%)");
            strBuilder.AppendLine($"10. Dây điện:      {Math.Round(DayDien.Item1, 1).ToString("#,##0.##").ShowLimit()} triệu USD|M({DayDien.Item2}%)|Y({DayDien.Item3}%)");

            return strBuilder.ToString();
        }

        private (double, double, double) GetDataWithRate(List<ThongKe> lData, DateTime dt, EKeyTongCucThongKe key1, EKeyTongCucThongKe key2 = EKeyTongCucThongKe.None)
        {
            var filterByKey1 = Builders<ThongKe>.Filter.Eq(x => x.key, (int)key1);
            var lDataFilter1 = _thongkeRepo.GetByFilter(filterByKey1);

            var curVal1 = lDataFilter1.FirstOrDefault(x => x.d == int.Parse($"{dt.Year}{dt.Month.To2Digit()}"));
            var valM1 = lDataFilter1.FirstOrDefault(x => x.d == int.Parse($"{dt.AddMonths(-1).Year}{dt.AddMonths(-1).Month.To2Digit()}"));
            var valY1 = lDataFilter1.FirstOrDefault(x => x.d == int.Parse($"{dt.AddYears(-1).Year}{dt.Month.To2Digit()}"));

            var curVal = curVal1?.va ?? 0;
            var valM = valM1?.va ?? 0;
            var valY = valY1?.va ?? 0;

            if (key2 != EKeyTongCucThongKe.None)
            {
                var filterByKey2 = Builders<ThongKe>.Filter.Eq(x => x.key, (int)key2);
                var lDataFilter2 = _thongkeRepo.GetByFilter(filterByKey2);
                var curVal2 = lDataFilter2.FirstOrDefault(x => x.d == int.Parse($"{dt.Year}{dt.Month.To2Digit()}"));
                var valM2 = lDataFilter2.FirstOrDefault(x => x.d == int.Parse($"{dt.AddMonths(-1).Year}{dt.AddMonths(-1).Month.To2Digit()}"));
                var valY2 = lDataFilter2.FirstOrDefault(x => x.d == int.Parse($"{dt.AddYears(-1).Year}{dt.Month.To2Digit()}"));

                curVal += (curVal2?.va ?? 0);
                valM += (valM2?.va ?? 0);
                valY += (valY2?.va ?? 0);
            }

            var rateM = valM > 0 ? Math.Round(100 * (-1 + curVal / valM), 1) : 0;
            var rateY = valY > 0 ? Math.Round(100 * (-1 + curVal / valY), 1) : 0;
            return (curVal, rateM, rateY);
        } 
        #endregion
    }
}
