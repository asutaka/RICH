using MongoDB.Driver;
using StockPr.DAL.Entity;
using StockPr.Model;
using StockPr.Utils;
using System.Text;

namespace StockPr.Service
{
    public partial class BaoCaoTaiChinhService
    {
        private async Task SyncBCTC_ChungKhoanzz(bool onlyLast = false)
        {
            try
            {
                var lStockFilter = StaticVal._lStock.Where(x => x.status == 1 && x.cat.Any(x => x.ty == (int)EStockType.ChungKhoan)).Select(x => x.s);

                foreach (var item in lStockFilter)
                {
                    await SyncBCTC_ChungKhoan_KQKDzz(item, onlyLast);
                    await SyncBCTC_CDKT(item, ECDKTType.ChungKhoan);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC_ChungKhoan|EXCEPTION| {ex.Message}");
            }
        }

        private async Task SyncBCTC_ChungKhoan_KQKDzz(string code, bool onlyLast)
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

                        var lUpdate = _financialRepo.GetByFilter(filter);
                        Financial entityUpdate = lUpdate.FirstOrDefault();
                        if (lUpdate is null || !lUpdate.Any())
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

                        switch (i)
                        {
                            case 0: AssignData(DoanhThu?.Value1, LoiNhuan?.Value1, LaiFVTPL?.Value1, LaiHTM?.Value1, LaiAFS?.Value1, LaiChoVay?.Value1, DoanhThuMoiGioi?.Value1, LoFVTPL?.Value1, LoHTM?.Value1, LoAFS?.Value1, ChiPhiMoiGioi?.Value1); break;
                            case 1: AssignData(DoanhThu?.Value2, LoiNhuan?.Value2, LaiFVTPL?.Value2, LaiHTM?.Value2, LaiAFS?.Value2, LaiChoVay?.Value2, DoanhThuMoiGioi?.Value2, LoFVTPL?.Value2, LoHTM?.Value2, LoAFS?.Value2, ChiPhiMoiGioi?.Value2); break;
                            case 2: AssignData(DoanhThu?.Value3, LoiNhuan?.Value3, LaiFVTPL?.Value3, LaiHTM?.Value3, LaiAFS?.Value3, LaiChoVay?.Value3, DoanhThuMoiGioi?.Value3, LoFVTPL?.Value3, LoHTM?.Value3, LoAFS?.Value3, ChiPhiMoiGioi?.Value3); break;
                            case 3: AssignData(DoanhThu?.Value4, LoiNhuan?.Value4, LaiFVTPL?.Value4, LaiHTM?.Value4, LaiAFS?.Value4, LaiChoVay?.Value4, DoanhThuMoiGioi?.Value4, LoFVTPL?.Value4, LoHTM?.Value4, LoAFS?.Value4, ChiPhiMoiGioi?.Value4); break;
                            case 4: AssignData(DoanhThu?.Value5, LoiNhuan?.Value5, LaiFVTPL?.Value5, LaiHTM?.Value5, LaiAFS?.Value5, LaiChoVay?.Value5, DoanhThuMoiGioi?.Value5, LoFVTPL?.Value5, LoHTM?.Value5, LoAFS?.Value5, ChiPhiMoiGioi?.Value5); break;
                            case 5: AssignData(DoanhThu?.Value6, LoiNhuan?.Value6, LaiFVTPL?.Value6, LaiHTM?.Value6, LaiAFS?.Value6, LaiChoVay?.Value6, DoanhThuMoiGioi?.Value6, LoFVTPL?.Value6, LoHTM?.Value6, LoAFS?.Value6, ChiPhiMoiGioi?.Value6); break;
                            case 6: AssignData(DoanhThu?.Value7, LoiNhuan?.Value7, LaiFVTPL?.Value7, LaiHTM?.Value7, LaiAFS?.Value7, LaiChoVay?.Value7, DoanhThuMoiGioi?.Value7, LoFVTPL?.Value7, LoHTM?.Value7, LoAFS?.Value7, ChiPhiMoiGioi?.Value7); break;
                            case 7: AssignData(DoanhThu?.Value8, LoiNhuan?.Value8, LaiFVTPL?.Value8, LaiHTM?.Value8, LaiAFS?.Value8, LaiChoVay?.Value8, DoanhThuMoiGioi?.Value8, LoFVTPL?.Value8, LoHTM?.Value8, LoAFS?.Value8, ChiPhiMoiGioi?.Value8); break;
                            default: break;
                        };

                        entityUpdate.t = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                        _financialRepo.Update(entityUpdate);

                        void AssignData(double? DoanhThu, double? LoiNhuan, double? LaiFVTPL, double? LaiHTM, double? LaiAFS, double? LaiChoVay, double? DoanhThuMoiGioi, double? LoFVTPL, double? LoHTM, double? LoAFS, double? ChiPhiMoiGioi)
                        {
                            entityUpdate.rv = DoanhThu ?? 0;
                            entityUpdate.pf = LoiNhuan ?? 0;
                            entityUpdate.broker = DoanhThuMoiGioi ?? 0;
                            entityUpdate.bcost = ChiPhiMoiGioi ?? 0;
                            entityUpdate.idebt = LaiChoVay ?? 0;
                            entityUpdate.trade = ((LaiFVTPL ?? 0) + (LaiHTM ?? 0) + (LaiAFS ?? 0)) - ((LoFVTPL ?? 0) + (LoHTM ?? 0) + (LoAFS ?? 0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC_BatDongSan_KQKD|EXCEPTION| {ex.Message}");
            }
        }
    }
}
