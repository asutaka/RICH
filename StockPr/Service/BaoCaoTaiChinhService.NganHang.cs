using MongoDB.Driver;
using StockPr.DAL.Entity;
using StockPr.Model;
using StockPr.Utils;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Xml.Linq;

namespace StockPr.Service
{
    public partial class BaoCaoTaiChinhService
    {
        /// <summary>
        /// Doanh thu,LNST, Chi phí vận hành lấy từ Kết quả kinh doanh
        /// CIR lấy từ chỉ số tài chính
        /// Nim, Tăng trưởng tín dụng, Giảm chi phí vốn, trích lập dự phòng lấy từ bảng cân đối kế toán và kết quả kinh doanh 
        /// 
        /// Casa tự tính trên BCTC


        /// Nợ xấu các mức tự nhập trên BCTC(tỉ lệ nợ xấu, tăng trưởng nợ xấu)
        /// Tỉ lệ bao trùm nợ xấu tự tính trên BCTC
        /// Cơ chế trích lập dự phòng:
        /// Nhóm 1 - Nợ đủ tiêu chuẩn(0%)
        /// Nhóm 2 - Nợ cần chú ý(5%)
        /// Nhóm 3 - Nợ dưới tiêu chuẩn(20%)
        /// Nhóm 4 - Nợ nghi ngờ(50%)
        /// Nhóm 5 - Nợ có khả năng mất vốn(100%)
        /// </summary>
        /// 
        /// 
        /// Kết luận
        /// Doanh thu: càng cao càng tốt
        /// LNST: càng cao càng tốt
        /// Trích lập dự phòng: càng nhỏ càng tốt
        /// CIR: càng nhỏ càng tốt
        /// Nim: càng cao càng tốt
        /// Tăng trưởng tín dụng: càng cao càng tốt,
        /// Giảm chi phí vốn: càng nhỏ càng tốt
        /// Casa: càng cao càng tốt
        /// Tỉ lệ nợ xấu: càng nhỏ càng tốt
        /// Tăng trưởng nợ xấu: càng nhỏ càng tốt
        /// Bao phủ nợ xấu: càng cao càng tốt
        /// <returns></returns>
        private async Task SyncBCTC_NganHang(bool ghide = false)
        {
            try
            {
                var lStockFilter = StaticVal._lStock.Where(x => x.status == 1 && x.cat.Any(x => x.ty == (int)EStockType.NganHang)).Select(x => x.s);
                foreach (var item in lStockFilter)
                {
                    var builder = Builders<Financial>.Filter;
                    var filter = builder.And(
                        builder.Eq(x => x.d, (int)StaticVal._currentTime.Item1),
                        builder.Eq(x => x.s, item)
                    );
                    var exists = _financialRepo.GetEntityByFilter(filter);
                    if (exists != null)
                    {
                        if(ghide)
                        {
                            _financialRepo.DeleteMany(filter);
                        }
                        else
                        {
                            continue;
                        }
                    }

                    await SyncBCTC_NganHang_KQKD(item);
                    await SyncBCTC_NganHang_CSTC(item);
                    await SyncBCTC_NganHang_ThuyetMinh(item);
                    await SyncBCTC_NganHang_NIM_TinDung(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC_NganHang|EXCEPTION| {ex.Message}");
            }
        }

        private async Task SyncBCTC_NganHang_KQKD(string code)
        {
            try
            {
                var time = GetCurrentTime();
                var lReportID = await _apiService.VietStock_KQKD_GetListReportData(code);
                Thread.Sleep(1000);
                if (!lReportID.data.Any())
                    return;
                lReportID.data.Reverse();
                ReportDataIDDetailResponse last = null;
                var d = 0;
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

                    //check day
                    d = int.Parse($"{year}{quarter}");
                    if (d != (int)StaticVal._currentTime.Item1)
                    {
                        d = 0;
                        continue;
                    }
                    last = item;
                    break;
                }

                if (d <= 0)
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
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC_NganHang_KQKD|EXCEPTION| {ex.Message}");
            }
        }

        private async Task SyncBCTC_NganHang_CSTC(string code)
        {
            try
            {
                var batchCount = 8;
                var lReportID = await _apiService.VietStock_CSTC_GetListTempID(code);
                Thread.Sleep(1000);
                if (!lReportID.data.Any())
                    return;
                lReportID.data.Reverse();
                ReportTempIDDetailResponse last = null;
                var d = 0;
                foreach (var item in lReportID.data)
                {
                    var year = item.YearPeriod;
                    var quarter = item.ReportTermID - 1;

                    //check day
                    d = int.Parse($"{year}{quarter}");
                    if (d != (int)StaticVal._currentTime.Item1)
                    {
                        d = 0;
                        continue;
                    }
                    last = item;
                    break;
                }

                if (d <= 0)
                    return;

                var strBuilder = new StringBuilder();
                strBuilder.Append($"StockCode={code}&");
                strBuilder.Append($"__RequestVerificationToken={StaticVal._VietStock_Token}&");
                strBuilder.Append($"ListTerms[0][ItemId]={last.IdTemp}&");
                strBuilder.Append($"ListTerms[0][YearPeriod]={last.YearPeriod}");
                var txt = strBuilder.ToString().Replace("]", "%5D").Replace("[", "%5B");
                var lData = await _apiService.VietStock_GetFinanceIndexDataValue_CSTC_ByListTerms(txt);
                Thread.Sleep(1000);

                var builder = Builders<Financial>.Filter;
                var filter = builder.And(
                     builder.Eq(x => x.s, code),
                            builder.Eq(x => x.d, d)
                );
                var lUpdate = _financialRepo.GetByFilter(filter);
                var entity = lUpdate.FirstOrDefault();
                if (entity is null)
                {
                    return;
                }

                //
                var cir = lData?.data.FirstOrDefault(x => x.FinanceIndexID == (int)EFinanceIndex.CIR);
                AssignData(cir?.Value1);

                entity.t = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                _financialRepo.Update(entity);

                void AssignData(double? cir)
                {
                    entity.cir_r = cir ?? 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC_NganHang_CSTC|EXCEPTION| {ex.Message}");
            }
        }

        private async Task SyncBCTC_NganHang_ThuyetMinh(string code)
        {
            try
            {
                var lReportID = await _apiService.VietStock_TM_GetListReportData(code);
                Thread.Sleep(1000);
                if (!lReportID.data.Any())
                    return;
                lReportID.data.Reverse();
                ReportDataIDDetailResponse last = null;
                var d = 0;
                foreach (var item in lReportID.data)
                {
                    var year = item.YearPeriod;
                    var quarter = item.ReportTermID - 1;

                    //check day
                    d = int.Parse($"{year}{quarter}");
                    if (d != (int)StaticVal._currentTime.Item1)
                    {
                        d = 0;
                        continue;
                    }
                    last = item;
                    break;
                }

                if (d <= 0)
                    return;

                var strBuilder = new StringBuilder();
                strBuilder.Append($"StockCode={code}&");
                strBuilder.Append($"Unit=1000000&");
                strBuilder.Append($"__RequestVerificationToken={StaticVal._VietStock_Token}&");
                strBuilder.Append($"listReportDataIds[0][ReportDataId]={last.ReportDataID}&");
                strBuilder.Append($"listReportDataIds[0][YearPeriod]={last.BasePeriodBegin / 100}");
                var txt = strBuilder.ToString().Replace("]", "%5D").Replace("[", "%5B");
                var lData = await _apiService.VietStock_GetReportDataDetailValue_TM_ByReportDataIds(txt);
                Thread.Sleep(1000);

                var builder = Builders<Financial>.Filter;
                var filter = builder.And(
                     builder.Eq(x => x.s, code),
                            builder.Eq(x => x.d, d)
                );

                var lUpdate = _financialRepo.GetByFilter(filter);
                var entity = lUpdate.FirstOrDefault();
                if (entity is null)
                {
                    return;
                }

                var NoNhom1 = lData?.data.FirstOrDefault(x => x.ReportNormNoteID == (int)EReportNormId.NoNhom1);
                var NoNhom2 = lData?.data.FirstOrDefault(x => x.ReportNormNoteID == (int)EReportNormId.NoNhom2);
                var NoNhom3 = lData?.data.FirstOrDefault(x => x.ReportNormNoteID == (int)EReportNormId.NoNhom3);
                var NoNhom4 = lData?.data.FirstOrDefault(x => x.ReportNormNoteID == (int)EReportNormId.NoNhom4);
                var NoNhom5 = lData?.data.FirstOrDefault(x => x.ReportNormNoteID == (int)EReportNormId.NoNhom5);
                var TienGuiKH = lData?.data.FirstOrDefault(x => x.ReportNormNoteID == (int)EReportNormId.TienGuiKhachHang);
                var TienGuiKhongKyHan = lData?.data.FirstOrDefault(x => x.ReportNormNoteID == (int)EReportNormId.TienGuiKhongKyHan);
                var casa = Math.Round((TienGuiKhongKyHan?.Value1?? 0) * 100 / (TienGuiKH?.Value1?? 1), 1);

                entity.casa_r = casa;

                AssignData(NoNhom1?.Value1, NoNhom2?.Value1, NoNhom3?.Value1, NoNhom4?.Value1, NoNhom5?.Value1);
                void AssignData(double? NoNhom1, double? NoNhom2, double? NoNhom3, double? NoNhom4, double? NoNhom5)
                {
                    entity.debt1 = (NoNhom1 ?? 0) / 1000;
                    entity.debt2 = (NoNhom2 ?? 0) / 1000;
                    entity.debt3 = (NoNhom3 ?? 0) / 1000;
                    entity.debt4 = (NoNhom4 ?? 0) / 1000;
                    entity.debt5 = (NoNhom5 ?? 0) / 1000;
                }

                entity.t = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                _financialRepo.Update(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC_NganHang_ThuyetMinh|EXCEPTION| {ex.Message}");
            }
        }

        private async Task SyncBCTC_NganHang_NIM_TinDung(string code)
        {
            try
            {
                await SubBCTC_KQKD(code);
                await SubBCTC_CDKT(code);

                var count = _lSubKQKD.Count();

                for (int i = 3; i < count; i++)
                {
                    var elementKQKD = _lSubKQKD[i];

                    var builder = Builders<Financial>.Filter;
                    var filter = builder.And(
                        builder.Eq(x => x.s, code),
                        builder.Eq(x => x.d, elementKQKD.d)
                    );

                    var lUpdate = _financialRepo.GetByFilter(filter);
                    Financial entityUpdate = lUpdate.FirstOrDefault();
                    if (lUpdate is null || !lUpdate.Any())
                    {
                        continue;
                    }
                    //
                    var elementKQKD_1 = _lSubKQKD[i - 1];
                    var elementKQKD_2 = _lSubKQKD[i - 2];
                    var elementKQKD_3 = _lSubKQKD[i - 3];


                    //
                    var elementCDKT = _lSubCDKT[i];
                    var elementCDKT_1 = _lSubCDKT[i - 1];
                    var elementCDKT_2 = _lSubCDKT[i - 2];
                    var elementCDKT_3 = _lSubCDKT[i - 3];
                    var lastQuarterPrev = _lSubCDKT.FirstOrDefault(x => x.d == int.Parse($"{-1 + elementCDKT.d / 10}4"));
                    var TuSo = 4 * (elementKQKD.ThuNhapLaiThuan +
                                    elementKQKD_1.ThuNhapLaiThuan +
                                    elementKQKD_2.ThuNhapLaiThuan +
                                    elementKQKD_3.ThuNhapLaiThuan);

                    var MauSo1 = elementCDKT.TienGuiNHNN + elementCDKT.TienGuiTCTD + elementCDKT.ChoVayTCTD + elementCDKT.ChungKhoanKD + elementCDKT.ChoVayKH + elementCDKT.ChungKhoanDauTu + elementCDKT.ChungKhoanDaoHan;
                    var MauSo2 = elementCDKT_1.TienGuiNHNN + elementCDKT_1.TienGuiTCTD + elementCDKT_1.ChoVayTCTD + elementCDKT_1.ChungKhoanKD + elementCDKT_1.ChoVayKH + elementCDKT_1.ChungKhoanDauTu + elementCDKT_1.ChungKhoanDaoHan;
                    var MauSo3 = elementCDKT_2.TienGuiNHNN + elementCDKT_2.TienGuiTCTD + elementCDKT_2.ChoVayTCTD + elementCDKT_2.ChungKhoanKD + elementCDKT_2.ChoVayKH + elementCDKT_2.ChungKhoanDauTu + elementCDKT_2.ChungKhoanDaoHan;
                    var MauSo4 = elementCDKT_3.TienGuiNHNN + elementCDKT_3.TienGuiTCTD + elementCDKT_3.ChoVayTCTD + elementCDKT_3.ChungKhoanKD + elementCDKT_3.ChoVayKH + elementCDKT_3.ChungKhoanDauTu + elementCDKT_3.ChungKhoanDaoHan;
                    var nim = Math.Round(100 * TuSo / (MauSo1 + MauSo2 + MauSo3 + MauSo4), 1);
                    double tindung = 0;
                    if (lastQuarterPrev != null)
                    {
                        tindung = Math.Round(100 * (-1 + elementCDKT.ChoVayKH / lastQuarterPrev.ChoVayKH), 1);
                    }
                    entityUpdate.nim_r = nim;
                    entityUpdate.credit_r = tindung;
                    entityUpdate.cost_r = Math.Round(100 * (-1 + elementKQKD.ChiPhiLai / elementKQKD_1.ChiPhiLai), 1);
                    entityUpdate.risk = Math.Abs(elementCDKT.TrichhLap);
                    var totalRisk = entityUpdate.debt3 + entityUpdate.debt4 + entityUpdate.debt5;
                    if (totalRisk <= 0)
                        continue;

                    entityUpdate.cover_r = Math.Round((entityUpdate.risk ?? 0 * 100) / totalRisk, 1);
                    entityUpdate.t = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
                    _financialRepo.Update(entityUpdate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SyncBCTC_BatDongSan_KQKD|EXCEPTION| {ex.Message}");
            }
        }

        private List<SubKQKD> _lSubKQKD = new List<SubKQKD>();
        private async Task SubBCTC_KQKD(string code)
        {
            try
            {
                _lSubKQKD.Clear();
                var batchCount = 8;
                var lReportID = await _apiService.VietStock_KQKD_GetListReportData(code);
                if (lReportID is null)
                {
                    return;
                }
                lReportID.data = lReportID.data.TakeLast(5).ToList();
                Thread.Sleep(1000);
                if (!lReportID.data.Any())
                    return;
                var time = GetCurrentTime();
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
                lReportID.data = lReportID.data.Where(x => (x.Isunited == 0 || x.Isunited == 1) && x.BasePeriodBegin >= 201901).ToList();
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

                        var year = element.YearPeriod;
                        var quarter = element.ReportTermID - 1;
                        var LaiThuan = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.ThuNhapLaiThuan);
                        var ChiPhiLai = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.ChiPhiLai);

                        switch (i)
                        {
                            case 0: AssignData(LaiThuan?.Value1, ChiPhiLai?.Value1); break;
                            case 1: AssignData(LaiThuan?.Value2, ChiPhiLai?.Value2); break;
                            case 2: AssignData(LaiThuan?.Value3, ChiPhiLai?.Value3); break;
                            case 3: AssignData(LaiThuan?.Value4, ChiPhiLai?.Value4); break;
                            case 4: AssignData(LaiThuan?.Value5, ChiPhiLai?.Value5); break;
                            case 5: AssignData(LaiThuan?.Value6, ChiPhiLai?.Value6); break;
                            case 6: AssignData(LaiThuan?.Value7, ChiPhiLai?.Value7); break;
                            case 7: AssignData(LaiThuan?.Value8, ChiPhiLai?.Value8); break;
                            default: break;
                        };

                        void AssignData(double? LaiThuan, double? ChiPhiLai)
                        {
                            _lSubKQKD.Add(new SubKQKD
                            {
                                d = int.Parse($"{year}{quarter}"),
                                ThuNhapLaiThuan = LaiThuan ?? 0,
                                ChiPhiLai = ChiPhiLai ?? 0
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SubBCTC_KQKD|EXCEPTION| {ex.Message}");
            }
        }

        private List<SubCDKT> _lSubCDKT = new List<SubCDKT>();
        private async Task SubBCTC_CDKT(string code)
        {
            try
            {
                _lSubCDKT.Clear();
                var batchCount = 8;
                var lReportID = await _apiService.VietStock_CDKT_GetListReportData(code);
                lReportID.data = lReportID.data.TakeLast(5).ToList();
                Thread.Sleep(1000);
                if (!lReportID.data.Any())
                    return;
                var time = GetCurrentTime();
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

                        var year = element.YearPeriod;
                        var quarter = element.ReportTermID - 1;
                        var TienGuiNHNN = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.TienGuiNHNN);
                        var TienGuiTCTD = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.TienGuiTCTD);
                        var ChoVayTCTD = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.ChoVayTCTD);
                        var ChungKhoanKD = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.ChungKhoanKD);
                        var ChoVayKH = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.ChoVayKH);
                        var ChungKhoanDauTu = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.ChungKhoanDauTu);
                        var ChungKhoanDaoHan = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.ChungKhoanDaoHan);
                        var TrichLap = lData?.data.FirstOrDefault(x => x.ReportnormId == (int)EReportNormId.TrichLap);

                        switch (i)
                        {
                            case 0: AssignData(TienGuiNHNN?.Value1, TienGuiTCTD?.Value1, ChoVayTCTD?.Value1, ChungKhoanKD?.Value1, ChoVayKH?.Value1, ChungKhoanDauTu?.Value1, ChungKhoanDaoHan?.Value1, TrichLap?.Value1); break;
                            case 1: AssignData(TienGuiNHNN?.Value2, TienGuiTCTD?.Value2, ChoVayTCTD?.Value2, ChungKhoanKD?.Value2, ChoVayKH?.Value2, ChungKhoanDauTu?.Value2, ChungKhoanDaoHan?.Value2, TrichLap?.Value2); break;
                            case 2: AssignData(TienGuiNHNN?.Value3, TienGuiTCTD?.Value3, ChoVayTCTD?.Value3, ChungKhoanKD?.Value3, ChoVayKH?.Value3, ChungKhoanDauTu?.Value3, ChungKhoanDaoHan?.Value3, TrichLap?.Value3); break;
                            case 3: AssignData(TienGuiNHNN?.Value4, TienGuiTCTD?.Value4, ChoVayTCTD?.Value4, ChungKhoanKD?.Value4, ChoVayKH?.Value4, ChungKhoanDauTu?.Value4, ChungKhoanDaoHan?.Value4, TrichLap?.Value4); break;
                            case 4: AssignData(TienGuiNHNN?.Value5, TienGuiTCTD?.Value5, ChoVayTCTD?.Value5, ChungKhoanKD?.Value5, ChoVayKH?.Value5, ChungKhoanDauTu?.Value5, ChungKhoanDaoHan?.Value5, TrichLap?.Value5); break;
                            case 5: AssignData(TienGuiNHNN?.Value6, TienGuiTCTD?.Value6, ChoVayTCTD?.Value6, ChungKhoanKD?.Value6, ChoVayKH?.Value6, ChungKhoanDauTu?.Value6, ChungKhoanDaoHan?.Value6, TrichLap?.Value6); break;
                            case 6: AssignData(TienGuiNHNN?.Value7, TienGuiTCTD?.Value7, ChoVayTCTD?.Value7, ChungKhoanKD?.Value7, ChoVayKH?.Value7, ChungKhoanDauTu?.Value7, ChungKhoanDaoHan?.Value7, TrichLap?.Value7); break;
                            case 7: AssignData(TienGuiNHNN?.Value8, TienGuiTCTD?.Value8, ChoVayTCTD?.Value8, ChungKhoanKD?.Value8, ChoVayKH?.Value8, ChungKhoanDauTu?.Value8, ChungKhoanDaoHan?.Value8, TrichLap?.Value8); break;
                            default: break;
                        };

                        void AssignData(double? guiNHNN, double? guiTCTD, double? chovayTCTD, double? ckKD, double? chovayKH, double? ckDauTu, double? ckDaoHan, double? trichlap)
                        {
                            _lSubCDKT.Add(new SubCDKT
                            {
                                d = int.Parse($"{year}{quarter}"),
                                TienGuiNHNN = guiNHNN ?? 0,
                                TienGuiTCTD = guiTCTD ?? 0,
                                ChoVayTCTD = chovayTCTD ?? 0,
                                ChungKhoanKD = ckKD ?? 0,
                                ChoVayKH = chovayKH ?? 0,
                                ChungKhoanDauTu = ckDauTu ?? 0,
                                ChungKhoanDaoHan = ckDaoHan ?? 0,
                                TrichhLap = trichlap ?? 0
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"BllService.SubBCTC_KQKD|EXCEPTION| {ex.Message}");
            }
        }

        

        public class SubKQKD
        {
            public int d { get; set; }
            public double ThuNhapLaiThuan { get; set; }
            public double ChiPhiLai { get; set; }
        }

        public class SubCDKT
        {
            public int d { get; set; }
            public double TienGuiNHNN { get; set; }
            public double TienGuiTCTD { get; set; }
            public double ChoVayTCTD { get; set; }
            public double ChungKhoanKD { get; set; }
            public double ChoVayKH { get; set; }
            public double ChungKhoanDauTu { get; set; }
            public double ChungKhoanDaoHan { get; set; }
            public double TrichhLap { get; set; }
        }
    }
}
