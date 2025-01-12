using MongoDB.Driver;
using StockPr.DAL.Entity;
using StockPr.Model;
using StockPr.Utils;
using System.Text;

namespace StockPr.Service
{
    public partial class BaoCaoTaiChinhService
    {
        private async Task SyncBCTC(bool onlyLast = false)
        {
            try
            {
                var lStock = _stockRepo.GetAll();
                var lStockFilter = lStock.Where(x => x.status == 1 && x.cat.Any(x => x.ty == (int)EStockType.BanLe
                                                                                || x.ty == (int)EStockType.CangBien
                                                                                || x.ty == (int)EStockType.CaoSu
                                                                                || x.ty == (int)EStockType.Go
                                                                                || x.ty == (int)EStockType.Nhua
                                                                                || x.ty == (int)EStockType.PhanBon
                                                                                || x.ty == (int)EStockType.Than
                                                                                || x.ty == (int)EStockType.Thep
                                                                                || x.ty == (int)EStockType.ThuySan
                                                                                || x.ty == (int)EStockType.XiMang
                                                                                || x.ty == (int)EStockType.Oto
                                                                                || x.ty == (int)EStockType.OtoTai
                                                                                || x.ty == (int)EStockType.VanTaiBien
                                                                                || x.ty == (int)EStockType.Logistic
                                                                                || x.ty == (int)EStockType.DienGio
                                                                                || x.ty == (int)EStockType.DienKhi
                                                                                || x.ty == (int)EStockType.DienMatTroi
                                                                                || x.ty == (int)EStockType.DienThan
                                                                                || x.ty == (int)EStockType.ThuyDien
                                                                                || x.ty == (int)EStockType.DetMay
                                                                                || x.ty == (int)EStockType.DauKhi
                                                                                || x.ty == (int)EStockType.HangKhong
                                                                                || x.ty == (int)EStockType.NangLuong)).Select(x => x.s);
                foreach (var item in lStockFilter)
                {
                    await SyncBCTC_KQKD(item, onlyLast);
                    await SyncBCTC_CDKT(item, onlyLast, ECDKTType.Normal);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC|EXCEPTION| {ex.Message}");
            }
        }

        private async Task SyncBCTC_BatDongSan(bool onlyLast = false)
        {
            try
            {
                var lBDS = StaticVal._lStock.Where(x => x.status == 1 && x.cat.Any(x => x.ty == (int)EStockType.BDS)).Select(x => x.s);
                foreach (var item in lBDS)
                {
                    await SyncBCTC_KQKD(item, onlyLast);
                    await SyncBCTC_CDKT(item, onlyLast, ECDKTType.BDS);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC_BatDongSan|EXCEPTION| {ex.Message}");
            }
        }

        private async Task SyncBCTC_KQKD(string code, bool onlyLast)
        {
            try
            {
                var time = GetCurrentTime();
                var batchCount = 8;
                var lReportID = await _apiService.VietStock_KQKD_GetListReportData(code);
                if (onlyLast)
                {
                    lReportID.data = lReportID.data.TakeLast(4).ToList();
                }
                Thread.Sleep(1000);
                if (!lReportID.data.Any())
                    return;

                var totalCount = lReportID.data.Count();
                var last = lReportID.data.Last();
                if (last.BasePeriodBegin / 100 > time.Item2)
                {
                    var div = last.ReportTermID - time.Item3;
                    foreach (var item in lReportID.data)
                    {
                        var term = item.ReportTermID - div;
                        if (term == 0)
                        {
                            term = 4;
                            item.YearPeriod -= 1;
                        }
                        var month = 0;
                        if (term == 1)
                        {
                            month = 1;
                        }
                        else if (term == 2)
                        {
                            month = 4;
                        }
                        else if (term == 3)
                        {
                            month = 7;
                        }
                        else if (term == 4)
                        {
                            month = 10;
                        }

                        item.BasePeriodBegin = int.Parse($"{item.YearPeriod}{month.To2Digit()}");
                    }
                }

                lReportID.data = lReportID.data.Where(x => (x.Isunited == 0 || x.Isunited == 1) && x.BasePeriodBegin >= 202001).ToList();

                var lBatch = new List<List<ReportDataIDDetailResponse>>();
                var lSub = new List<ReportDataIDDetailResponse>();
                for (int i = 0; i < lReportID.data.Count; i++)
                {
                    if (i > 0 && i % batchCount == 0)
                    {
                        lBatch.Add(lSub.ToList());
                        lSub.Clear();
                    }
                    lSub.Add(lReportID.data[i]);
                }
                if (lSub.Any())
                {
                    lBatch.Add(lSub.ToList());
                }

                foreach (var item in lBatch)
                {
                    var strBuilder = new StringBuilder();
                    strBuilder.Append($"StockCode={code}&");
                    strBuilder.Append($"Unit=1000000000&");
                    strBuilder.Append($"__RequestVerificationToken={StaticVal._VietStock_Token}&");

                    var count = item.Count();
                    for (int i = 0; i < count; i++)
                    {
                        if (i > 0)
                        {
                            strBuilder.Append("&");
                        }
                        var element = item[i];
                        strBuilder.Append($"listReportDataIds[{i}][ReportDataId]={element.ReportDataID}&");
                        strBuilder.Append($"listReportDataIds[{i}][YearPeriod]={element.BasePeriodBegin / 100}");
                    }
                    var txt = strBuilder.ToString().Replace("]", "%5D").Replace("[", "%5B");
                    var lData = await _apiService.VietStock_GetReportDataDetailValue_KQKD_ByReportDataIds(txt);
                    Thread.Sleep(1000);
                    if (lData is null || lData.data is null)
                        continue;

                    for (int i = 0; i < count; i++)
                    {
                        var element = item[i];
                        var year = element.BasePeriodBegin / 100;
                        var month = element.BasePeriodBegin - year * 100;
                        var quarter = 1;
                        if (month >= 10)
                        {
                            quarter = 4;
                        }
                        else if (month >= 7)
                        {
                            quarter = 3;
                        }
                        else if (month >= 4)
                        {
                            quarter = 2;
                        }

                        FilterDefinition<Financial> filter = null;
                        var builder = Builders<Financial>.Filter;
                        var lFilter = new List<FilterDefinition<Financial>>
                        {
                            builder.Eq(x => x.s, code),
                            builder.Eq(x => x.d, int.Parse($"{year}{quarter}"))
                        };

                        foreach (var itemFilter in lFilter)
                        {
                            if (filter is null)
                            {
                                filter = itemFilter;
                                continue;
                            }
                            filter &= itemFilter;
                        }

                        var entityUpdate = _financialRepo.GetEntityByFilter(filter);
                        if (entityUpdate is null)
                        {
                            //insert
                            entityUpdate = new Financial
                            {
                                d = int.Parse($"{year}{quarter}"),
                                s = code,
                                t = (int)DateTimeOffset.Now.ToUnixTimeSeconds()
                            };
                            _financialRepo.InsertOne(entityUpdate);
                        }

                        //
                        var DoanhThu = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.DoanhThu);
                        var LoiNhuan = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.LNST);
                        var LoiNhuanGop = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.LNGop);
                        var LoiNhuanRong = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.LNRong);

                        switch (i)
                        {
                            case 0: AssignData(DoanhThu?.Value1, LoiNhuan?.Value1, LoiNhuanGop?.Value1, LoiNhuanRong?.Value1); break;
                            case 1: AssignData(DoanhThu?.Value2, LoiNhuan?.Value2, LoiNhuanGop?.Value2, LoiNhuanRong?.Value2); break;
                            case 2: AssignData(DoanhThu?.Value3, LoiNhuan?.Value3, LoiNhuanGop?.Value3, LoiNhuanRong?.Value3); break;
                            case 3: AssignData(DoanhThu?.Value4, LoiNhuan?.Value4, LoiNhuanGop?.Value4, LoiNhuanRong?.Value4); break;
                            case 4: AssignData(DoanhThu?.Value5, LoiNhuan?.Value5, LoiNhuanGop?.Value5, LoiNhuanRong?.Value5); break;
                            case 5: AssignData(DoanhThu?.Value6, LoiNhuan?.Value6, LoiNhuanGop?.Value6, LoiNhuanRong?.Value6); break;
                            case 6: AssignData(DoanhThu?.Value7, LoiNhuan?.Value7, LoiNhuanGop?.Value7, LoiNhuanRong?.Value7); break;
                            case 7: AssignData(DoanhThu?.Value8, LoiNhuan?.Value8, LoiNhuanGop?.Value8, LoiNhuanRong?.Value8); break;
                            default: break;
                        };

                        entityUpdate.t = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                        _financialRepo.Update(entityUpdate);

                        void AssignData(double? DoanhThu, double? LoiNhuan, double? LoiNhuanGop, double? LoiNhuanRong)
                        {
                            entityUpdate.rv = DoanhThu ?? 0;
                            entityUpdate.pf = LoiNhuan ?? 0;
                            entityUpdate.pfg = LoiNhuanGop ?? 0;
                            entityUpdate.pfn = LoiNhuanRong ?? 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC_KQKD|EXCEPTION| {ex.Message}");
            }
        }

        private async Task SyncBCTC_CDKT(string code, bool onlyLast, ECDKTType type)//type: 1-Normal
        {
            try
            {
                var time = GetCurrentTime();
                var batchCount = 8;
                var lReportID = await _apiService.VietStock_CDKT_GetListReportData(code);
                if (onlyLast)
                {
                    lReportID.data = lReportID.data.TakeLast(4).ToList();
                }
                Thread.Sleep(1000);
                if (!lReportID.data.Any())
                    return;
                var totalCount = lReportID.data.Count();
                var last = lReportID.data.Last();
                if (last.BasePeriodBegin / 100 > time.Item2)
                {
                    var div = last.ReportTermID - time.Item3;
                    foreach (var item in lReportID.data)
                    {
                        var term = item.ReportTermID - div;
                        if (term == 0)
                        {
                            term = 4;
                            item.YearPeriod -= 1;
                        }
                        var month = 0;
                        if (term == 1)
                        {
                            month = 1;
                        }
                        else if (term == 2)
                        {
                            month = 4;
                        }
                        else if (term == 3)
                        {
                            month = 7;
                        }
                        else if (term == 4)
                        {
                            month = 10;
                        }

                        item.BasePeriodBegin = int.Parse($"{item.YearPeriod}{month.To2Digit()}");
                    }
                }
                lReportID.data = lReportID.data.Where(x => (x.Isunited == 0 || x.Isunited == 1) && x.BasePeriodBegin >= 202001).ToList();
                var lBatch = new List<List<ReportDataIDDetailResponse>>();
                var lSub = new List<ReportDataIDDetailResponse>();
                for (int i = 0; i < lReportID.data.Count; i++)
                {
                    if (i > 0 && i % batchCount == 0)
                    {
                        lBatch.Add(lSub.ToList());
                        lSub.Clear();
                    }
                    lSub.Add(lReportID.data[i]);
                }
                if (lSub.Any())
                {
                    lBatch.Add(lSub.ToList());
                }


                foreach (var item in lBatch)
                {
                    var strBuilder = new StringBuilder();
                    strBuilder.Append($"StockCode={code}&");
                    strBuilder.Append($"Unit=1000000000&");
                    strBuilder.Append($"__RequestVerificationToken={StaticVal._VietStock_Token}&");

                    var count = item.Count();
                    for (int i = 0; i < count; i++)
                    {
                        if (i > 0)
                        {
                            strBuilder.Append("&");
                        }
                        var element = item[i];
                        strBuilder.Append($"listReportDataIds[{i}][ReportDataId]={element.ReportDataID}&");
                        strBuilder.Append($"listReportDataIds[{i}][YearPeriod]={element.BasePeriodBegin / 100}");
                    }
                    var txt = strBuilder.ToString().Replace("]", "%5D").Replace("[", "%5B");
                    var lData = await _apiService.VietStock_GetReportDataDetailValue_CDKT_ByReportDataIds(txt);
                    Thread.Sleep(1000);
                    if (lData is null || lData.data is null)
                        continue;

                    for (int i = 0; i < count; i++)
                    {
                        var element = item[i];
                        var year = element.BasePeriodBegin / 100;
                        var month = element.BasePeriodBegin - year * 100;
                        var quarter = 1;
                        if (month >= 10)
                        {
                            quarter = 4;
                        }
                        else if (month >= 7)
                        {
                            quarter = 3;
                        }
                        else if (month >= 4)
                        {
                            quarter = 2;
                        }

                        FilterDefinition<Financial> filter = null;
                        var builder = Builders<Financial>.Filter;
                        var lFilter = new List<FilterDefinition<Financial>>
                        {
                            builder.Eq(x => x.s, code),
                            builder.Eq(x => x.d, int.Parse($"{year}{quarter}"))
                        };

                        foreach (var itemFilter in lFilter)
                        {
                            if (filter is null)
                            {
                                filter = itemFilter;
                                continue;
                            }
                            filter &= itemFilter;
                        }

                        var entityUpdate = _financialRepo.GetEntityByFilter(filter);
                        if (entityUpdate is null)
                        {
                            continue;
                        }

                        if (type == ECDKTType.BDS)
                        {
                            entityUpdate = CDKT_BDS(entityUpdate, lData, i);
                        }
                        else if (type == ECDKTType.ChungKhoan)
                        {
                            entityUpdate = CDKT_ChungKhoan(entityUpdate, lData, i);
                        }
                        else
                        {
                            entityUpdate = CDKT_Normal(entityUpdate, lData, i);
                        }
                        _financialRepo.Update(entityUpdate);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC_CDKT|EXCEPTION| {ex.Message}");
            }
        }

        private Financial CDKT_Normal(Financial entityUpdate, ReportDataDetailValue_BCTTResponse lData, int i)
        {
            try
            {
                entityUpdate.t = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                var TonKho = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.TonKhoThep);
                var VayNganHan = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.VayNganHan);
                var VayDaiHan = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.VayDaiHan);
                var VonChuSH = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.VonChuSoHuu);

                switch (i)
                {
                    case 0: AssignData(TonKho?.Value1, VayNganHan?.Value1, VayDaiHan?.Value1, VonChuSH?.Value1); break;
                    case 1: AssignData(TonKho?.Value2, VayNganHan?.Value2, VayDaiHan?.Value2, VonChuSH?.Value2); break;
                    case 2: AssignData(TonKho?.Value3, VayNganHan?.Value3, VayDaiHan?.Value3, VonChuSH?.Value3); break;
                    case 3: AssignData(TonKho?.Value4, VayNganHan?.Value4, VayDaiHan?.Value4, VonChuSH?.Value4); break;
                    case 4: AssignData(TonKho?.Value5, VayNganHan?.Value5, VayDaiHan?.Value5, VonChuSH?.Value5); break;
                    case 5: AssignData(TonKho?.Value6, VayNganHan?.Value6, VayDaiHan?.Value6, VonChuSH?.Value6); break;
                    case 6: AssignData(TonKho?.Value7, VayNganHan?.Value7, VayDaiHan?.Value7, VonChuSH?.Value7); break;
                    case 7: AssignData(TonKho?.Value8, VayNganHan?.Value8, VayDaiHan?.Value8, VonChuSH?.Value8); break;
                    default: break;
                };

                void AssignData(double? TonKho, double? VayNganHan, double? VayDaiHan, double? VonChuSH)
                {
                    entityUpdate.inv = TonKho ?? 0;
                    entityUpdate.debt = (VayNganHan ?? 0) + (VayDaiHan ?? 0);
                    entityUpdate.eq = VonChuSH ?? 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.CDKT_Normal|EXCEPTION| {ex.Message}");
            }
            return entityUpdate;
        }

        private Financial CDKT_BDS(Financial entityUpdate, ReportDataDetailValue_BCTTResponse lData, int i)
        {
            try
            {
                entityUpdate.t = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                var TonKho = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.TonKho);
                var NguoiMua = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.NguoiMuaTraTienTruoc);
                var VayNganHan = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.VayNganHan);
                var VayDaiHan = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.VayDaiHan);
                var VonChu = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.VonChuSoHuu);

                switch (i)
                {
                    case 0: AssignData(TonKho?.Value1, NguoiMua?.Value1, VayNganHan?.Value1, VayDaiHan?.Value1, VonChu?.Value1); break;
                    case 1: AssignData(TonKho?.Value2, NguoiMua?.Value2, VayNganHan?.Value2, VayDaiHan?.Value2, VonChu?.Value2); break;
                    case 2: AssignData(TonKho?.Value3, NguoiMua?.Value3, VayNganHan?.Value3, VayDaiHan?.Value3, VonChu?.Value3); break;
                    case 3: AssignData(TonKho?.Value4, NguoiMua?.Value4, VayNganHan?.Value4, VayDaiHan?.Value4, VonChu?.Value4); break;
                    case 4: AssignData(TonKho?.Value5, NguoiMua?.Value5, VayNganHan?.Value5, VayDaiHan?.Value5, VonChu?.Value5); break;
                    case 5: AssignData(TonKho?.Value6, NguoiMua?.Value6, VayNganHan?.Value6, VayDaiHan?.Value6, VonChu?.Value6); break;
                    case 6: AssignData(TonKho?.Value7, NguoiMua?.Value7, VayNganHan?.Value7, VayDaiHan?.Value7, VonChu?.Value7); break;
                    case 7: AssignData(TonKho?.Value8, NguoiMua?.Value8, VayNganHan?.Value8, VayDaiHan?.Value8, VonChu?.Value8); break;
                    default: break;
                };

                void AssignData(double? tonkho, double? nguoimua, double? vayNganHan, double? vayDaiHan, double? vonchu)
                {
                    entityUpdate.inv = tonkho ?? 0;
                    entityUpdate.bp = nguoimua ?? 0;
                    entityUpdate.debt = (vayNganHan ?? 0) + (vayDaiHan ?? 0);
                    entityUpdate.eq = vonchu ?? 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.CDKT_Normal|EXCEPTION| {ex.Message}");
            }
            return entityUpdate;
        }

        private Financial CDKT_ChungKhoan(Financial entityUpdate, ReportDataDetailValue_BCTTResponse lData, int i)
        {
            try
            {
                entityUpdate.t = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                var TaiSanFVTPL = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.TaiSanFVTPL);
                var TaiSanHTM = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.TaiSanHTM);
                var TaiSanAFS = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.TaiSanAFS);
                var TaiSanChoVay = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.TaiSanChoVay);
                var VonChuSoHuuCK = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.VonChuSoHuuCK);

                switch (i)
                {
                    case 0: AssignData(TaiSanFVTPL?.Value1, TaiSanHTM?.Value1, TaiSanAFS?.Value1, TaiSanChoVay?.Value1, VonChuSoHuuCK?.Value1); break;
                    case 1: AssignData(TaiSanFVTPL?.Value2, TaiSanHTM?.Value2, TaiSanAFS?.Value2, TaiSanChoVay?.Value2, VonChuSoHuuCK?.Value2); break;
                    case 2: AssignData(TaiSanFVTPL?.Value3, TaiSanHTM?.Value3, TaiSanAFS?.Value3, TaiSanChoVay?.Value3, VonChuSoHuuCK?.Value3); break;
                    case 3: AssignData(TaiSanFVTPL?.Value4, TaiSanHTM?.Value4, TaiSanAFS?.Value4, TaiSanChoVay?.Value4, VonChuSoHuuCK?.Value4); break;
                    case 4: AssignData(TaiSanFVTPL?.Value5, TaiSanHTM?.Value5, TaiSanAFS?.Value5, TaiSanChoVay?.Value5, VonChuSoHuuCK?.Value5); break;
                    case 5: AssignData(TaiSanFVTPL?.Value6, TaiSanHTM?.Value6, TaiSanAFS?.Value6, TaiSanChoVay?.Value6, VonChuSoHuuCK?.Value6); break;
                    case 6: AssignData(TaiSanFVTPL?.Value7, TaiSanHTM?.Value7, TaiSanAFS?.Value7, TaiSanChoVay?.Value7, VonChuSoHuuCK?.Value7); break;
                    case 7: AssignData(TaiSanFVTPL?.Value8, TaiSanHTM?.Value8, TaiSanAFS?.Value8, TaiSanChoVay?.Value8, VonChuSoHuuCK?.Value8); break;
                    default: break;
                };

                void AssignData(double? TaiSanFVTPL, double? TaiSanHTM, double? TaiSanAFS, double? TaiSanChoVay, double? VonChuSoHuuCK)
                {
                    entityUpdate.itrade = TaiSanFVTPL ?? 0 + TaiSanHTM ?? 0 + TaiSanAFS ?? 0;
                    entityUpdate.debt = TaiSanChoVay ?? 0;
                    entityUpdate.eq = VonChuSoHuuCK ?? 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.CDKT_Normal|EXCEPTION| {ex.Message}");
            }
            return entityUpdate;
        }
    }
}
