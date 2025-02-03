using MongoDB.Driver;
using StockPr.DAL.Entity;
using StockPr.Model;
using StockPr.Utils;
using System.Text;

namespace StockPr.Service
{
    public partial class BaoCaoTaiChinhService
    {
        public async Task BCTC_FixData(string code)
        {
            try
            {
                var isNganHang = StaticVal._lStock.Any(x => x.status == 1 && x.cat.Any(x => x.ty == (int)EStockType.NganHang) && x.s == code);
                if (isNganHang)
                {
                    await BCTC_FixData_NganHang(code);
                    return;
                }

                var isChungKhoan = StaticVal._lStock.Any(x => x.status == 1 && x.cat.Any(x => x.ty == (int)EStockType.ChungKhoan) && x.s == code);
                if (isChungKhoan)
                {
                    await BCTC_FixData_ChungKhoan(code);
                    return;
                }

                var isBDS = StaticVal._lStock.Any(x => x.status == 1 && x.cat.Any(x => x.ty == (int)EStockType.BDS) && x.s == code);
                if (isChungKhoan)
                {
                    await BCTC_FixData_BDS(code);
                    return;
                }

                await BCTC_FixData_CK(code);
            }
            catch(Exception ex)
            {
                _logger.LogError($"BaoCaoTaiChinhService.FixData|EXCEPTION| {ex.Message}");
            }
        }

        private async Task BCTC_FixData_NganHang(string code)
        {
            try
            {
                await Fix_SyncBCTC_NganHang_KQKD(code);
                await Fix_SyncBCTC_NganHang_CSTC(code);
                await Fix_SyncBCTC_NganHang_ThuyetMinh(code);
            }
            catch (Exception ex)
            {
                _logger.LogError($"BaoCaoTaiChinhService.BCTC_FixData_NganHang|EXCEPTION| {ex.Message}");
            }
        }
        private async Task Fix_SyncBCTC_NganHang_KQKD(string code)
        {
            try
            {
                var lReportID = await _apiService.VietStock_KQKD_GetListReportData(code);
                Thread.Sleep(1000);
                if (!lReportID.data.Any())
                    return;

                var isFirst = true;
                do
                {
                    IEnumerable<ReportDataIDDetailResponse> lDataReport = null;
                    if(isFirst)
                    {
                        lDataReport = lReportID.data.TakeLast(8);
                        isFirst = false;
                    }
                    else if(lReportID.data.Count > 8)
                    {
                        lDataReport = lReportID.data.SkipLast(8).TakeLast(8);
                    }

                    var strBuilder = new StringBuilder();
                    strBuilder.Append($"StockCode={code}&");
                    strBuilder.Append($"Unit=1000000000&");
                    var index = 0;
                    foreach (var item in lDataReport)
                    {
                        strBuilder.Append($"listReportDataIds[{index}][ReportDataId]={item.ReportDataID}&");
                        strBuilder.Append($"listReportDataIds[{index}][YearPeriod]={item.YearPeriod}&");
                        index++;
                    }
                    strBuilder.Append($"__RequestVerificationToken={StaticVal._VietStock_Token}");

                    var txt = strBuilder.ToString().Replace("]", "%5D").Replace("[", "%5B");
                    var lData = await _apiService.VietStock_GetReportDataDetailValue_KQKD_ByReportDataIds(txt);
                    Thread.Sleep(1000);
                    if (!(lData?.data?.Any() ?? false))
                        return;

                    foreach (var item in lReportID.data)
                    {
                        var year = item.BasePeriodBegin / 100;
                        var month = item.BasePeriodBegin - year * 100;
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

                        //insert
                        var entity = new Financial
                        {
                            d = int.Parse($"{year}{quarter}"),
                            s = code,
                            t = (int)DateTimeOffset.Now.ToUnixTimeSeconds()
                        };

                        var ThuNhapLai = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.ThuNhapLai);
                        var ThuNhapTuDichVu = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.ThuNhapTuDichVu);
                        var LNSTNH = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.LNSTNH);
                        AssignData(ThuNhapLai?.Value1, ThuNhapTuDichVu?.Value1, LNSTNH?.Value1);
                        _financialRepo.InsertOne(entity);

                        void AssignData(double? thunhaplai, double? thunhaptudichvu, double? loinhuan)
                        {
                            entity.rv = (thunhaplai ?? 0) + (thunhaptudichvu ?? 0);
                            entity.pf = loinhuan ?? 0;
                        }
                    }

                    if (!isFirst)
                        return;
                }
                while (true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC_NganHang_KQKD|EXCEPTION| {ex.Message}");
            }
        }
        private async Task Fix_SyncBCTC_NganHang_CSTC(string code)
        {
            try
            {
                var lReportID = await _apiService.VietStock_CSTC_GetListTempID(code);
                Thread.Sleep(1000);
                if (!lReportID.data.Any())
                    return;

                var isFirst = true;
                do
                {
                    IEnumerable<ReportTempIDDetailResponse> lDataReport = null;
                    if (isFirst)
                    {
                        lDataReport = lReportID.data.TakeLast(8);
                        isFirst = false;
                    }
                    else if (lReportID.data.Count > 8)
                    {
                        lDataReport = lReportID.data.SkipLast(8).TakeLast(8);
                    }

                    var strBuilder = new StringBuilder();
                    strBuilder.Append($"StockCode={code}&");
                    var index = 0;
                    foreach (var item in lDataReport)
                    {
                        strBuilder.Append($"ListTerms[{index}][ItemId]={item.IdTemp}&");
                        strBuilder.Append($"ListTerms[{index}][YearPeriod]={item.YearPeriod}&");
                        index++;
                    }
                    strBuilder.Append($"__RequestVerificationToken={StaticVal._VietStock_Token}");

                    var txt = strBuilder.ToString().Replace("]", "%5D").Replace("[", "%5B");
                    var lData = await _apiService.VietStock_GetFinanceIndexDataValue_CSTC_ByListTerms(txt);
                    Thread.Sleep(1000);
                    if (!(lData?.data?.Any() ?? false))
                        return;


                    foreach (var item in lReportID.data)
                    {
                        var year = item.YearPeriod;
                        var quarter = item.ReportTermID - 1;

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

                        var lUpdate = _financialRepo.GetByFilter(filter);
                        var entity = lUpdate.FirstOrDefault();
                        if (entity is null)
                        {
                            continue;
                        }

                        //update
                        var cir = lData?.data.FirstOrDefault(x => x.FinanceIndexID == (int)EFinanceIndex.CIR);
                        AssignData(cir?.Value1);
                        entity.t = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                        _financialRepo.Update(entity);

                        void AssignData(double? cir)
                        {
                            entity.cir_r = cir ?? 0;
                        }
                    }

                    if (!isFirst)
                        return;
                }
                while (true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC_NganHang_KQKD|EXCEPTION| {ex.Message}");
            }
        }
        private async Task Fix_SyncBCTC_NganHang_ThuyetMinh(string code)
        {
            try
            {
                var lReportID = await _apiService.VietStock_TM_GetListReportData(code);
                Thread.Sleep(1000);
                if (!lReportID.data.Any())
                    return;

                var isFirst = true;
                do
                {
                    IEnumerable<ReportDataIDDetailResponse> lDataReport = null;
                    if (isFirst)
                    {
                        lDataReport = lReportID.data.TakeLast(8);
                        isFirst = false;
                    }
                    else if (lReportID.data.Count > 8)
                    {
                        lDataReport = lReportID.data.SkipLast(8).TakeLast(8);
                    }

                    var strBuilder = new StringBuilder();
                    strBuilder.Append($"StockCode={code}&");
                    strBuilder.Append($"Unit=1000000&");
                    var index = 0;
                    foreach (var item in lDataReport)
                    {
                        strBuilder.Append($"listReportDataIds[{index}][ReportDataId]={item.ReportDataID}&");
                        strBuilder.Append($"listReportDataIds[{index}][YearPeriod]={item.BasePeriodBegin / 100}&");
                        index++;
                    }
                    strBuilder.Append($"__RequestVerificationToken={StaticVal._VietStock_Token}");

                    var txt = strBuilder.ToString().Replace("]", "%5D").Replace("[", "%5B");
                    var lData = await _apiService.VietStock_GetReportDataDetailValue_TM_ByReportDataIds(txt);
                    Thread.Sleep(1000);
                    if (!(lData?.data?.Any() ?? false))
                        return;


                    foreach (var item in lReportID.data)
                    {
                        var year = item.YearPeriod;
                        var quarter = item.ReportTermID - 1;

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

                        var lUpdate = _financialRepo.GetByFilter(filter);
                        var entity = lUpdate.FirstOrDefault();
                        if (entity is null)
                        {
                            continue;
                        }

                        //update
                        var NoNhom1 = lData?.data.FirstOrDefault(x => x.ReportNormNoteID == (int)EReportNormId.NoNhom1);
                        var NoNhom2 = lData?.data.FirstOrDefault(x => x.ReportNormNoteID == (int)EReportNormId.NoNhom2);
                        var NoNhom3 = lData?.data.FirstOrDefault(x => x.ReportNormNoteID == (int)EReportNormId.NoNhom3);
                        var NoNhom4 = lData?.data.FirstOrDefault(x => x.ReportNormNoteID == (int)EReportNormId.NoNhom4);
                        var NoNhom5 = lData?.data.FirstOrDefault(x => x.ReportNormNoteID == (int)EReportNormId.NoNhom5);
                        var TienGuiKH = lData?.data.FirstOrDefault(x => x.ReportNormNoteID == (int)EReportNormId.TienGuiKhachHang);
                        var TienGuiKhongKyHan = lData?.data.FirstOrDefault(x => x.ReportNormNoteID == (int)EReportNormId.TienGuiKhongKyHan);
                        var casa = Math.Round((TienGuiKhongKyHan?.Value1 ?? 0) * 100 / (TienGuiKH?.Value1 ?? 1), 1);

                        entity.casa_r = casa;

                        AssignData(NoNhom1?.Value1, NoNhom2?.Value1, NoNhom3?.Value1, NoNhom4?.Value1, NoNhom5?.Value1);
                        entity.t = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                        _financialRepo.Update(entity);

                        void AssignData(double? NoNhom1, double? NoNhom2, double? NoNhom3, double? NoNhom4, double? NoNhom5)
                        {
                            entity.debt1 = (NoNhom1 ?? 0) / 1000;
                            entity.debt2 = (NoNhom2 ?? 0) / 1000;
                            entity.debt3 = (NoNhom3 ?? 0) / 1000;
                            entity.debt4 = (NoNhom4 ?? 0) / 1000;
                            entity.debt5 = (NoNhom5 ?? 0) / 1000;
                        }
                    }

                    if (!isFirst)
                        return;
                }
                while (true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC_NganHang_KQKD|EXCEPTION| {ex.Message}");
            }
        }

        private async Task BCTC_FixData_ChungKhoan(string code)
        {
            try
            {

            }
            catch (Exception ex)
            {
                _logger.LogError($"BaoCaoTaiChinhService.BCTC_FixData_ChungKhoan|EXCEPTION| {ex.Message}");
            }
        }

        private async Task BCTC_FixData_BDS(string code)
        {
            try
            {

            }
            catch (Exception ex)
            {
                _logger.LogError($"BaoCaoTaiChinhService.BCTC_FixData_BDS|EXCEPTION| {ex.Message}");
            }
        }

        private async Task BCTC_FixData_CK(string code)
        {
            try
            {

            }
            catch (Exception ex)
            {
                _logger.LogError($"BaoCaoTaiChinhService.BCTC_FixData_CK|EXCEPTION| {ex.Message}");
            }
        }
    }
}
