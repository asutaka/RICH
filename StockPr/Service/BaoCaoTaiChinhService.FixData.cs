using StockPr.DAL.Entity;
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
                lReportID.data = lReportID.data.Where(x => x.YearPeriod >= 2024).ToList();

                var strBuilder = new StringBuilder();
                strBuilder.Append($"StockCode={code}&");
                strBuilder.Append($"Unit=1000000000&");
                var index = 0;
                foreach (var item in lReportID.data)
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
