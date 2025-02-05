using MongoDB.Driver;
using StockPr.DAL.Entity;
using StockPr.Utils;
using System.Text;

namespace StockPr.Service
{
    public partial class BaoCaoTaiChinhService
    {
        private async Task SyncBCTC_ChungKhoan(bool ghide = false)
        {
            try
            {
                var lStockFilter = StaticVal._lStock.Where(x => x.status == 1 && x.cat.Any(x => x.ty == (int)EStockType.ChungKhoan)).Select(x => x.s);

                foreach (var item in lStockFilter)
                {
                    FilterDefinition<Financial> filter = null;
                    var builder = Builders<Financial>.Filter;
                    var lFilter = new List<FilterDefinition<Financial>>()
                    {
                        builder.Gte(x => x.d, StaticVal._curQuarter),
                        builder.Eq(x => x.s, item),
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
                    var exists = _financialRepo.GetEntityByFilter(filter);
                    if (exists != null)
                    {
                        if (ghide)
                        {
                            _financialRepo.DeleteMany(filter);
                        }
                        else
                        {
                            continue;
                        }
                    }

                    await SyncBCTC_ChungKhoan_KQKD(item);
                    await SyncBCTC_CDKT(item, ECDKTType.ChungKhoan);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC_ChungKhoan|EXCEPTION| {ex.Message}");
            }
        }

        private async Task SyncBCTC_ChungKhoan_KQKD(string code)
        {
            try
            {
                var time = GetCurrentTime();
                var lReportID = await _apiService.VietStock_KQKD_GetListReportData(code);
                Thread.Sleep(1000);
                var last = lReportID.data.LastOrDefault();
                if (last is null)
                    return;

                var year = last.BasePeriodBegin / 100;
                var month = last.BasePeriodBegin - year * 100;
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

                //check day
                var d = int.Parse($"{year}{quarter}");
                if (d < StaticVal._curQuarter)
                    return;

                var strBuilder = new StringBuilder();
                strBuilder.Append($"StockCode={code}&");
                strBuilder.Append($"Unit=1000000000&");
                strBuilder.Append($"__RequestVerificationToken={StaticVal._VietStock_Token}&");
                strBuilder.Append($"listReportDataIds[0][ReportDataId]={last.ReportDataID}&");
                strBuilder.Append($"listReportDataIds[0][YearPeriod]={last.BasePeriodBegin / 100}");
                var txt = strBuilder.ToString().Replace("]", "%5D").Replace("[", "%5B");
                var lData = await _apiService.VietStock_GetReportDataDetailValue_KQKD_ByReportDataIds(txt);
                Thread.Sleep(1000);
                if (!(lData?.data?.Any() ?? false))
                    return;

                //insert
                var entity = new Financial
                {
                    d = d,
                    s = code,
                    t = (int)DateTimeOffset.Now.ToUnixTimeSeconds()
                };

                var DoanhThu = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.DoanhThuThuan);
                var LoiNhuan = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.LNKTST);

                var LaiFVTPL = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.LaiFVTPL);
                var LaiHTM = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.LaiHTM);
                var LaiAFS = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.LaiAFS);
                var LaiChoVay = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.LaiChoVay);
                var DoanhThuMoiGioi = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.DoanhThuMoiGioi);
                var LoFVTPL = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.LoFVTPL);
                var LoHTM = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.LoHTM);
                var LoAFS = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.LoAFS);
                var ChiPhiMoiGioi = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.ChiPhiMoiGioi);
                AssignData(DoanhThu?.Value1, LoiNhuan?.Value1, LaiFVTPL?.Value1, LaiHTM?.Value1, LaiAFS?.Value1, LaiChoVay?.Value1, DoanhThuMoiGioi?.Value1, LoFVTPL?.Value1, LoHTM?.Value1, LoAFS?.Value1, ChiPhiMoiGioi?.Value1);
                _financialRepo.InsertOne(entity);

                void AssignData(double? DoanhThu, double? LoiNhuan, double? LaiFVTPL, double? LaiHTM, double? LaiAFS, double? LaiChoVay, double? DoanhThuMoiGioi, double? LoFVTPL, double? LoHTM, double? LoAFS, double? ChiPhiMoiGioi)
                {
                    entity.rv = DoanhThu ?? 0;
                    entity.pf = LoiNhuan ?? 0;
                    entity.broker = DoanhThuMoiGioi ?? 0;
                    entity.bcost = ChiPhiMoiGioi ?? 0;
                    entity.idebt = LaiChoVay ?? 0;
                    entity.trade = ((LaiFVTPL ?? 0) + (LaiHTM ?? 0) + (LaiAFS ?? 0)) - ((LoFVTPL ?? 0) + (LoHTM ?? 0) + (LoAFS ?? 0));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC_ChungKhoan_KQKD|EXCEPTION| {ex.Message}");
            }
        }
    }
}
